using System.Security.Claims;

namespace CRUD.Services;

/// <inheritdoc cref="IAuthManager"/>
public class AuthManager : IAuthManager
{
    private readonly IUserManager _userManager;
    private readonly IValidator<LoginDataDto> _loginDataValidator;
    private readonly IValidator<CreateUserDto> _createUserDtoValidator;
    private readonly IValidator<OAuthCompleteRegistrationDto> _oAuthCompleteRegistrationDtoValidator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenManager _tokenManager;
    private readonly ApplicationDbContext _db;
    private readonly IConfirmEmailRequestManager _confirmEmailRequestManager;
    private readonly IVerificationPhoneNumberRequestManager _verificationPhoneNumberRequestManager;
    private readonly IAuthRefreshTokenManager _authRefreshTokenManager;

    public AuthManager(IUserManager userManager,
        IValidator<LoginDataDto> loginDataValidator,
        IValidator<CreateUserDto> createUserDtoValidator,
        IValidator<OAuthCompleteRegistrationDto> oAuthCompleteRegistrationDtoValidator,
        IPasswordHasher passwordHasher,
        ITokenManager tokenManager,
        ApplicationDbContext db,
        IConfirmEmailRequestManager confirmEmailRequestManager,
        IVerificationPhoneNumberRequestManager verificationPhoneNumberRequestManager,
        IAuthRefreshTokenManager authRefreshTokenManager)
    {
        _userManager = userManager;
        _loginDataValidator = loginDataValidator;
        _createUserDtoValidator = createUserDtoValidator;
        _oAuthCompleteRegistrationDtoValidator = oAuthCompleteRegistrationDtoValidator;
        _passwordHasher = passwordHasher;
        _tokenManager = tokenManager;
        _db = db;
        _confirmEmailRequestManager = confirmEmailRequestManager;
        _verificationPhoneNumberRequestManager = verificationPhoneNumberRequestManager;
        _authRefreshTokenManager = authRefreshTokenManager;
    }

    public async Task<ServiceResult<AuthJwtResponse>> LoginAsync(LoginDataDto loginData, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(loginData);

        // Валидация модели
        var validationResult = await _loginDataValidator.ValidateAsync(loginData, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(LoginDataDto), validationResult.Errors));

