using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public sealed class UserManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
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

    [Fact] // Корректные данные
    public async Task GetUserAsync_ReturnsUser()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.GetUserAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.GetUserAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }


    [Fact] // Корректные данные
    public async Task GetUserDtoAsync_ReturnsUserDto()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;
        var userDtoFromDb = new UserDto
        {
            Firstname = user.Firstname,
            Username = user.Username,
            LanguageCode = user.LanguageCode,
            AvatarPresignedUrl = "something"
        };

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        AssertExtensions.EqualIgnoring(userDtoFromDb, result.Value, ignoreProperties: nameof(UserDto.AvatarPresignedUrl)); // AvatarPresignedUrl не сравниваем
        Assert.StartsWith("https://", result.Value.AvatarPresignedUrl); // Нормальная ссылка
    }

    [Fact]
    public async Task GetUserDtoAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.GetUserDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.GetUserFullDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.GetUserFullDtoAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = newFirstname,
            Username = newUsername,
            LanguageCode = newLanguageCode
        };

        // Такой User должен быть после обновления
        var mustUserFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        mustUserFromDbAfterUpdate.Firstname = updateUserDto.Firstname;
        mustUserFromDbAfterUpdate.Username = updateUserDto.Username;
        mustUserFromDbAfterUpdate.LanguageCode = updateUserDto.LanguageCode;

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new UpdateUserDtoValidator(validatorsLocalizer).ValidateAsync(updateUserDto, TestContext.Current.CancellationToken);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.UpdateUserAsync(userIdGuid, updateUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(UpdateUserDto), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Пользователя и вправду не существует
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, firstname: firstname, username: username, languageCode: languageCode, ct: TestContext.Current.CancellationToken);

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, username: "username", ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user2 = await DI.CreateUserAsync(_db, username: username, email: "test", phoneNumber: "1234567", ct: TestContext.Current.CancellationToken);

        var updateUserDto = new UpdateUserDto()
        {
            Firstname = firstname,
            Username = username,
            LanguageCode = languageCode
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.UpdateUserAsync(userIdGuid, updateUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UsernameAlreadyTaken, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }


    [Fact]
    public async Task DeleteUserAsync_ReturnsServiceResult()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду не удалилась, т.к дефолтная
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenAvatarIsNotDefault_ReturnsServiceResult()
    {
        // Arrange
        string password = "123";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream, TestContext.Current.CancellationToken);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")] // Пустые данные
    [InlineData(null)] // Пустые данные
    public async Task DeleteUserAsync_NotValidData_ThrowsInvalidOperationException(string password)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = password
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new DeleteUserDtoValidator(validatorsLocalizer).ValidateAsync(deleteUserDto, TestContext.Current.CancellationToken);

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
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        var deleteUserDto = new DeleteUserDto()
        {
            Password = "12345"
        };
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, deleteUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidPassword, result.ErrorMessage);

        // Пользователь всё ещё существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfterDelete);
    }


    [Fact]
    public async Task DeleteUserWithoutDtoAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду не удалилась, т.к дефолтная
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUserWithoutDtoAsync_WhenAvatarIsNotDefault_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream, TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователя больше не существует
        var userFromDbAfterDelete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterDelete);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteUserWithoutDto_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.DeleteUserAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.CreateUserAsync(createUserDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == createUserDto.Username, TestContext.Current.CancellationToken);
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
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new CreateUserDtoValidator(validatorsLocalizer).ValidateAsync(createUserDto, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.CreateUserAsync(createUserDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(CreateUserDto), validationResult.Errors), ex.Message);

        // Пользователь не создан
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == createUserDto.Username, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, username: username, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.CreateUserAsync(createUserDto, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, email: email, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.CreateUserAsync(createUserDto, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, phoneNumber: phoneNumber, ct: TestContext.Current.CancellationToken);

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
        var result = await _userManager.CreateUserAsync(createUserDto, ct: TestContext.Current.CancellationToken);

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
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new OAuthCompleteRegistrationDtoValidator(validatorsLocalizer).ValidateAsync(oAuthCompleteRegistrationDto, TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, username: userInfo.Nickname, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Пользователь создался
        var userFromDbAfterCreate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, email: userInfo.Email, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, phoneNumber: oAuthCompleteRegistrationDto.PhoneNumber, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.CreateUserAsync(userInfo, oAuthCompleteRegistrationDto, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };

        // Такой User должен быть после обновления
        var mustUserFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        mustUserFromDbAfterUpdate.Role = role;

        // Act
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = user.Id;
        var validatorsLocalizer = new ValidatorLocalizer();
        var validationResult = await new SetRoleDtoValidator(validatorsLocalizer).ValidateAsync(setRoleDto, TestContext.Current.CancellationToken);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        Func<Task> a = async () =>
        {
            await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(SetRoleDto), validationResult.Errors), ex.Message);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
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
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);

        // Пользователя и вправду не существует
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Null(userFromDbAfterUpdate);
    }

    [Fact]
    public async Task SetRoleAsync_ReturnsErrorMessage_NoChangesDetected()
    {
        // Arrange
        string role = UserRoles.Admin;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, role: role, ct: TestContext.Current.CancellationToken);

        var setRoleDto = new SetRoleDto()
        {
            Role = role
        };
        var userIdGuid = user.Id;
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.SetRoleUserAsync(userIdGuid, setRoleDto, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.NoChangesDetected, result.ErrorMessage);

        // Пользователь и вправду не обновился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }


    [Fact]
    public async Task RevokeRefreshTokensAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "12133244", ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.RevokeRefreshTokensAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Все токены пользователя удалены
        var countAuthRefreshTokens = await _db.AuthRefreshTokens.Where(x => x.UserId == userIdGuid).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, countAuthRefreshTokens);
    }

    [Fact]
    public async Task RevokeRefreshTokensAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _userManager.RevokeRefreshTokensAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task ConfirmEmailAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Почта и вправду подтвердилась
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id  == userIdGuid, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsEmailConfirm);
    }

    [Fact] // Неверный токен
    public async Task ConfirmEmailAsync_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        string token = "something";

        // Act
        var result = await _userManager.ConfirmEmailAsync(token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact] // Неверный токен (т.к мы удалим пользователя). У токена не может быть несуществующего пользователя. Запрос автоматически удалится
    public async Task ConfirmEmailAsync_WhenUserDeleted_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsErrorMessage_UserAlreadyConfirmedEmail()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var confirmEmailRequest = await DI.CreateConfirmEmailRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.ConfirmEmailAsync(confirmEmailRequest.Token, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedEmail, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task VerificatePhoneNumberAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Номер и вправду подтвердился
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfter);
        Assert.True(userFromDbAfter.IsPhoneNumberConfirm);
    }

    [Fact] // Неверный код
    public async Task VerificatePhoneNumberAsync_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
        string code = "1234";

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, code, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidCode, result.ErrorMessage);
    }

    [Fact] // Неверный код, если авторизованный пользователь, пытается выдать себя за владельца кода
    public async Task VerificatePhoneNumberAsync_WhenRequestSendAnotherUser_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test@test.test", phoneNumber: "123456789", ct: TestContext.Current.CancellationToken);

        // Добавляем токен в базу. Владелец этого токена первый пользователь
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id, ct: TestContext.Current.CancellationToken);

        // Act
        // Запрос делает второй пользователь (не владелец) с таким же кодом
        var result = await _userManager.VerificatePhoneNumberAsync(user2.Id, verificationPhoneNumberRequest.Code, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.InvalidCode, result.ErrorMessage);
    }

    [Fact] // Неверный код (т.к мы удалим пользователя). У кода не может быть несуществующего пользователя
    public async Task VerificatePhoneNumberAsync_WhenUserDeleted_ReturnsErrorMessage_InvalidCode()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Удаляем пользователя
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Запрос тоже должен удалиться вместе с пользователем
        var verificationPhoneNumberRequestFromDb = await _db.VerificationPhoneNumberRequests.FirstOrDefaultAsync(x => x.Id == verificationPhoneNumberRequest.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Добавляем токен в базу
        var verificationPhoneNumberRequest = await DI.CreateVerificationPhoneNumberRequestAsync(_db, userIdGuid, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _userManager.VerificatePhoneNumberAsync(userIdGuid, verificationPhoneNumberRequest.Code, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedPhoneNumber, result.ErrorMessage);
    }
}