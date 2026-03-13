#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class UserManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IUserManager _userManager;
    private readonly IS3Manager _s3Manager;
    private readonly IAvatarManager _avatarManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ApplicationDbContext _db;

    public UserManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _userManager = scopedServices.GetRequiredService<IUserManager>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
        _avatarManager = scopedServices.GetRequiredService<IAvatarManager>();
        _passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    private IUserManager GenerateNewUserManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IUserManager>();
    }


    [Fact] // Корректные данные
    public async Task GetUserAsync_ReturnsUser()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.GetUserAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Equivalent(user, result);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.GetUserAsync(userIdGuid);

        // Assert
        Assert.Null(result);
    }


    [Fact] // Корректные данные
    public async Task GetUserDtoAsync_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;
        var userDtoFromDb = new UserDto
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode
        };

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        Assert.Equivalent(userDtoFromDb, result.Value);
    }

    [Fact]
    public async Task GetUserDtoAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task GetUserFullDtoAsync_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;
        var expectedDto = new UserFullDto
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            Role = user.Role,
            IsPremium = user.IsPremium,
            ApiKey = user.ApiKey,
            DisposableApiKey = user.DisposableApiKey,
            AvatarURL = user.AvatarURL,
            Email = user.Email,
            IsEmailConfirm = user.IsEmailConfirm,
            PhoneNumber = user.PhoneNumber,
            IsPhoneNumberConfirm = user.IsPhoneNumberConfirm
        };

        // Act
        var result = await _userManager.GetUserFullDtoAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        Assert.Equivalent(expectedDto, result.Value);
    }

    [Fact]
    public async Task GetUserFullDtoAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.GetUserFullDtoAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Theory] // Корректные данные
    [InlineData("новоеИмя", "newusername", "nn")]
    [InlineData("Кля", "username", "en")] // Меняем всё кроме username'а
    public async Task UpdateUserAsyncByUpdateUserDto_ReturnsServiceResult(string newFirstname, string newUsername, string newLanguageCode)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Такой User должен быть после обновления
        var mustUserFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        mustUserFromDbAfterUpdate.Firstname = updateUserDto.Firstname;
        mustUserFromDbAfterUpdate.Username = updateUserDto.Username;
        mustUserFromDbAfterUpdate.LanguageCode = updateUserDto.LanguageCode;

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Все поля совпадают, кроме RowVersion, но RowVersion также должен пройти проверку на null
        AssertExtensions.EqualIgnoring(userFromDbAfterUpdate, mustUserFromDbAfterUpdate, (user) =>
        {
            Assert.NotNull(user.RowVersion);
        }, nameof(userFromDbAfterUpdate.RowVersion));
    }

    [Theory]
    [InlineData("новоеImya", "юзернейм", "ru")] // Имя и Username невалидные
    [InlineData(null, null, null)] // Пустые данные
    public async Task UpdateUserAsyncByUpdateUserDto_NotValidData_ThrowsInvalidOperationException(string firstname, string username, string languageCode)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new UpdateUserDtoValidator(validatorsLocalizer).ValidateAsync(updateUserDto);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(UpdateUserDto), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task UpdateUserAsyncByUpdateUserDto_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        string firstname = "новоеИмя";
        string username = "newusername";
        string languageCode = "nn";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: "НЕВАЛИДНАЯ РОЛЬ");

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;

        // Результат валидации (о том, что роль невалидна)
        var validationResult = await new UserValidator().ValidateAsync(user);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился (после манипуляций с ролью)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdateUserAsyncByUpdateUserDto_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string firstname = "новоеИмя";
        string username = "newusername";
        string languageCode = "nn";

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Пользователя и вправду не существует
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdateUserAsyncByUpdateUserDto_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string firstname = "новоеИмя";
        string username = "newusername";
        string languageCode = "nn";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, firstname: firstname, username: username, languageCode: languageCode);

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task UpdateUserAsyncByUpdateUserDto_ReturnsErrorMessage_UsernameAlreadyTaken()
    {
        // Arrange
        string firstname = "новоеИмя";
        string username = "newusername";
        string languageCode = "nn";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: "username");

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, username: username, email: "test", phoneNumber: "1234567");

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UsernameAlreadyTaken, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }


    [Fact]
    public async Task DeleteUserAsync_ReturnsServiceResult()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду не удалилась, т.к дефолтная
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenAvatarIsNotDefault_ReturnsServiceResult()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }

    [Theory]
    [InlineData("")] // Пустые данные
    [InlineData(null)] // Пустые данные
    public async Task DeleteUserAsync_NotValidData_ThrowsInvalidOperationException(string password)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new DeleteUserDtoValidator(validatorsLocalizer).ValidateAsync(deleteUserDto);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(DeleteUserDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string password = "123";

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteUserAsync_ReturnsErrorMessage_InvalidPassword()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = "12345"
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidPassword, result.ErrorMessage);

        // Пользователь всё ещё существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.NotNull(userFromDbAfterDelete);
    }


    [Fact]
    public async Task DeleteUserWithoutDtoAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду не удалилась, т.к дефолтная
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }

    [Fact]
    public async Task DeleteUserWithoutDtoAsync_WhenAvatarIsNotDefault_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream);

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }

    [Fact]
    public async Task DeleteUserWithoutDto_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Theory] // Корректные данные
    [InlineData("Никита", "niksuper", "123@", "ru", "fan.ass995@mail.ru", "912345")]
    public async Task CreateUserAsyncByCreateUserDto_ReturnsServiceResult(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
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

        // Такой пользователь должен быть
        var mustUser = new User
        {
            Firstname = createUserDto.Firstname,
            Username = createUserDto.Username,
            HashedPassword = _passwordHasher.GenerateHashedPassword(createUserDto.Password),
            LanguageCode = createUserDto.LanguageCode,
            Role = UserRoles.User,
            IsPremium = false,
            AvatarURL = TestConstants.DefaultAvatarPath,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _userManager.CreateUserAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == createUserDto.Username);
        Assert.NotNull(userFromDbAfterCreate);

        // У пользователя, который должен создаться и, у пользователя который создался поля равны, кроме Id, HashedPassword, RowVersion, но эти игнорируемые поля должы быть не пустыми, кроме RowVersion
        AssertExtensions.EqualIgnoring(mustUser, userFromDbAfterCreate, (result) =>
        {
            if (result.Id == Guid.Empty)
                Assert.Fail(nameof(result.Id) + "is empty");
            Assert.NotNull(result.HashedPassword);
            // RowVersion для mustUser null, а для userFromDbAfterCreate не null, поэтому проверка на null, тут не поможет
        }, nameof(mustUser.Id), nameof(mustUser.HashedPassword), nameof(mustUser.RowVersion));
    }

    [Theory]
    [InlineData("Имя", "user@name", "password123", "eng", "fan.ass95@mail.ru", "12345")] // Username, язык и роль невалидные
    [InlineData(null, null, "null", null, "fan.ass95@mail.ru", "12345")] // Пустые данные, кроме пароля, т.к GenerateHashedPassword выбросит исключение 
    public async Task CreateUserAsyncByCreateUserDto_NotValidData_ThrowsInvalidOperationException(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
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
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new CreateUserDtoValidator(validatorsLocalizer).ValidateAsync(createUserDto);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(createUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(CreateUserDto), validationResult.Errors), ex.Message);

        // Пользователь не создан
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == createUserDto.Username);
        Assert.Null(userFromDbAfterCreate);
    }

    // Тест, где прямо перед записью в базу выбрасывается исключение о невалидной моделью, выполнен в юнит-тесте

    [Fact]
    public async Task CreateUserAsyncByCreateUserDto_ReturnsErrorMessage_UsernameAlreadyTaken()
    {
        // Arrange
        string firstname = "Кля";
        string username = "username";
        string password = "12345";
        string languageCode = "ru";
        string email = "test2@mail.ru";
        string phoneNumber = "123456789";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username);

        var createUserDto = new CreateUserDto
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _userManager.CreateUserAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UsernameAlreadyTaken, result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUserAsyncByCreateUserDto_ReturnsErrorMessage_EmailAlreadyTaken()
    {
        // Arrange
        string firstname = "Кля";
        string username = "some";
        string password = "12345";
        string languageCode = "ru";
        string email = "test2@mail.ru";
        string phoneNumber = "123456789";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, email: email);

        var createUserDto = new CreateUserDto
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _userManager.CreateUserAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.EmailAlreadyTaken, result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUserAsyncByCreateUserDto_ReturnsErrorMessage_PhoneNumberAlreadyTaken()
    {
        // Arrange
        string firstname = "Кля";
        string username = "some";
        string password = "12345";
        string languageCode = "ru";
        string email = "test2@mail.ru";
        string phoneNumber = "123456789";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, phoneNumber: phoneNumber);

        var createUserDto = new CreateUserDto
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _userManager.CreateUserAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PhoneNumberAlreadyTaken, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_ReturnsServiceResult()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
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
        Assert.NotEqual(TestConstants.DefaultAvatarPath, userFromDbAfterCreate.AvatarURL); // Не дефолтная аватарка
    }

    [Theory]
    [InlineData("phone")]
    [InlineData(null)] // Пустые данные
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_NotValidData_ThrowsInvalidOperationException(string phoneNumber)
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "фантом ассасин",
            Picture = "https://filin.mail.ru/pic?d=LHWIAVI9Bmqq-UAzSRq6yA1J_o-rvlv1PSR85MXdulxodK9yOvgAj89nM5bITfA~&name=%D1%84%D0%B0%D0%BD%D1%82%D0%BE%D0%BC+%D0%B0%D1%81%D1%81%D1%81%D0%B8%D0%BD",
            Gender = "male",
            Birthdate = DateTime.Now,
            Locale = "ru",
            Email = "some@some.some"
        };

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = phoneNumber
        };
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new OAuthCompleteRegistrationDtoValidator(validatorsLocalizer).ValidateAsync(oAuthCompleteRegistrationDto);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(OAuthCompleteRegistrationDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_WhenUsernameAlreadyTaken_ReturnsServiceResult()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
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
        var user = await DI.CreateUserAsync(_db, username: userInfo.Nickname);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email);
        Assert.NotNull(userFromDbAfterCreate);

        Assert.Equal(userInfo.GivenName, userFromDbAfterCreate.Firstname);
        Assert.StartsWith("und-", userFromDbAfterCreate.Username); // Username рандомный
        Assert.NotNull(userFromDbAfterCreate.HashedPassword);
        Assert.Equal(userInfo.Locale, userFromDbAfterCreate.LanguageCode);
        Assert.Equal(UserRoles.User, userFromDbAfterCreate.Role);
        Assert.False(userFromDbAfterCreate.IsPremium);
        Assert.Equal(userInfo.Email, userFromDbAfterCreate.Email);
        Assert.Equal(oAuthCompleteRegistrationDto.PhoneNumber, userFromDbAfterCreate.PhoneNumber);
        Assert.NotEqual(TestConstants.DefaultAvatarPath, userFromDbAfterCreate.AvatarURL); // Не дефолтная аватарка
    }

    [Fact]
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_ReturnsErrorMessage_EmailAlreadyTaken()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
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
        var user = await DI.CreateUserAsync(_db, email: userInfo.Email);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.EmailAlreadyTaken, result.ErrorMessage);
    }

    [Fact]
    public async Task CreateUserAsyncByUserInfoAndOAuthCompleteRegistrationDto_ReturnsErrorMessage_PhoneNumberAlreadyTaken()
    {
        // Arrange
        var userInfo = new OpenIdUserInfo
        {
            Sub = "123",
            Name = "фантом ассасин",
            GivenName = "фантом",
            FamilyName = "ассасин",
            Nickname = "assasin",
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
        var user = await DI.CreateUserAsync(_db, phoneNumber: oAuthCompleteRegistrationDto.PhoneNumber);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PhoneNumberAlreadyTaken, result.ErrorMessage);
    }

    // WhenPictureIsNotValid_ShouldDefaultAvatar в Unit-тесте


    [Theory] // Корректные данные
    [InlineData(UserRoles.Admin)]
    public async Task SetRoleAsync_ReturnsServiceResult(string role)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };

        // Такой User должен быть после обновления
        var mustUserFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        mustUserFromDbAfterUpdate.Role = role;

        // Act
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Все поля совпадают, кроме RowVersion, но RowVersion также должен пройти проверку на null
        AssertExtensions.EqualIgnoring(userFromDbAfterUpdate, mustUserFromDbAfterUpdate, (user) =>
        {
            Assert.NotNull(user.RowVersion);
        }, nameof(userFromDbAfterUpdate.RowVersion));
    }

    [Theory]
    [InlineData("something")] // Роль невалидна
    [InlineData(null)] // Пустые данные
    public async Task SetRoleAsync_NotValidData_ThrowsInvalidOperationException(string role)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new SetRoleDtoValidator(validatorsLocalizer).ValidateAsync(setRoleDto);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(SetRoleDto), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task SetRoleAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        string role = UserRoles.Admin;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: "НЕВАЛИДНЫЙ ЮЗЕРНЕЙМ");

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = user.Id;

        // Результат валидации (о том, что username невалиден)
        var validationResult = await new UserValidator().ValidateAsync(user);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился (после манипуляций с username)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task SetRoleAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        string role = UserRoles.Admin;

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Пользователя и вправду не существует
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Null(userFromDbAfterUpdate);
    }

    [Fact]
    public async Task SetRoleAsync_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string role = UserRoles.Admin;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: role);

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }


    [Fact]
    public async Task RevokeRefreshTokensAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid);
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "12133244");

        // Act
        var result = await _userManager.RevokeRefreshTokensAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Все токены пользователя удалены
        var countAuthRefreshTokens = await _db.AuthRefreshTokens.Where(x => x.UserId == userIdGuid).CountAsync();
        Assert.Equal(0, countAuthRefreshTokens);
    }

    [Fact]
    public async Task RevokeRefreshTokensAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.RevokeRefreshTokensAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task ConfirmEmailAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Почта и вправду подтвердилась
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id  == userIdGuid);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsEmailConfirm);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task ConfirmEmailAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: "НЕВАЛИДНАЯ РОЛЬ");

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Результат валидации (о том, что роль невалидна)
        var validationResult = await new UserValidator().ValidateAsync(user);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился (после манипуляций с ролью)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact] // Неверный токен
    public async Task ConfirmEmailAsync_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        string token = "something";

        // Act
        var result = await _userManager.ConfirmEmailAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact] // Неверный токен (т.к мы удалим пользователя). У токена не может быть несуществующего пользователя. Запрос автоматически удалится
    public async Task ConfirmEmailAsync_WhenUserDeleted_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsErrorMessage_UserAlreadyConfirmedEmail()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedEmail, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task VerificatePhoneNumberAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Номер и вправду подтвердился
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsPhoneNumberConfirm);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task VerificatePhoneNumberAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: "НЕВАЛИДНАЯ РОЛЬ");

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Результат валидации (о том, что роль невалидна)
        var validationResult = await new UserValidator().ValidateAsync(user);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился (после манипуляций с ролью)
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact] // Неверный код
    public async Task VerificatePhoneNumberAsync_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
        string code = "1234";

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, code);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidCode, result.ErrorMessage);
    }

    [Fact] // Неверный код, если авторизованный пользователь, пытается выдать себя за владельца кода
    public async Task VerificatePhoneNumberAsync_WhenRequestSendAnotherUser_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@test.test", phoneNumber: "123456789");

        // Добавляем токен в базу. Владелец этого токена первый пользователь
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id);

        // Act
        // Запрос делает второй пользователь (не владелец) с таким же кодом
        var result = await _userManager.VerificatePhoneNumberAsync(user2.Id, verificationPhoneNumberRequest.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidCode, result.ErrorMessage);
    }

    [Fact] // Неверный код (т.к мы удалим пользователя). У кода не может быть несуществующего пользователя
    public async Task VerificatePhoneNumberAsync_WhenUserDeleted_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        // Запрос тоже должен удалиться вместе с пользователем
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.FirstOrDefaultAsync(x => x.Id == verificationPhoneNumberRequest.Id);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidCode, result.ErrorMessage);

        // Запроса тоже нет, как и пользователя
        Assert.Null(verificationPhoneNumberRequestFromDb);
    }

    [Fact] // У этого пользователя уже подтвержден телефонный номер
    public async Task VerificatePhoneNumberAsync_ReturnsErrorMessage_UserAlreadyConfirmedPhoneNumber()
    {
        // Arrange
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedPhoneNumber, result.ErrorMessage);
    }


    // Конфликты параллельности


    [Fact] // Корректные данные
    public async Task GetUserAsync_ConcurrencyConflict_ReturnsUser()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        var userFromDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.GetUserAsync(userIdGuid);
        var task2 = userManager2.GetUserAsync(userIdGuid);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result); // Так-то userFromDb тоже может быть null и Equivalent пройдёт
        Assert.Equivalent(userFromDb, result);

        Assert.Equivalent(result, result2);
    }

    [Fact]
    public async Task GetUserAsync_ConcurrencyConflict_ReturnsNull()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.GetUserAsync(userIdGuid);
        var task2 = userManager2.GetUserAsync(userIdGuid);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.Null(result);

        Assert.Null(result2);
    }


    [Fact] // Корректные данные
    public async Task GetUserDtoAsync_ConcurrencyConflict_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;
        var expectedDto = new UserDto()
        {
            Username = user.Username,
            Firstname = user.Firstname,
            LanguageCode = user.LanguageCode
        };
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.GetUserDtoAsync(userIdGuid);
        var task2 = userManager2.GetUserDtoAsync(userIdGuid);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value); // Так-то userDtoFromDb тоже может быть null и Equivalent пройдёт
        Assert.Equivalent(expectedDto, result.Value);

        Assert.Equivalent(result, result2);
    }


    [Theory] // Корректные данные
    [InlineData("новоеИмя", "newusername", "nn")]
    [InlineData("Кля", "klya", "ru")] // Меняем всё кроме username'а
    public async Task UpdateUserAsyncByUpdateUserDto_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrNoChangesDetectedOrUsernameAlreadyTaken(string firstname, string username, string languageCode)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Такой User должен быть после обновления
        var mustUserFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        mustUserFromDbAfterUpdate.Firstname = updateUserDto.Firstname;
        mustUserFromDbAfterUpdate.Username = updateUserDto.Username;
        mustUserFromDbAfterUpdate.LanguageCode = updateUserDto.LanguageCode;

        // Act
        var task = userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        var task2 = userManager2.UpdateUserAsync(userIdGuid, updateUserDto);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо нет изменений, либо Username, Email, PhoneNumber уже занят
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.NoChangesDetected,
                    ErrorMessages.UsernameAlreadyTaken,
                    ErrorMessages.EmailAlreadyTaken,
                    ErrorMessages.PhoneNumberAlreadyTaken,
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

            // Все поля совпадают, кроме RowVersion, но RowVersion также должен пройти проверку на null
            AssertExtensions.EqualIgnoring(userFromDbAfterUpdate, mustUserFromDbAfterUpdate, (user) =>
            {
                Assert.NotNull(user.RowVersion);
            }, nameof(userFromDbAfterUpdate.RowVersion));
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }


    [Fact]
    public async Task DeleteUserAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUserNotFound()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.DeleteUserAsync(userIdGuid, deleteUserDto);
        var task2 = userManager2.DeleteUserAsync(userIdGuid, deleteUserDto);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь не найден
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserNotFound
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            // Пользователя больше не существует
            var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
            Assert.Null(userFromDbAfterDelete);
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }


    [Theory] // Корректные данные
    [InlineData("Никита", "niksuper", "123@", "ru", "fan.ass995@mail.ru", "912345")]
    public async Task CreateUserAsyncByCreateUserDto_CorrectData_ReturnsErrorMessage_NothingOrConflictOrUsernameAlreadyTakenOrEmailAlreadyTakenOrPhoneNumberAlreadyTaken(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
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
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Такой пользователь должен быть
        var mustUser = new User
        {
            Firstname = createUserDto.Firstname,
            Username = createUserDto.Username,
            HashedPassword = _passwordHasher.GenerateHashedPassword(createUserDto.Password),
            LanguageCode = createUserDto.LanguageCode,
            Role = UserRoles.User,
            IsPremium = false,
            AvatarURL = TestConstants.DefaultAvatarPath,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var task = userManager.CreateUserAsync(createUserDto);
        var task2 = userManager2.CreateUserAsync(createUserDto);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо публикация не найдена, либо пользователь не является автором этой публикации (первый запрос успел удалить)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UsernameAlreadyTaken,
                    ErrorMessages.EmailAlreadyTaken,
                    ErrorMessages.PhoneNumberAlreadyTaken,
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            // Пользователь и вправду создался
            var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == createUserDto.Username);
            Assert.NotNull(userFromDbAfterCreate);

            // У пользователя, который должен создаться и, у пользователя который создался поля равны, кроме Id, HashedPassword, RowVersion, но эти игнорируемые поля должы быть не пустыми, кроме RowVersion
            AssertExtensions.EqualIgnoring(mustUser, userFromDbAfterCreate, (result) =>
            {
                if (result.Id == Guid.Empty)
                    Assert.Fail(nameof(result.Id) + "is empty");
                Assert.NotNull(result.HashedPassword);
                // RowVersion для mustUser null, а для userFromDbAfterCreate не null, поэтому проверка на null, тут не поможет
            }, nameof(mustUser.Id), nameof(mustUser.HashedPassword), nameof(mustUser.RowVersion));
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }


    [Fact] // Корректные данные
    public async Task ConfirmEmailAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUserAlreadyConfirmedEmailOrInvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.ConfirmEmailAsync(confirmEmailRequest.Token);
        var task2 = userManager2.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил почту, либо токен не найден (невалидный, т.е первый запрос уже удалил токен)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserAlreadyConfirmedEmail,
                    ErrorMessages.InvalidToken
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            // Почта и вправду подтвердилась
            var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
            Assert.NotNull(userFromDbAfter);
            Assert.True(userFromDbAfter.IsEmailConfirm);
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }

    [Fact] // Неверный токен (т.к мы удалим пользователя). У токена не может быть несуществующего пользователя
    public async Task ConfirmEmailAsync_ConcurrencyConflict_ReturnsServiceResult_NothingOrConflictOrUserNotFoundOrInvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.ConfirmEmailAsync(confirmEmailRequest.Token);
        var task2 = userManager2.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил почту, либо токен не найден (невалидный, т.е первый запрос уже удалил токен)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserNotFound,
                    ErrorMessages.InvalidToken
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }

    [Fact]
    public async Task ConfirmEmailAsync_ConcurrencyConflict_UserAlreadyConfirmedEmail_ReturnsErrorMessage_NothingOrConflictOrUserAlreadyConfirmedEmailOrInvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid);
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.ConfirmEmailAsync(confirmEmailRequest.Token);
        var task2 = userManager2.ConfirmEmailAsync(confirmEmailRequest.Token);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил почту, либо токен не найден (невалидный, т.е первый запрос уже удалил токен)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserAlreadyConfirmedEmail,
                    ErrorMessages.InvalidToken
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }


    [Fact] // Корректные данные
    public async Task VerificatePhoneNumberAsync_ConcurrencyConflict_ReturnsServiceResult_NothingOrConflictOrUserAlreadyConfirmedPhoneNumberOrInvalidCode()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);
        var task2 = userManager2.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил номер телефона, либо код не найден (невалидный, т.е первый запрос уже удалил код)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserAlreadyConfirmedPhoneNumber,
                    ErrorMessages.InvalidCode
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }

            // Номер и вправду подтвердился
            var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
            Assert.NotNull(userFromDbAfter);
            Assert.True(userFromDbAfter.IsPhoneNumberConfirm);
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }

    [Fact] // Неверный код (т.к мы удалим пользователя). У кода не может быть несуществующего пользователя
    public async Task VerificatePhoneNumberAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUserNotFoundOrInvalidCode()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Удаляем пользователя
        _db.Users.Remove(user);

        // Act
        var task = userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);
        var task2 = userManager2.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил почту, либо токен не найден (невалидный, т.е первый запрос уже удалил токен)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserNotFound,
                    ErrorMessages.InvalidCode
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }

    [Fact]
    public async Task VerificatePhoneNumberAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUserAlreadyConfirmedPhoneNumberOrInvalidCode()
    {
        // Arrange
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid);
        var userManager = GenerateNewUserManager();
        var userManager2 = GenerateNewUserManager();

        // Act
        var task = userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);
        var task2 = userManager2.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо пользователь уже подтвердил почту, либо токен не найден (невалидный, т.е первый запрос уже удалил токен)
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserAlreadyConfirmedPhoneNumber,
                    ErrorMessages.InvalidCode
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }
}