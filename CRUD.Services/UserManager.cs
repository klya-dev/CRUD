namespace CRUD.Services;

/// <inheritdoc cref="IUserManager"/>
public sealed class UserManager : IUserManager
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAvatarManager _avatarManager;
    private readonly AvatarManagerOptions _avatarManagerOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IValidator<CreateUserDto> _createUserDtoValidator;
    private readonly IValidator<OAuthCompleteRegistrationDto> _oAuthCompleteRegistrationDtoValidator;
    private readonly IValidator<UpdateUserDto> _updateUserDtoValidator;
    private readonly IValidator<DeleteUserDto> _deleteUserDtoValidator;
    private readonly IValidator<SetRoleDto> _setRoleDtoValidator;
    private readonly ILogger<UserManager> _logger;

    public UserManager(ApplicationDbContext db, IPasswordHasher passwordHasher, IAvatarManager avatarManager, IOptions<AvatarManagerOptions> avatarManagerOptions, IHttpClientFactory httpClientFactory, IValidator<CreateUserDto> createUserDtoValidator, IValidator<OAuthCompleteRegistrationDto> oAuthCompleteRegistrationDtoValidator, IValidator<UpdateUserDto> updateUserDtoValidator, IValidator<DeleteUserDto> deleteUserDtoValidator, IValidator<SetRoleDto> setRoleDtoValidator, ILogger<UserManager> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _avatarManagerOptions = avatarManagerOptions.Value;
        _httpClientFactory = httpClientFactory;
        _createUserDtoValidator = createUserDtoValidator;
        _oAuthCompleteRegistrationDtoValidator = oAuthCompleteRegistrationDtoValidator;
        _updateUserDtoValidator = updateUserDtoValidator;
        _deleteUserDtoValidator = deleteUserDtoValidator;
        _setRoleDtoValidator = setRoleDtoValidator;
        _avatarManager = avatarManager;
        _logger = logger;
    }

    public Task<User?> GetUserAsync(Guid userId, bool tracking = true, CancellationToken ct = default)
    {
        if (tracking)
            return _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

        return _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public Task<User?> GetUserAsync(string username, bool tracking = true, CancellationToken ct = default)
    {
        if (tracking)
            return _db.Users.FirstOrDefaultAsync(x => x.Username == username, ct);

        return _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, ct);
    }

    public async Task<ServiceResult<UserDto>> GetUserDtoAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Не удалось получить ссылку на аватарку
        // Возвращаем ошибку, только, если пользователь не найден
        // Лучше вернём DTO без аватарки, чем вообще не вернём, а если пользователь не найден, то и DTO не сформируется
        var avatarPresignedUrlResult = await _avatarManager.GetPresignedUrlAvatarAsync(userId, ct: ct);
        if (avatarPresignedUrlResult.ErrorMessage != null
            && avatarPresignedUrlResult.ErrorMessage == ErrorMessages.UserNotFound)
            return ServiceResult<UserDto>.Fail(avatarPresignedUrlResult.ErrorMessage);

        // Пользователь не найден | создаём DTO на стороне базы
        var userDto = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.ToUserDto(avatarPresignedUrlResult.Value)).FirstOrDefaultAsync(ct);
        if (userDto == null)
            return ServiceResult<UserDto>.Fail(ErrorMessages.UserNotFound);

        return ServiceResult<UserDto>.Success(userDto);
    }

    public async Task<ServiceResult<UserFullDto>> GetUserFullDtoAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден | создаём DTO на стороне базы
        var userDto = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.ToUserFullDto()).FirstOrDefaultAsync(ct);
        if (userDto == null)
            return ServiceResult<UserFullDto>.Fail(ErrorMessages.UserNotFound);

        return ServiceResult<UserFullDto>.Success(userDto);
    }

    public async Task<ServiceResult> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(updateUserDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _updateUserDtoValidator.ValidateAsync(updateUserDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидные данные, это его ответственность, если исключение - значит разраб накосипорил, недотестил
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(UpdateUserDto), validationResult.Errors));

        // Пользователь не найден
        var userFromDb = await _db.Users.Where(x => x.Id == userId).Select(x => new { x.Firstname, x.Username, x.LanguageCode, x.RowVersion }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Не обнаружено изменений
        if (userFromDb.Firstname == updateUserDto.Firstname &&
           userFromDb.Username == updateUserDto.Username &&
           userFromDb.LanguageCode == updateUserDto.LanguageCode)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        // Username уже занят
        if (updateUserDto.Username != userFromDb.Username && await IsUsernameAlreadyTakenAsync(updateUserDto.Username, ct)) // Если пользователь меняет username и такой username занят
            return ServiceResult.Fail(ErrorMessages.UsernameAlreadyTaken);

        // Обновляем пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userId && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.Firstname, updateUserDto.Firstname)
                .SetProperty(p => p.Username, updateUserDto.Username)
                .SetProperty(p => p.LanguageCode, updateUserDto.LanguageCode), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid userId, DeleteUserDto deleteUserDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(deleteUserDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _deleteUserDtoValidator.ValidateAsync(deleteUserDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(DeleteUserDto), validationResult.Errors));

        // Пользователь не найден. Грузим только HashedPassword, AvatarURL
        var userFromDb = await _db.Users.Where(x => x.Id == userId).Select(x => new { x.HashedPassword, x.AvatarURL }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Проверка пароля
        if (!_passwordHasher.Verify(deleteUserDto.Password, userFromDb.HashedPassword))
            return ServiceResult.Fail(ErrorMessages.InvalidPassword);

        // Удаляем пользователя
        await _db.Users.Where(x => x.Id == userId)
            .ExecuteDeleteAsync(ct);

        // Удаляем аватарку
        var deleteAvatarResult = await _avatarManager.DeleteAvatarAsync(userFromDb.AvatarURL, ct);

        // Не удалось удалить аватарку - логируем
        if (deleteAvatarResult.ErrorMessage != null)
            _logger.LogWarning("Не удалось удалить аватарку пользователя \"{userId}\". AvatarUrl: \"{avatarUrl}\".", userId, userFromDb.AvatarURL);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден. Грузим только AvatarURL
        var userFromDb = await _db.Users.Where(x => x.Id == userId).Select(x => new { x.AvatarURL }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Удаляем пользователя
        await _db.Users.Where(x => x.Id == userId)
            .ExecuteDeleteAsync(ct);

        // Удаляем аватарку
        var deleteAvatarResult = await _avatarManager.DeleteAvatarAsync(userFromDb.AvatarURL, ct);

        // Не удалось удалить аватарку - логируем
        if (deleteAvatarResult.ErrorMessage != null)
            _logger.LogWarning("Не удалось удалить аватарку пользователя \"{userId}\". AvatarUrl: \"{avatarUrl}\".", userId, userFromDb.AvatarURL);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<User>> CreateUserAsync(CreateUserDto createUserDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(createUserDto);

        // Валидация модели
        var validationResult = await _createUserDtoValidator.ValidateAsync(createUserDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreateUserDto), validationResult.Errors));

        // Username уже занят
        if (await IsUsernameAlreadyTakenAsync(createUserDto.Username, ct)) 
            return ServiceResult<User>.Fail(ErrorMessages.UsernameAlreadyTaken);

        // Email уже занят
        if (await IsEmailAlreadyTakenAsync(createUserDto.Email, ct))
            return ServiceResult<User>.Fail(ErrorMessages.EmailAlreadyTaken);

        // PhoneNumber уже занят
        if (await IsPhoneNumberAlreadyTakenAsync(createUserDto.PhoneNumber, ct))
            return ServiceResult<User>.Fail(ErrorMessages.PhoneNumberAlreadyTaken);

        var user = new User
        {
            Firstname = createUserDto.Firstname,
            Username = createUserDto.Username,
            HashedPassword = _passwordHasher.GenerateHashedPassword(createUserDto.Password),
            LanguageCode = createUserDto.LanguageCode,
            Role = UserRoles.User,
            IsPremium = false,
            AvatarURL = _avatarManagerOptions.DefaultAvatarPath,
            Email = createUserDto.Email,
            PhoneNumber = createUserDto.PhoneNumber
        };

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult<User>> CreateUserAsync(OpenIdUserInfo userInfo, OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(userInfo);
        ArgumentNullException.ThrowIfNull(oAuthCompleteRegistrationDto);

        // Валидация модели OAuthCompleteRegistrationDto
        var validationResultOAuthCompleteRegistrationDto = await _oAuthCompleteRegistrationDtoValidator.ValidateAsync(oAuthCompleteRegistrationDto, ct);
        if (!validationResultOAuthCompleteRegistrationDto.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(OAuthCompleteRegistrationDto), validationResultOAuthCompleteRegistrationDto.Errors));

        // Создаём CreateUserDto из OpenIdUserInfo и OAuthCompleteRegistrationDto
        var createUserDto = userInfo.ToCreateUserDto(oAuthCompleteRegistrationDto);

        // Валидация модели CreateUserDto
        var validationResultCreateUserDto = await _createUserDtoValidator.ValidateAsync(createUserDto, ct);
        if (!validationResultCreateUserDto.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreateUserDto), validationResultCreateUserDto.Errors));

        string createUsername = createUserDto.Username;

        // Username уже занят, просто меняем на рандомный
        if (await IsUsernameAlreadyTakenAsync(createUserDto.Username, ct))
            createUsername = RandomDataGenerator.GenerateRandomUsername();

        // Email уже занят
        if (await IsEmailAlreadyTakenAsync(createUserDto.Email, ct))
            return ServiceResult<User>.Fail(ErrorMessages.EmailAlreadyTaken);

        // PhoneNumber уже занят
        if (await IsPhoneNumberAlreadyTakenAsync(createUserDto.PhoneNumber, ct))
            return ServiceResult<User>.Fail(ErrorMessages.PhoneNumberAlreadyTaken);

        var user = new User
        {
            Firstname = createUserDto.Firstname,
            Username = createUsername,
            HashedPassword = _passwordHasher.GenerateHashedPassword(createUserDto.Password),
            LanguageCode = createUserDto.LanguageCode,
            Role = UserRoles.User,
            IsPremium = false,
            AvatarURL = _avatarManagerOptions.DefaultAvatarPath,
            Email = userInfo.Email,
            PhoneNumber = createUserDto.PhoneNumber,
        };

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        // Создаём HttpClient для скачивания картинки
        var httpClient = _httpClientFactory.CreateClient();

        // Устанавливаем пользователю UserInfo аватарку
        var setAvatarResult = await _avatarManager.SetAvatarAsync(user.Id, await OAuthHelper.DownloadPictureAsync(httpClient, userInfo.Picture), ct);

        // Не удалось установить аватарку, просто логируем. Если не получилось, то дефолтную так и оставим
        if (setAvatarResult.ErrorMessage != null)
            _logger.LogWarning("Не удалось установить аватарку из UserInfo. Причина: \"{error}\".", setAvatarResult.ErrorMessage);

        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult> SetRoleUserAsync(Guid userId, SetRoleDto setRoleDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(setRoleDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _setRoleDtoValidator.ValidateAsync(setRoleDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(SetRoleDto), validationResult.Errors));

        // Пользователь не найден
        var userFromDb = await _db.Users.Where(x => x.Id == userId).Select(x => new { x.Role, x.RowVersion }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Не обнаружено изменений
        if (userFromDb.Role == setRoleDto.Role)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        // Обновляем пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userId && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.Role, setRoleDto.Role), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RevokeRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userExists = await _db.Users.AnyAsync(x => x.Id == userId, ct);
        if (!userExists)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Удаляем все Refresh-токены пользователя
        await _db.AuthRefreshTokens.Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return ServiceResult.Success();
    }

    public Task<bool> IsUsernameAlreadyTakenAsync(string username, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(x => x.Username == username, ct);
    }

    public Task<bool> IsEmailAlreadyTakenAsync(string email, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(x => x.Email == email, ct);
    }

    public Task<bool> IsPhoneNumberAlreadyTakenAsync(string phoneNumber, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(x => x.PhoneNumber == phoneNumber, ct);
    }

    public Task<bool> IsUserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(x => x.Id == userId, ct);
    }

    public Task<bool> IsUserExistsAsync(string username, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(x => x.Username == username, ct);
    }

    public async Task<ServiceResult> ConfirmEmailAsync(string token, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(token);

        // Запрос не найден
        var confirmEmailRequestFromDb = await _db.ConfirmEmailRequests.Where(x => x.Token == token)
            .AsNoTracking()
            .Select(x => new
            {
                // Очень осторожно с полной сущностью (Request = x), если где-то загрузили/создали всю сущность, а потом вызвали ExecuteUpdateAsync, то из-за кэша значения могут разниться
                // Прикол, в том, что EF и вправду делает запрос в базу (FirstOrDefaultAsync), но после получения этих данных он сравнивает ID сущности с ID сущности в кэше и просто отдаёт кэшированные значения - так и работает ChangeTracker (в одном контексте базы) | После ExecuteUpdateAsync данные сущности отличные от кэша
                // Поэтому когда грузим всю сущность через .Select() - .AsNoTracking() обязательно и данные будут прямиком из базы (актуальные)
                Request = x,

                // Пользователя может не существовать. Если не сущетсвует, то userFromDb.User = null
                User = x.User == null ? null : new { x.User.Id, x.User.IsEmailConfirm, x.User.RowVersion }
            })
            .FirstOrDefaultAsync(ct);

        if (confirmEmailRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Удаляем токен из базы (в любом случае надо удалить токен, он одноразовый)
        _db.ConfirmEmailRequests.Remove(confirmEmailRequestFromDb.Request); // Через ExecuteDelete не получится: 'ExecuteDelete'/'ExecuteUpdate' operations on hierarchies mapped as TPT is not supported + так-то мы уже прогрузили сущность в память
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (confirmEmailRequestFromDb.Request.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Пользователь не найден
        var userFromDb = confirmEmailRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Электронная почта пользователя уже подтверждёна
        if (userFromDb.IsEmailConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedEmail);

        // Обновляем пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userFromDb.Id && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.IsEmailConfirm, true), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> VerificatePhoneNumberAsync(Guid userId, string code, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(code);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Запрос не найден (по UserId и коду)
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.Where(x => x.UserId == userId && x.Code == code)
            .AsNoTracking()
            .Select(x => new
            {
                // Очень осторожно с полной сущностью (Request = x), если где-то загрузили/создали всю сущность, а потом вызвали ExecuteUpdateAsync, то из-за кэша значения могут разниться
                // Прикол, в том, что EF и вправду делает запрос в базу (FirstOrDefaultAsync), но после получения этих данных он сравнивает ID сущности с ID сущности в кэше и просто отдаёт кэшированные значения - так и работает ChangeTracker (в одном контексте базы)
                // Поэтому когда грузим всю сущность через .Select() - .AsNoTracking() обязательно
                Request = x,

                // Пользователя может не существовать. Если не сущетсвует, то userFromDb.User = null
                User = x.User == null ? null : new { x.User.Id, x.User.IsPhoneNumberConfirm, x.User.RowVersion }
            })
            .FirstOrDefaultAsync(ct);

        if (verificationPhoneNumberRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidCode);

        // Удаляем код из базы (в любом случае надо удалить код, он одноразовый)
        _db.VerificationPhoneNumberRequests.Remove(verificationPhoneNumberRequestFromDb.Request);
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (verificationPhoneNumberRequestFromDb.Request.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidCode);

        // Пользователь не найден
        var userFromDb = verificationPhoneNumberRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Телефонный номер пользователя уже подтверждён
        if (userFromDb.IsPhoneNumberConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedPhoneNumber);

        // Обновляем пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userFromDb.Id && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.IsPhoneNumberConfirm, true), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }

    public async Task CreateAdminUserAsync(CancellationToken ct = default)
    {
        // Уже существует админ, ничего не делаем
        if (await IsUserExistsAsync("admin", ct))
            return;

        var user = new User()
        {
            Firstname = "Klya",
            Username = "admin",
            HashedPassword = _passwordHasher.GenerateHashedPassword("123"),
            LanguageCode = "ru",
            Role = UserRoles.Admin,
            IsPremium = true,
            AvatarURL = _avatarManagerOptions.DefaultAvatarPath,
            Email = "admin@mail.ru",
            PhoneNumber = "1234567890",
        };

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);
    }
}