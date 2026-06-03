namespace CRUD.Tests.UnitTests;

public sealed class UserManagerUnitTest
{
    private readonly UserManager _userManager;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IAvatarManager> _mockAvatarManager;
    private readonly Mock<IOptions<AvatarManagerOptions>> _mockAvatarManagerOptions;
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
            _mockCreateUserDtoValidator.Object,
            _mockOAuthCompleteRegistrationDtoValidator.Object,
            _mockUpdateUserDtoValidator.Object,
            _mockDeleteUserDtoValidator.Object,
            _mockSetRoleDtoValidator.Object,
            _mockLogger.Object
        );
    }

    [Fact] // Если AvatarManager GetPresignedUrlAvatarAsync возвращает ошибку (не UserNotFound), то возвращается DTO без аватарки
    public async Task GetUserDtoAsync_WhenGetPresignedUrlAvatarAsyncErrorNotUserNotFound_ReturnsUserDtoWithoutAvatarPresignedUrl()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;
        var expectedDto = new UserDto
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            AvatarPresignedUrl = null
        };

        // Не удалось получить аватарку
        _mockAvatarManager.Setup(x => x.GetPresignedUrlAvatarAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<string>.Fail("Some"));

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        Assert.Equivalent(expectedDto, result.Value);
    }

    [Fact] // Если AvatarManager GetPresignedUrlAvatarAsync возвращает ошибку (UserNotFound), то возвращается эта ошибка 
    public async Task GetUserDtoAsync_WhenGetPresignedUrlAvatarAsyncErrorUserNotFound_ReturnsServiceResult()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Не удалось получить аватарку - UserNotFound
        _mockAvatarManager.Setup(x => x.GetPresignedUrlAvatarAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<string>.Fail(ErrorMessages.UserNotFound));

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.NotNull(result.ErrorMessage);

        Assert.Equal(ErrorMessages.UserNotFound, result.ErrorMessage);
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Успешная валидация OAuthCompleteRegistrationDto
        _mockOAuthCompleteRegistrationDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<OAuthCompleteRegistrationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Успешная валидация CreateUserDto
        _mockCreateUserDtoValidator.Setup(x => x.ValidateAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Успешная генерация хэшированного пароля
        _mockPasswordHasher.Setup(x => x.GenerateHashedPassword(It.IsAny<string>())).Returns(TestConstants.UserHashedPassword);

        // Не удалось установить аватарку
        _mockAvatarManager.Setup(x => x.SetAvatarAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.DoesNotMatchSignature));

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email, TestContext.Current.CancellationToken);
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
}