        // Пользователь не найден | Неверный логин или пароль
        // Дабы не раскрывать пользователей, всё трактуется, как "неверный логин или пароль"
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Username == loginData.Username).Select(x => new { x.Id, x.Username, x.Role, x.LanguageCode, x.IsPremium, x.HashedPassword }).FirstOrDefaultAsync(ct);
        if (userFromDb == null || !_passwordHasher.Verify(loginData.Password, userFromDb.HashedPassword))
            return ServiceResult<AuthJwtResponse>.Fail(ErrorMessages.InvalidLoginOrPassword);

        // Полезная информация, которая будет в JWT-токене
        Claim[] claims = CreateClaims(userFromDb.Id, userFromDb.Username, userFromDb.Role, userFromDb.LanguageCode, userFromDb.IsPremium);

        // Генерация токенов
        var authResponse = _tokenManager.GenerateAuthResponse(claims, loginData.Username);

        // Добавляем в базу Refresh-токен и удаляем старые
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authResponse.RefreshToken, userFromDb.Id, ct: ct);

        return ServiceResult<AuthJwtResponse>.Success(authResponse);
    }

    public async Task<ServiceResult<AuthJwtResponse>> LoginAsync(string refreshToken, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(refreshToken);

        // Токен не найден
        var tokenFromDb = await _db.AuthRefreshTokens.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Token == refreshToken, ct);
        if (tokenFromDb == null)
            return ServiceResult<AuthJwtResponse>.Fail(ErrorMessages.InvalidToken);

        // Пользователь не найден
        var userFromDb = tokenFromDb.User;
        if (userFromDb == null)
            return ServiceResult<AuthJwtResponse>.Fail(ErrorMessages.UserNotFound);

        // Проверка срока действия токена
        if (tokenFromDb.IsExpired())
            return ServiceResult<AuthJwtResponse>.Fail(ErrorMessages.InvalidToken);

        // Полезная информация, которая будет в JWT-токене
        Claim[] claims = CreateClaims(userFromDb);

        // Генерация токенов
        var authResponse = _tokenManager.GenerateAuthResponse(claims, userFromDb.Username);

        // Добавляем в базу Refresh-токен и удаляем старые и использованный
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authResponse.RefreshToken, userFromDb.Id, refreshToken, ct);

        return ServiceResult<AuthJwtResponse>.Success(authResponse);
    }

    public async Task<ServiceResult<AuthJwtResponse>> LoginAsync(OpenIdUserInfo userInfo, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(userInfo);

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Email == userInfo.Email).Select(x => new { x.Id, x.Username, x.Role, x.LanguageCode, x.IsPremium }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult<AuthJwtResponse>.Fail(ErrorMessages.UserNotFound);

        // Полезная информация, которая будет в JWT-токене
        Claim[] claims = CreateClaims(userFromDb.Id, userFromDb.Username, userFromDb.Role, userFromDb.LanguageCode, userFromDb.IsPremium);

        // Генерация токенов
        var authResponse = _tokenManager.GenerateAuthResponse(claims, userFromDb.Username);

        // Добавляем в базу Refresh-токен и удаляем старые
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authResponse.RefreshToken, userFromDb.Id, ct: ct);

        return ServiceResult<AuthJwtResponse>.Success(authResponse);
    }

    public async Task<ServiceResult<AuthJwtResponse>> RegisterAsync(CreateUserDto createUserDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(createUserDto);

        // Валидация модели
        var validationResult = await _createUserDtoValidator.ValidateAsync(createUserDto, ct);
        if (!validationResult.IsValid) // Эндпоинт должен предоставить валидную модель
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(CreateUserDto), validationResult.Errors));

        // Создание пользователя
        var result = await _userManager.CreateUserAsync(createUserDto, ct);

        if (result.ErrorMessage != null) // Есть ошибка
            return ServiceResult<AuthJwtResponse>.Fail(result.ErrorMessage);

        // Присваиваем результат сервиса
        var createdUser = result.Value!;

        // Добавляем токен в базу и отправляем письмо
        var resultAddTokenToDbAndSendLetter = await _confirmEmailRequestManager.AddTokenToDatabaseAndSendLetterAsync(createdUser.Id, createdUser.Email, createdUser.LanguageCode, ct);

        // Есть ошибка
        if (resultAddTokenToDbAndSendLetter.ErrorMessage != null)
            return ServiceResult<AuthJwtResponse>.Fail(resultAddTokenToDbAndSendLetter.ErrorMessage, resultAddTokenToDbAndSendLetter.ErrorParams);

        // Подтверждение телефона, решил отдельно, а то, что-то при регистрации сразу дудосим пользователя, и почту, и телефон
        // Да и к тому же, есть мыслить бизнесом, то СМС стоит денег, а пользователь может быть и не хочет пользоваться сервисом

        // Полезная информация, которая будет в JWT-токене
        Claim[] claims = CreateClaims(createdUser);

        // Генерация токенов
        var authResponse = _tokenManager.GenerateAuthResponse(claims, createdUser.Username);

        // Добавляем в базу Refresh-токен и удаляем старые (которых пока нет)
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authResponse.RefreshToken, createdUser.Id, ct: ct);

        return ServiceResult<AuthJwtResponse>.Success(authResponse);
    }

    public async Task<ServiceResult<AuthJwtResponse>> RegisterAsync(OpenIdUserInfo userInfo, OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(userInfo);
        ArgumentNullException.ThrowIfNull(oAuthCompleteRegistrationDto);

        // Валидация модели
        var validationResult = await _oAuthCompleteRegistrationDtoValidator.ValidateAsync(oAuthCompleteRegistrationDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(OAuthCompleteRegistrationDto), validationResult.Errors));

        // Создание пользователя
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto, ct);

        // Не удалось создать пользователя
        if (result.ErrorMessage != null)
            return ServiceResult<AuthJwtResponse>.Fail(result.ErrorMessage);

        // Присваиваем результат сервиса
        var createdUser = result.Value!;

        // Полезная информация, которая будет в JWT-токене
        Claim[] claims = CreateClaims(createdUser);

        // Генерация токенов
        var authResponse = _tokenManager.GenerateAuthResponse(claims, createdUser.Username);

        // Добавляем в базу Refresh-токен и удаляем старые (которых пока нет)
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authResponse.RefreshToken, createdUser.Id, ct: ct);

        return ServiceResult<AuthJwtResponse>.Success(authResponse);
    }

    public async Task<ServiceResult> SendConfirmEmailAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.IsEmailConfirm, x.Email, x.LanguageCode }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Электронная почта пользователя уже подтверждёна
        if (userFromDb.IsEmailConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedEmail);

        // Добавляем токен в базу и отправляем письмо
        var result = await _confirmEmailRequestManager.AddTokenToDatabaseAndSendLetterAsync(userId, userFromDb.Email, userFromDb.LanguageCode, ct);

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage, result.ErrorParams);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SendVerificationCodePhoneNumberAsync(Guid userId, bool isTelegram, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.IsPhoneNumberConfirm, x.PhoneNumber, x.LanguageCode }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Телефонный номер пользователя уже подтверждён
        if (userFromDb.IsPhoneNumberConfirm)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyConfirmedPhoneNumber);

        // Добавляем код подтверждения телефона в базу и отправляем СМС
        var result = await _verificationPhoneNumberRequestManager.AddCodeToDatabaseAndSendSmsAsync(userId, userFromDb.PhoneNumber, userFromDb.LanguageCode, isTelegram, ct);

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage, result.ErrorParams);

        return ServiceResult.Success();
    }

    private static Claim[] CreateClaims(User user)
    {
        // Полезная информация, которая будет в JWT-токене
        return
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("language_code", user.LanguageCode),
            new Claim("premium", user.IsPremium.ToString())
        ];
    }

    private static Claim[] CreateClaims(Guid userId, string username, string role, string languageCode, bool isPremium)
    {
        // Полезная информация, которая будет в JWT-токене
        return
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("language_code", languageCode),
            new Claim("premium", isPremium.ToString())
        ];
    }
}