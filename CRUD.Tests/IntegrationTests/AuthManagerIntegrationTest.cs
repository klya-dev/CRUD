#nullable disable
using CRUD.Models.Domains;
using CRUD.Models.Dtos.User;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class AuthManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IAuthManager _authManager;
    private readonly ApplicationDbContext _db;

    public AuthManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _authManager = scopedServices.GetRequiredService<IAuthManager>();
    }


    private IAuthManager GenerateNewAuthManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IAuthManager>();
    }

    [Theory]
    [InlineData("test", "123")] // Корректные данные
    [InlineData("klya", "1")]
    public async Task LoginAsync_ReturnsAuthJwtResponse(string username, string password)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, hashedPassword: password);

        var loginData = new LoginDataDto { Username = username, Password = password };

        // Act
        var result = await _authManager.LoginAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value); // Не пустой ответ
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync();
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Theory]
    [InlineData(null, null)] // Невалидная модель
    [InlineData("", "")]
    public async Task LoginAsync_WhenNotValidData_ThrowsInvalidOperationException(string username, string password)
    {
        // Arrange
        var loginData = new LoginDataDto { Username = username, Password = password };

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
        string username = "username";
        string password = "123";

        var loginData = new LoginDataDto { Username = username, Password = password };

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
        string username = "username";
        string password = "12345";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, hashedPassword: password);

        var loginData = new LoginDataDto { Username = username, Password = "123" };

        // Act
        var result = await _authManager.LoginAsync(loginData);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.InvalidLoginOrPassword, result.ErrorMessage);
    }


    [Theory]
    [InlineData("dfsedqweqws12")]
    public async Task LoginAsyncByRefreshToken_ReturnsAuthJwtResponse(string refreshToken)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем Refresh-токен в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, token: refreshToken);

        // Act
        var result = await _authManager.LoginAsync(refreshToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value); // Не пустой ответ
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        // Переданный Refresh-токен удалён
        var authRefreshTokenFromDbAfterLogin = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken.Id);
        Assert.Null(authRefreshTokenFromDbAfterLogin);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync();
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task LoginAsyncByRefreshToken_WhenTokenNotFound_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        string token = "some";

        // Act
        var result = await _authManager.LoginAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsyncByRefreshToken_WhenTokenIsExpired_ReturnsErrorMessage_InvalidToken()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем Refresh-токен в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, expires: DateTime.MinValue);

        // Act
        var result = await _authManager.LoginAsync(authRefreshToken.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.InvalidToken, result.ErrorMessage);
    }


    [Fact]
    public async Task LoginAsyncByUserInfo_ReturnsAuthJwtResponse()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

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
            Email = user.Email
        };

        // Act
        var result = await _authManager.LoginAsync(userInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value); // Не пустой ответ
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync();
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task LoginAsyncByUserInfo_ReturnsErrorMessage_UserNotFound()
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
            Email = "some"
        };

        // Act
        var result = await _authManager.LoginAsync(userInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData("Васян", "killmonster", "qwerty100", "ru", "fan.ass995@mail.ru", "912345")] // Корректные данные
    [InlineData("Костя", "fanatklya", "1234", "en", "fan.ass9951@mail.ru", "9123456")]
    public async Task RegisterAsync_ReturnsAuthJwtResponse(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _authManager.RegisterAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        // Пользователь добавился в базу
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        Assert.NotNull(userFromDbAfter);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == userFromDbAfter.Id).CountAsync();
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Theory]
    [InlineData("Oleg", "12oleg@", "12345", "uk", "fan.ass95@mail.ru", "12345")] // Имя и Username невалидные
    [InlineData(null, null, null, null, null, null)] // Пустые данные
    public async Task RegisterAsync_NotValidData_ThrowsInvalidOperationException(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        Func<Task> a = async () =>
        {
            await _authManager.RegisterAsync(createUserDto);
        };

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(a);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsErrorMessage_UsernameAlreadyTaken()
    {
        // Arrange
        string firstname = "тест";
        string username = "test";
        string password = "12345";
        string languageCode = "ru";
        string email = "fan.ass95@mail.ru";
        string phoneNumber = "12345678";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username);

        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Act
        var result = await _authManager.RegisterAsync(createUserDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.UsernameAlreadyTaken, result.ErrorMessage);
    }

    // Письмо уже отправлено нет теста
    // Да и SendVerificationCodePhoneNumberAsync нет


    [Fact]
    public async Task RegisterAsyncByUserInfoAndOAuthCompleteRegistrationDto_ReturnsAuthJwtResponse()
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
            PhoneNumber = "123456789"
        };

        // Act
        var result = await _authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        // Пользователь добавился в базу
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email);
        Assert.NotNull(userFromDbAfter);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == userFromDbAfter.Id).CountAsync();
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Theory]
    [InlineData("phone")] // PhoneNumber невалиднен
    [InlineData(null)] // Пустые данные
    public async Task RegisterAsyncByUserInfoAndOAuthCompleteRegistrationDto_NotValidData_ThrowsInvalidOperationException(string phoneNumber)
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
            await _authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(OAuthCompleteRegistrationDto), validationResult.Errors), ex.Message);
    }

    [Fact]
    public async Task RegisterAsyncByUserInfoAndOAuthCompleteRegistrationDto_ReturnsErrorMessage_EmailAlreadyTaken()
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
            PhoneNumber = "123456789"
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, email: userInfo.Email);

        // Act
        var result = await _authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(ErrorMessages.EmailAlreadyTaken, result.ErrorMessage);
    }


    [Fact]
    public async Task SendConfirmEmailAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Act
        var result = await _authManager.SendConfirmEmailAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendConfirmEmailAsync_ReturnsErrorMessage_UserNotFound()
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
    public async Task SendConfirmEmailAsync_ReturnsErrorMessage_UserAlreadyConfirmedEmail()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true);

        // Act
        var result = await _authManager.SendConfirmEmailAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyConfirmedEmail, result.ErrorMessage);
    }

    [Fact] // Письмо уже отправлено (таймаут)
    public async Task SendConfirmEmailAsync_ReturnsErrorMessage_LetterAlreadySent()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Отправляем первое письмо
        await _authManager.SendConfirmEmailAsync(user.Id);

        // Act
        var result = await _authManager.SendConfirmEmailAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.LetterAlreadySent, result.ErrorMessage);
    }


    // Конфликты параллельности


    [Theory]
    [InlineData("test", "123")] // Корректные данные
    [InlineData("klya", "1")]
    public async Task LoginAsync_ConcurrencyConflict_ReturnsToken(string username, string password)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, hashedPassword: password);

        var loginData = new LoginDataDto { Username = username, Password = password };
        var authManager = GenerateNewAuthManager();
        var authManager2 = GenerateNewAuthManager();

        // Act
        var task = authManager.LoginAsync(loginData);
        var task2 = authManager2.LoginAsync(loginData);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value); // Не пустой токен
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);

        Assert.NotNull(result2);
        Assert.Null(result2.ErrorMessage);
        Assert.NotNull(result2.Value); // Не пустой токен
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.Value.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result2.Value.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.Value.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.Value.Username);
    }


    [Theory]
    [InlineData("Васян", "killmonster", "qwerty100", "ru", "fan.ass95@mail.ru", "12345")] // Корректные данные
    [InlineData("Костя", "fanatklya", "1234", "en", "fan.ass95@mail.ru", "12345")]
    public async Task RegisterAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUsernameAlreadyTakenOrEmailAlreadyTakenOrPhoneNumberAlreadyTaken(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var createUserDto = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };
        var authManager = GenerateNewAuthManager();
        var authManager2 = GenerateNewAuthManager();

        // Act
        var task = authManager.RegisterAsync(createUserDto);
        var task2 = authManager2.RegisterAsync(createUserDto);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Может быть успешный ответ
                var errorMessage = result.ErrorMessage;
                if (errorMessage == null)
                {
                    Assert.NotNull(result.Value); // Не пустой токен
                    AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.AccessToken);
                    Assert.NotEqual(DateTime.MinValue, result.Value.Expires);
                    AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.RefreshToken);
                    AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.Username);
                    continue;
                }

                // Либо Username, Email, PhoneNumber уже занят
                string[] allowedErrors =
                [
                    ErrorMessages.UsernameAlreadyTaken,
                    ErrorMessages.EmailAlreadyTaken,
                    ErrorMessages.PhoneNumberAlreadyTaken
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
    public async Task SendConfirmEmailAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrLetterAlreadySent()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        var authManager = GenerateNewAuthManager();
        var authManager2 = GenerateNewAuthManager();

        // Act
        var task = authManager.SendConfirmEmailAsync(user.Id);
        var task2 = authManager2.SendConfirmEmailAsync(user.Id);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Ничего, либо письмо уже отправлено
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.LetterAlreadySent
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