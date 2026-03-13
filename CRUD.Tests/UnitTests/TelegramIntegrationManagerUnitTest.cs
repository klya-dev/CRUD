using System.Net;
using System.Text.Json;

namespace CRUD.Tests.UnitTests;

public class TelegramIntegrationManagerUnitTest
{
    private readonly TelegramIntegrationManager _telegramIntegrationManager;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<TelegramIntegrationManager>> _mockLogger;
    private readonly Mock<IOptions<TelegramIntegrationOptions>> _mockOptions;

    public TelegramIntegrationManagerUnitTest()
    {
        _mockHttpClientFactory = new();
        _mockHttpMessageHandler = new();
        _mockLogger = new();
        _mockOptions = new();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://localhost");
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _mockOptions.Setup(x => x.Value).Returns(new TelegramIntegrationOptions() { ServiceURL = "https://localhost", ApiKey = "", TimeToLive = 60 });

        _telegramIntegrationManager = new TelegramIntegrationManager(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object);
    }

    // Хуй протестишь, т.к во внутреннем методе IsTelegramAbilityAsync, тоже вызывается SendAsync, а мок один, поэтому ошибка: объект уже задиспозился
    // Если закоментить IsTelegramAbilityAsync, то тесты пройдут
    // НО, если использовать очередь, то всё заебись

    [Theory]
    [InlineData("12345678910", "1234")]
    public async Task SendVerificationCodeTelegramAsync_ShouldReturnTrue_WhenApiReturnsSuccess(string phoneNumber, string code)
    {
        // Arrange
        // Ответ в IsTelegramAbilityAsync
        var responseContent = new
        {
            ok = true,
            result = new { request_id = "123" }
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        // Ответ в SendVerificationCodeTelegramAsync
        var responseContent2 = new
        {
            ok = true,
            result = new { request_id = "123" }
        };
        var responseJson2 = JsonSerializer.Serialize(responseContent2);
        var responseMessage2 = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson2)
        };

        var queueStuff = new Queue<HttpResponseMessage>();
        queueStuff.Enqueue(responseMessage);
        queueStuff.Enqueue(responseMessage2);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(queueStuff.Dequeue);

        // Act
        var result = await _telegramIntegrationManager.SendVerificationCodeTelegramAsync(phoneNumber, code);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("12345678910", "1234")]
    public async Task SendVerificationCodeTelegramAsync_ShouldReturnFalse_WhenApiReturnsError(string phoneNumber, string code)
    {
        // Arrange
        var responseContent = new
        {
            ok = false,
            error = "Some error"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _telegramIntegrationManager.SendVerificationCodeTelegramAsync(phoneNumber, code);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckConnectionAsync_ReturnTrue()
    {
        // Arrange
        // Неизвестный метод, значит авторизация прошла и подключение удалось
        var responseContent = new
        {
            ok = false,
            error = "UNKNOWN_METHOD"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _telegramIntegrationManager.CheckConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenUnauthorize_ReturnFalse()
    {
        // Arrange
        // Авторизация не прошла
        var responseContent = new
        {
            ok = false,
            error = "ACCESS_TOKEN_INVALID"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _telegramIntegrationManager.CheckConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenUnknownError_ReturnFalse()
    {
        // Arrange
        // Неизвестная ошибка
        var responseContent = new
        {
            ok = false,
            error = "something"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _telegramIntegrationManager.CheckConnectionAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckConnectionAsync_WhenOkIsTrue_ReturnFalse()
    {
        // Arrange
        var responseContent = new
        {
            ok = true,
            error = "something"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _telegramIntegrationManager.CheckConnectionAsync();

        // Assert
        Assert.False(result);
    }
}