using Microsoft.AspNetCore.TestHost;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public class UserConfirmationSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UserConfirmationSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact]
    public async Task Post_Email_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact]
    public async Task Post_Email_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Пользователь не найден.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Email_ReturnsUserAlreadyConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Пользователь уже подтвердил электронную почту.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact] // Письмо уже отправлено (таймаут)
    public async Task Post_Email_ReturnsLetterAlreadySent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Отправляем первое письмо
        await client.SendAsync(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Письмо уже отправлено.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.LETTER_ALREADY_SENT, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Post_Phone_Mock_WhenIsTelegramFalse_ReturnsNoContent()
    {
        // Arrange
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(x => x.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockSmsSender.Object);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram=false";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact]
    public async Task Post_Phone_Mock_WhenIsTelegramTrue_ReturnsNoContent()
    {
        // Arrange
        var mockTelegramIntegrationManager = new Mock<ITelegramIntegrationManager>();
        mockTelegramIntegrationManager.Setup(x => x.SendVerificationCodeTelegramAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockTelegramIntegrationManager.Object);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram=true";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact]
    public async Task Post_Phone_Mock_ReturnsUserNotFound()
    {
        // Arrange
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(x => x.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockSmsSender.Object);
            });
        }).CreateClient();

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram=false";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Пользователь не найден.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Phone_Mock_ReturnsUserAlreadyConfirmedPhoneNumber()
    {
        // Arrange
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(x => x.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockSmsSender.Object);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram=false";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_PHONE_NUMBER, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact] // Код уже отправлен (таймаут)
    public async Task Post_Phone_Mock_ReturnsCodeAlreadySent()
    {
        // Arrange
        var mockSmsSender = new Mock<ISmsSender>();
        mockSmsSender.Setup(x => x.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockSmsSender.Object);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Данные для запросов
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram=false";

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Отправляем первое письмо
        await client.SendAsync(request);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.CODE_ALREADY_SENT, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // Конфликты параллельности


    [Fact]
    public async Task Post_Email_ConcurrencyConflict_ReturnsNoContentOrConflictOrLetterAlreadySent()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.NoContent == result.StatusCode)
            {
                Assert.Null(result.Content.Headers.ContentType);
                continue;
            }

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Либо письмо уже отправлено, Conflict
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.LETTER_ALREADY_SENT,
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
        }
    }
}