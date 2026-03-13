using System.Security.Claims;

namespace CRUD.Tests.UnitTests;

public class AuthManagerUnitTest
{
    private readonly AuthManager _authManager;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly Mock<IValidator<LoginDataDto>> _mockLoginDataValidator;
    private readonly Mock<IValidator<CreateUserDto>> _mockCreateUserDtoValidator;
    private readonly Mock<IValidator<OAuthCompleteRegistrationDto>> _mockOAuthCompleteRegistrationDtoValidator;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenManager> _mockTokenManager;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IConfirmEmailRequestManager> _mockConfirmEmailRequestManager;
    private readonly Mock<IVerificationPhoneNumberRequestManager> _mockVerificationPhoneNumberRequestManager;
    private readonly Mock<IAuthRefreshTokenManager> _mockAuthRefreshTokenManager;

    public AuthManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockUserManager = new();
        _mockLoginDataValidator = new();
        _mockCreateUserDtoValidator = new();
        _mockOAuthCompleteRegistrationDtoValidator = new();
        _mockPasswordHasher = new();
        _mockTokenManager = new();
        _mockConfirmEmailRequestManager = new();
        _mockVerificationPhoneNumberRequestManager = new();
        _mockAuthRefreshTokenManager = new();

        _authManager = new AuthManager(
            _mockUserManager.Object,
            _mockLoginDataValidator.Object,
            _mockCreateUserDtoValidator.Object,
            _mockOAuthCompleteRegistrationDtoValidator.Object,
            _mockPasswordHasher.Object,
            _mockTokenManager.Object,
            db,
            _mockConfirmEmailRequestManager.Object,
            _mockVerificationPhoneNumberRequestManager.Object,
            _mockAuthRefreshTokenManager.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken()
    {
        // Arrange
        var username = "test";
        var password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, username: username);

        // Модель входа
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Валидация проходит
        _mockLoginDataValidator.Setup(x => x.ValidateAsync(It.IsAny<LoginDataDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Пароль подходит
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Возврат токена
        var authJwtResponse = new AuthJwtResponse() { AccessToken = "token", Username = "username", RefreshToken = "token", Expires = DateTime.UtcNow };
        _mockTokenManager.Setup(x => x.GenerateAuthResponse(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>())).Returns(authJwtResponse);

        // Act
        var result = await _authManager.LoginAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value); // Не пустой токен
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);
    }

    [Fact]
    public async Task LoginAsync_WhenNotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var username = "test";
        var password = "123";
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Модель невалидна
        _mockLoginDataValidator.Setup(x => x.ValidateAsync(It.IsAny<LoginDataDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult() { Errors = [new()] });

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.LoginAsync(loginData);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsErrorMessage_InvalidLoginOrPassword()
    {
        // Arrange
        var username = "test";
        var password = "123";

        // Модель входа
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Валидация проходит
        _mockLoginDataValidator.Setup(x => x.ValidateAsync(It.IsAny<LoginDataDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _authManager.LoginAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.InvalidLoginOrPassword, result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidPassword_ReturnsErrorMessage_InvalidLoginOrPassword()
    {
        // Arrange
        var username = "test";
        var password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, username: username);

        // Модель входа
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Валидация проходит
        _mockLoginDataValidator.Setup(x => x.ValidateAsync(It.IsAny<LoginDataDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Пароль не подходит
        _mockPasswordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = await _authManager.LoginAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.InvalidLoginOrPassword, result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        LoginDataDto loginData = null;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.LoginAsync(loginData);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Equal(nameof(loginData), ex.ParamName);
    }


    [Fact]
    public async Task LoginAsyncByRefreshToken_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string refreshToken = null;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.LoginAsync(refreshToken);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Equal(nameof(refreshToken), ex.ParamName);
    }


    [Fact]
    public async Task LoginAsyncByUserInfo_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        OpenIdUserInfo userInfo = null;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.LoginAsync(userInfo);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Equal(nameof(userInfo), ex.ParamName);
    }


    [Fact]
    public async Task RegisterAsync_ReturnsToken()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber);

        // Модель регистрации
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Валидация проходит
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Успешное создание пользователя
        _mockUserManager.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<User>.Success(user));

        // Успешное добавление токена в базу и отправка письма
        _mockConfirmEmailRequestManager.Setup(x => x.AddTokenToDatabaseAndSendLetterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Возврат токена
        var authJwtResponse = new AuthJwtResponse() { AccessToken = "token", Username = "username", RefreshToken = "token", Expires = DateTime.UtcNow };
        _mockTokenManager.Setup(x => x.GenerateAuthResponse(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>())).Returns(authJwtResponse);

        // Act
        var result = await _authManager.RegisterAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);
    }

    [Fact]
    public async Task RegisterAsync_WhenNotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";

