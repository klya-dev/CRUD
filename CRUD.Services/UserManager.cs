using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IUserManager"/>
public class UserManager : IUserManager
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAvatarManager _avatarManager;
    private readonly AvatarManagerOptions _avatarManagerOptions;
    private readonly IValidator<User> _userValidator;
    private readonly IValidator<CreateUserDto> _createUserDtoValidator;
    private readonly IValidator<OAuthCompleteRegistrationDto> _oAuthCompleteRegistrationDtoValidator;
    private readonly IValidator<UpdateUserDto> _updateUserDtoValidator;
    private readonly IValidator<DeleteUserDto> _deleteUserDtoValidator;
    private readonly IValidator<SetRoleDto> _setRoleDtoValidator;
    private readonly ILogger<UserManager> _logger;

    public UserManager(ApplicationDbContext db, IPasswordHasher passwordHasher, IAvatarManager avatarManager, IOptions<AvatarManagerOptions> avatarManagerOptions, IValidator<User> userValidator, IValidator<CreateUserDto> createUserDtoValidator, IValidator<OAuthCompleteRegistrationDto> oAuthCompleteRegistrationDtoValidator, IValidator<UpdateUserDto> updateUserDtoValidator, IValidator<DeleteUserDto> deleteUserDtoValidator, IValidator<SetRoleDto> setRoleDtoValidator, ILogger<UserManager> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _avatarManagerOptions = avatarManagerOptions.Value;
        _userValidator = userValidator;
        _createUserDtoValidator = createUserDtoValidator;
        _oAuthCompleteRegistrationDtoValidator = oAuthCompleteRegistrationDtoValidator;
        _updateUserDtoValidator = updateUserDtoValidator;
        _deleteUserDtoValidator = deleteUserDtoValidator;
        _setRoleDtoValidator = setRoleDtoValidator;
        _avatarManager = avatarManager;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(Guid userId, bool tracking = true, CancellationToken ct = default)
    {
        if (tracking)
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
    }

    public async Task<User?> GetUserAsync(string username, bool tracking = true, CancellationToken ct = default)
    {
        if (tracking)
            return await _db.Users.FirstOrDefaultAsync(x => x.Username == username, ct);

        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, ct);
    }

    public async Task<ServiceResult<UserDto>> GetUserDtoAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден | создаём DTO на стороне базы
        var userDto = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.ToUserDto()).FirstOrDefaultAsync(ct);
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

    // Просто вспомогательный метод без бизнес логики, пока не используется
    public async Task UpdateUserAsync(User changedUser, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(changedUser);

        // Валидация модели
        var validationResult = await ValidateAsync(changedUser, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors));

        _db.Users.Update(changedUser);
        await _db.SaveChangesAsync(ct);
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
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Не обнаружено изменений
        if (userFromDb.Firstname == updateUserDto.Firstname &&
           userFromDb.Username == updateUserDto.Username &&
           userFromDb.LanguageCode == updateUserDto.LanguageCode)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        // Username уже занят
        if (updateUserDto.Username != userFromDb.Username && await IsUsernameAlreadyTakenAsync(updateUserDto.Username, ct)) // Если пользователь меняет username и такой username не свободен
            return ServiceResult.Fail(ErrorMessages.UsernameAlreadyTaken);

        userFromDb.Firstname = updateUserDto.Firstname;
        userFromDb.Username = updateUserDto.Username;
        userFromDb.LanguageCode = updateUserDto.LanguageCode;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

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

        // Пользователь не найден
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Проверка пароля
        if (!_passwordHasher.Verify(deleteUserDto.Password, userFromDb.HashedPassword))
            return ServiceResult.Fail(ErrorMessages.InvalidPassword);

        // Удаляем пользователя
        _db.Users.Remove(userFromDb);
        await _db.SaveChangesAsync(ct);

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

        // Пользователь не найден
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Удаляем пользователя
        _db.Users.Remove(userFromDb);
        await _db.SaveChangesAsync(ct);

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

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(user, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

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

        // Username уже занят, просто меняем на рандомный
        if (await IsUsernameAlreadyTakenAsync(createUserDto.Username, ct))
            createUserDto.Username = RandomDataGenerator.GenerateRandomUsername();

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
            Email = userInfo.Email,
            PhoneNumber = createUserDto.PhoneNumber,
        };

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(user, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        await _db.Users.AddAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        // Устанавливаем пользователю UserInfo аватарку
        var setAvatarResult = await _avatarManager.SetAvatarAsync(user.Id, await OAuthHelper.DownloadPictureAsync(userInfo.Picture), ct);

        // Не удалось установить аватарку, просто логируем. Если не получилось, то дефолтную так и оставим
        if (setAvatarResult.ErrorMessage != null)
            _logger.LogWarning("Не удалось установить аватарку, как из UserInfo. Причина: \"{error}\".", setAvatarResult.ErrorMessage);

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
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Не обнаружено изменений
        if (userFromDb.Role == setRoleDto.Role)
            return ServiceResult.Fail(ErrorMessages.NoChangesDetected);

        userFromDb.Role = setRoleDto.Role;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

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

    public async Task<bool> IsUsernameAlreadyTakenAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(x => x.Username == username, ct);
    }

    public async Task<bool> IsEmailAlreadyTakenAsync(string email, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(x => x.Email == email, ct);
    }

    public async Task<bool> IsPhoneNumberAlreadyTakenAsync(string phoneNumber, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(x => x.PhoneNumber == phoneNumber, ct);
    }

    public async Task<bool> IsUserExistsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(x => x.Id == userId, ct);
    }

    public async Task<bool> IsUserExistsAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users.AnyAsync(x => x.Username == username, ct);
    }

    public FluentValidation.Results.ValidationResult Validate(User user) => _userValidator.Validate(user);
    public async Task<FluentValidation.Results.ValidationResult> ValidateAsync(User user, CancellationToken ct = default) => await _userValidator.ValidateAsync(user, ct);

    public async Task<ServiceResult> ConfirmEmailAsync(string token, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(token);

        // Запрос не найден
        var confirmEmailRequestFromDb = await _db.ConfirmEmailRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == token, ct);
        if (confirmEmailRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Удаляем токен из базы (в любом случае надо удалить токен, он одноразовый)
        _db.ConfirmEmailRequests.Remove(confirmEmailRequestFromDb);
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (confirmEmailRequestFromDb.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Пользователь не найден
        var userFromDb = confirmEmailRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Электронная почта пользователя уже подтверждёна
        if (userFromDb.IsEmailConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedEmail);

        userFromDb.IsEmailConfirm = true;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        // Сохраняем изменения
        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

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
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == userId && x.Code == code, ct);
        if (verificationPhoneNumberRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidCode);

        // Удаляем код из базы (в любом случае надо удалить код, он одноразовый)
        _db.VerificationPhoneNumberRequests.Remove(verificationPhoneNumberRequestFromDb);
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (verificationPhoneNumberRequestFromDb.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidCode);

        // Пользователь не найден
        var userFromDb = verificationPhoneNumberRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Телефонный номер пользователя уже подтверждён
        if (userFromDb.IsPhoneNumberConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedPhoneNumber);

        userFromDb.IsPhoneNumberConfirm = true;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        // Сохраняем изменения
        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

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