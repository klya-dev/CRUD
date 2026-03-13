#nullable disable

namespace CRUD.Tests.UnitTests;

public class UserManagerUnitTest
{
    // #nullable disable

    private readonly UserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IAvatarManager> _mockAvatarManager;
    private readonly Mock<IOptions<AvatarManagerOptions>> _mockAvatarManagerOptions;
    private readonly Mock<IValidator<User>> _mockUserValidator;
    private readonly Mock<IValidator<CreateUserDto>> _mockCreateUserDtoValidator;
    private readonly Mock<IValidator<OAuthCompleteRegistrationDto>> _mockOAuthCompleteRegistrationDtoValidator;
    private readonly Mock<IValidator<UpdateUserDto>> _mockUpdateUserDtoValidator;
    private readonly Mock<IValidator<DeleteUserDto>> _mockDeleteUserDtoValidator;
    private readonly Mock<IValidator<SetRoleDto>> _mockSetRoleDtoValidator;
    private readonly Mock<ILogger<UserManager>> _mockLogger;

    public UserManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockPasswordHasher = new();
        _mockAvatarManager = new();
        _mockAvatarManagerOptions = new();
        _mockUserValidator = new();
        _mockCreateUserDtoValidator = new();
        _mockOAuthCompleteRegistrationDtoValidator = new();
        _mockUpdateUserDtoValidator = new();
        _mockDeleteUserDtoValidator = new();
        _mockSetRoleDtoValidator = new();
        _mockLogger = new();

        _mockAvatarManagerOptions.Setup(x => x.Value).Returns(TestSettingsHelper.GetConfigurationValue<AvatarManagerOptions, TestMarker>(AvatarManagerOptions.SectionName));

        _userManager = new UserManager(
            db,
            _mockPasswordHasher.Object,
            _mockAvatarManager.Object,
            _mockAvatarManagerOptions.Object,
            _mockUserValidator.Object,
            _mockCreateUserDtoValidator.Object,
            _mockOAuthCompleteRegistrationDtoValidator.Object,
            _mockUpdateUserDtoValidator.Object,
            _mockDeleteUserDtoValidator.Object,
            _mockSetRoleDtoValidator.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUserDtoAsync_NotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.GetUserDtoAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task GetUserFullDtoAsync_NotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.GetUserFullDtoAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task UpdateUserAsyncByUpdateUserDto_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        string firstname = "firstname";
        string username = "username";
        string languageCode = "languageCode";

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task UpdateUserAsyncByUpdateUserDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        UpdateUserDto updateUserDto = null;
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(updateUserDto), ex.ParamName);
    }


    [Fact]
    public async Task DeleteUserAsync_NotValidData_ThrowsInvalidOperationException_EmptyGuid()
    {
        // Arrange
        string password = "12345";
        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        DeleteUserDto deleteUserDto = null;
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(deleteUserDto), ex.ParamName);
    }


    [Fact]
    public async Task DeleteUserWithoutDtoAsync_NotValidData_ThrowsInvalidOperationException_EmptyGuid()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.DeleteUserAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task CreateUserAsyncByCreateUserDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        CreateUserDto createUserDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(createUserDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(createUserDto), ex.ParamName);
    }


    [Fact]
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        OpenIdUserInfo userInfo = null;
        OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto = null;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(userInfo), ex.ParamName);
    }

    [Fact]
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_WhenPictureIsNotValid_ShouldDefaultAvatar()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
            // Замокаем неудачу
            Picture = "https://filin.mail.ru/pic?d=LHWIAVI9Bmqq-UAzSRq6yA1J_o-rvlv1PSR85MXdulxodK9yOvgAj89nM5bITfA~&name=%D1%84%D0%B0%D0%BD%D1%82%D0%BE%D0%BC+%D0%B0%D1%81%D1%81%D1%81%D0%B8%D0%BD",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "ru",
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Успешная валидация OAuthCompleteRegistrationDto
        _mockOAuthCompleteRegistrationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<OAuthCompleteRegistrationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Успешная валидация CreateUserDto
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Успешная генерация хэшированного пароля
        _mockPasswordHasher.Setup(x => x.GenerateHashedPassword(It.IsAny<string>())).Returns(TestConstants.UserHashedPassword);

        // Успешная валидация User
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Не удалось установить аватарку
        _mockAvatarManager.Setup(x => x.SetAvatarAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.DoesNotMatchSignature));

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email);
        Assert.NotNull(userFromDbAfterCreate);

        Assert.Equal(userInfo.GivenName, userFromDbAfterCreate.Firstname);
        Assert.StartsWith(userInfo.Nickname, userFromDbAfterCreate.Username);
        Assert.NotNull(userFromDbAfterCreate.HashedPassword);
        Assert.Equal(userInfo.Locale, userFromDbAfterCreate.LanguageCode);
        Assert.Equal(UserRoles.User, userFromDbAfterCreate.Role);
        Assert.False(userFromDbAfterCreate.IsPremium);
        Assert.Equal(userInfo.Email, userFromDbAfterCreate.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, userFromDbAfterCreate.PhoneNumber);
        Assert.Equal(TestConstants.DefaultAvatarPath, userFromDbAfterCreate.AvatarURL); // Дефолтная аватарка
    }


    [Fact]
    public async Task SetRoleAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        string role = UserRoles.Admin;

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task SetRoleAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        SetRoleDto setRoleDto = null;
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(setRoleDto), ex.ParamName);
    }


    [Fact]
    public async Task RevokeRefreshTokensAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        Guid userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.RevokeRefreshTokensAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task ConfirmEmailAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string token = null;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.ConfirmEmailAsync(token);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(token), ex.ParamName);
    }


    [Fact]
    public async Task VerificatePhoneNumberAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Guid userIdGuid = Guid.NewGuid();
        string code = null;

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.VerificatePhoneNumberAsync(userIdGuid, code);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(code), ex.ParamName);
    }

    [Fact]
    public async Task VerificatePhoneNumberAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        Guid userIdGuid = Guid.Empty;
        string code = "123456";

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.VerificatePhoneNumberAsync(userIdGuid, code);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Theory] // Корректные данные, но прямо перед записью в базу должно выбросится исключение, о том, что User невалидный (в интеграционном тесте такое провернуть не получится)
    [InlineData("Никита", "niksuper", "123@", "ru", "fan.ass95@mail.ru", "12345")]
    public async Task CreateUserAsyncByCreateUserDto_CorrectData_ThrowsInvalidOperationException_NotValidBeforeCreate(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto
        { 
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Нет ошибок
        var validationResultCreateUserDto = new ValidationResult();

        // Какие-то ошибки
        var validationResultUser = new ValidationResult()
        {
            Errors =
            [
                new ValidationFailure("PropertyName", "ErrorMessage")
            ]
        };

        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(createUserDto, default)).ReturnsAsync(validationResultCreateUserDto);
        //_mockDb.Setup(x => x.Users.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), default)).ReturnsAsync(true); // Возвращаем true на любой предикат метода Users.AnyAsync (Чтобы IsUsernameAlreadyTakenAsync вернул true)
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), default)).ReturnsAsync(validationResultUser); // Возвращаем ошибки валидации на любого User'а, который впихивается в ValidateAsync

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(createUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors), ex.Message);
    }
}