        // Модель регистрации
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Валидация не проходит
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult() { Errors = [new ValidationFailure()] });

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.RegisterAsync(createUserDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }

    [Fact]
    public async Task RegisterAsync_WhenFailCreateUser_ReturnsErrorMessage_UsernameAlreadyTaken()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber);

        // Модель регистрации
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Валидация проходит
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Ошибка при создании пользователя (Username уже занят)
        _mockUserManager.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<User>.Fail(ErrorMessages.UsernameAlreadyTaken));

        // Act
        var result = await _authManager.RegisterAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.UsernameAlreadyTaken, result.ErrorMessage);
    }

    [Fact] // Письмо уже отправлено (таймаут)
    public async Task RegisterAsync_WhenRequestAlreadyExists_ReturnsErrorMessage_LetterAlreadySent()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber);

        // Модель регистрации
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Валидация проходит
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Успешное создание пользователя
        _mockUserManager.Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<User>.Success(user));

        // Письмо уже отправлено
        _mockConfirmEmailRequestManager.Setup(x => x.AddTokenToDatabaseAndSendLetterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.LetterAlreadySent));

        // Act
        var result = await _authManager.RegisterAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.LetterAlreadySent, result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        CreateUserDto createUserDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.RegisterAsync(createUserDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(createUserDto), ex.ParamName);
    }


    [Fact]
    public async Task RegisterAsyncByUserInfoAndOAuthCompleteRegistrationDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        OpenIdUserInfo userInfo = null;
        OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Equal(nameof(userInfo), ex.ParamName);
    }

    [Fact]
    public async Task RegisterAsyncByUserInfoAndOAuthCompleteRegistrationDto_WhenNotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "",
            Name = "",
            GivenName = "",
            FamilyName = "",
            Nickname = "",
            Picture = "",
            Gender = "",
            Birthdate = DateTime.Now,
            Locale = "",
            Email = ""
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto 
        { 
            PhoneNumber = ""
        };

        // Валидация не проходит
        _mockOAuthCompleteRegistrationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<OAuthCompleteRegistrationDto>(), default)).ReturnsAsync(new ValidationResult() { Errors = [new ValidationFailure()] });

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }


    [Fact]
    public async Task SendConfirmEmailAsync_ReturnsServiceResult()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool emailConfirm = false;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isEmailConfirm: emailConfirm);
        var userIdGuid = user.Id;

        // Успешное добавление токена в базу и отправка письма
        _mockConfirmEmailRequestManager.Setup(x => x.AddTokenToDatabaseAndSendLetterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Act
        var result = await _authManager.SendConfirmEmailAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _authManager.SendConfirmEmailAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_WhenUserAlreadyConfirmedEmail_ReturnsErrorMessage_UserAlreadyConfirmedEmail()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool emailConfirm = true;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isEmailConfirm: emailConfirm);
        var userIdGuid = user.Id;

        // Act
        var result = await _authManager.SendConfirmEmailAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedEmail, result.ErrorMessage);
    }

    [Fact] // Письмо уже отправлено (таймаут)
    public async Task SendConfirmEmailAsync_WhenRequestAlreadyExists_ReturnsErrorMessage_LetterAlreadySent()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool emailConfirm = false;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isEmailConfirm: emailConfirm);
        var userIdGuid = user.Id;

        // Письмо уже отправлено
        _mockConfirmEmailRequestManager.Setup(x => x.AddTokenToDatabaseAndSendLetterAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.LetterAlreadySent));

        // Act
        var result = await _authManager.SendConfirmEmailAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.LetterAlreadySent, result.ErrorMessage);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_WhenEmptyGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.SendConfirmEmailAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task SendVerificationCodePhoneNumberAsync_ReturnsServiceResult()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool emailConfirm = false;
        bool isTelegram = true;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isEmailConfirm: emailConfirm);
        var userIdGuid = user.Id;

        // Успешное добавление кода в базу и отправка кода
        _mockVerificationPhoneNumberRequestManager.Setup(x => x.AddCodeToDatabaseAndSendSmsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Act
        var result = await _authManager.SendVerificationCodePhoneNumberAsync(userIdGuid, isTelegram);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendVerificationCodePhoneNumberAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
        bool isTelegram = true;

        // Act
        var result = await _authManager.SendVerificationCodePhoneNumberAsync(userIdGuid, isTelegram);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SendVerificationCodePhoneNumberAsync_WhenUserAlreadyConfirmedPhoneNumber_ReturnsErrorMessage_UserAlreadyConfirmedPhoneNumber()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool phoneNumberConfirm = true;
        bool isTelegram = true;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isPhoneNumberConfirm: phoneNumberConfirm);
        var userIdGuid = user.Id;

        // Act
        var result = await _authManager.SendVerificationCodePhoneNumberAsync(userIdGuid, isTelegram);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedPhoneNumber, result.ErrorMessage);
    }

    [Fact] // Код уже отправлен (таймаут)
    public async Task SendVerificationCodePhoneNumberAsync_WhenRequestAlreadyExists_ReturnsErrorMessage_CodeAlreadySent()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "123";
        string languageCode = "ru";
        string email = "test@test.ru";
        string phoneNumber = "12345678";
        bool phoneNumberConfirm = false;
        bool isTelegram = true;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, hashedPassword: password, username: username, languageCode: languageCode, email: email, phoneNumber: phoneNumber, isPhoneNumberConfirm: phoneNumberConfirm);
        var userIdGuid = user.Id;

        // Письмо уже отправлено
        _mockVerificationPhoneNumberRequestManager.Setup(x => x.AddCodeToDatabaseAndSendSmsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.CodeAlreadySent));

        // Act
        var result = await _authManager.SendVerificationCodePhoneNumberAsync(userIdGuid, isTelegram);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.CodeAlreadySent, result.ErrorMessage);
    }

    [Fact]
    public async Task SendVerificationCodePhoneNumberAsync_WhenEmptyGuid_ThrowsInvalidOperationException()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);
        bool isTelegram = true;

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.SendVerificationCodePhoneNumberAsync(userIdGuid, isTelegram);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }
}