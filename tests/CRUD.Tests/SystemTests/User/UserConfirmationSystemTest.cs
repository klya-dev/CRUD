using Microsoft.AspNetCore.TestHost;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class UserConfirmationSystemTest : IClassFixture<TestWebApplicationFactory>
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

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
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Пользователь не найден.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_Email_ReturnsUserAlreadyConfirmedEmail()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isEmailConfirm: true, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Пользователь уже подтвердил электронную почту.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.USER_ALREADY_CONFIRMED_EMAIL, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact] // Письмо уже отправлено (таймаут)
    public async Task Post_Email_ReturnsLetterAlreadySent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Отправляем первое письмо
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_EMAIL_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request2, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Письмо уже отправлено.", jsonDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal(ErrorCodes.LETTER_ALREADY_SENT, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Post_Phone_Mock_WhenSms_ReturnsNoContent()
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?messageType=" + MessageType.Sms;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact]
    public async Task Post_Phone_Mock_WhenTelegram_ReturnsNoContent()
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?messageType=" + MessageType.Telegram;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

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
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?messageType=" + MessageType.Sms;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, isPhoneNumberConfirm: true, ct: TestContext.Current.CancellationToken);

        // Запрос
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?messageType=" + MessageType.Sms;
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Данные для запросов
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?messageType=" + MessageType.Sms;

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, user.Id.ToString());

        // Отправляем первое письмо
        await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request2, _tokenManager, user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request2, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.CODE_ALREADY_SENT, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}