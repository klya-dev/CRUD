using Microsoft.AspNetCore.TestHost;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class AuthSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public AuthSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Theory]
    [InlineData("test", "123")] // Корректные данные
    [InlineData("klya", "1")]
    public async Task Post_Login_ReturnsAuthJwtResponse(string username, string password)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL)
        {
            Content = JsonContent.Create(loginData)
        };

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.Deserialize<AuthJwtResponse>();

        Assert.NotNull(response);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.AccessToken);
        Assert.NotEqual(DateTime.MinValue, response.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.Username);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    // NotValid случаи уже протещены в NotValidDataEndpointSystemTest

    [Fact]
    public async Task Post_Login_WhenUserNotFound_ReturnsInvalidLoginOrPassword()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные для запроса
        string username = "username";
        string password = "123";
        var loginData = new LoginDataDto { Username = username, Password = password };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL)
        {
            Content = JsonContent.Create(loginData)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Неверный логин или пароль.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.INVALID_LOGIN_OR_PASSWORD, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Login_WhenInvalidPassword_ReturnsInvalidLoginOrPassword()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные для запроса
        string username = "username";
        string password = "12345";
        var loginData = new LoginDataDto { Username = username, Password = "123" };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL)
        {
            Content = JsonContent.Create(loginData)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, hashedPassword: password, ct: TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Неверный логин или пароль.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.INVALID_LOGIN_OR_PASSWORD, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Theory]
    [InlineData("dfsedqweqws12")]
    public async Task Post_RefreshLogin_ReturnsAuthJwtResponse(string refreshToken)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем Refresh-токен в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, token: refreshToken, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_REFRESH_LOGIN_URL)
        {
            Content = JsonContent.Create(authRefreshToken.Token)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.Deserialize<AuthJwtResponse>();

        Assert.NotNull(response);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.AccessToken);
        Assert.NotEqual(DateTime.MinValue, response.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.Username);

        // Переданный Refresh-токен удалён
        var authRefreshTokenFromDbAfterLogin = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken.Id, TestContext.Current.CancellationToken);
        Assert.Null(authRefreshTokenFromDbAfterLogin);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task Post_RefreshLogin_WhenTokenNotFound_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        string token = "some";

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_REFRESH_LOGIN_URL)
        {
            Content = JsonContent.Create(token)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_RefreshLogin_WhenTokenIsExpired_ReturnsInvalidToken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем Refresh-токен в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, expires: DateTime.MinValue, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_REFRESH_LOGIN_URL)
        {
            Content = JsonContent.Create(authRefreshToken.Token)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.INVALID_TOKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Theory]
    [InlineData("Васян", "killmonster", "qwerty100", "ru", "fan.ass995@mail.ru", "912345")] // Корректные данные
    [InlineData("Костя", "fanatklya", "1234", "en", "fan.ass9951@mail.ru", "9123456")]
    public async Task Post_Register_ReturnsToken(string firstname, string username, string password, string languageCode, string email, string phoneNumber)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные для запроса
        var data = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_REGISTER_URL)
        {
            Content = JsonContent.Create(data)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.Deserialize<AuthJwtResponse>();

        Assert.NotNull(response);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.AccessToken);
        Assert.NotEqual(DateTime.MinValue, response.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.Username);

        // Пользователь добавился в базу
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == data.Email, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfter);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.User.Username == username).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task Post_Register_ReturnsUsernameAlreadyTaken()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Данные для запроса
        string firstname = "тест";
        string username = "test";
        string password = "12345";
        string languageCode = "ru";
        string email = "fan.ass95@mail.ru";
        string phoneNumber = "12345678";
        var data = new CreateUserDto()
        {
            Firstname = firstname,
            Username = username,
            Password = password,
            LanguageCode = languageCode,
            Email = email,
            PhoneNumber = phoneNumber
        };

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_REGISTER_URL)
        {
            Content = JsonContent.Create(data)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, username: username, ct: TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Имя пользователя занято.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USERNAME_ALREADY_TAKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_OAuthLink_ReturnsLink()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.AUTH_OAUTH_LINK_URL);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.StartsWith("\"http", response);
    }


    [Fact]
    public async Task Post_OAuthLogin_Mock_ReturnsAuthJwtResponse()
    {
        // Arrange
        var code = "somecode";
        var state = "somestate";

        var mockOAuthMailRuProvider = new Mock<IOAuthMailRuProvider>();

        // Успешное получение токена
        mockOAuthMailRuProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("accesstoken");

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

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
            Email = user.Email
        };

        // Успешное получение данных пользователя по токену
        mockOAuthMailRuProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(userInfo);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockOAuthMailRuProvider.Object);
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_OAUTH_LOGIN_URL + $"?code={code}&state={state}");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.Deserialize<AuthJwtResponse>();

        Assert.NotNull(response);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.AccessToken);
        Assert.NotEqual(DateTime.MinValue, response.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.Username);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == user.Id).CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task Post_OAuthLogin_Mock_ReturnsUserNotFound()
    {
        // Arrange
        var code = "somecode";
        var state = "somestate";

        var mockOAuthMailRuProvider = new Mock<IOAuthMailRuProvider>();

        // Успешное получение токена
        mockOAuthMailRuProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("accesstoken");

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

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
            Email = "some"
        };

        // Успешное получение данных пользователя по токену
        mockOAuthMailRuProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(userInfo);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockOAuthMailRuProvider.Object);
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_OAUTH_LOGIN_URL + $"?code={code}&state={state}");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Post_OAuthRegistration_Mock_ReturnsAuthJwtResponse()
    {
        // Arrange
        var code = "somecode";
        var state = "somestate";

        var mockOAuthMailRuProvider = new Mock<IOAuthMailRuProvider>();

        // Успешное получение токена
        mockOAuthMailRuProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("accesstoken");

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

        // Успешное получение данных пользователя по токену
        mockOAuthMailRuProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(userInfo);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockOAuthMailRuProvider.Object);
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_OAUTH_REGISTRATION_URL + $"?code={code}&state={state}")
        {
            Content = JsonContent.Create(oAuthCompleteRegistrationDto)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);
        var response = jsonDocument.Deserialize<AuthJwtResponse>();

        Assert.NotNull(response);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.AccessToken);
        Assert.NotEqual(DateTime.MinValue, response.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response.Username);

        // Пользователь добавился в базу
        var userFromDbAfter = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == userInfo.Email, TestContext.Current.CancellationToken);
        Assert.NotNull(userFromDbAfter);

        // Refresh-токен добавился в базу
        var countRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.UserId == userFromDbAfter.Id).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, countRefreshTokensFromDb);
    }

    [Fact]
    public async Task Post_OAuthRegistration_Mock_ReturnsEmailAlreadyTaken()
    {
        // Arrange
        var code = "somecode";
        var state = "somestate";

        var mockOAuthMailRuProvider = new Mock<IOAuthMailRuProvider>();

        // Успешное получение токена
        mockOAuthMailRuProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("accesstoken");

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

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, email: userInfo.Email, ct: TestContext.Current.CancellationToken);

        var oAuthCompleteRegistrationDto = new OAuthCompleteRegistrationDto
        {
            PhoneNumber = "123456789"
        };

        // Успешное получение данных пользователя по токену
        mockOAuthMailRuProvider.Setup(x => x.GetUserInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(userInfo);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockOAuthMailRuProvider.Object);
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_OAUTH_REGISTRATION_URL + $"?code={code}&state={state}")
        {
            Content = JsonContent.Create(oAuthCompleteRegistrationDto)
        };

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.EMAIL_ALREADY_TAKEN, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}