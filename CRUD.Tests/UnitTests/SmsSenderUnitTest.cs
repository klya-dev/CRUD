using CRUD.Utility.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using System.Net;

namespace CRUD.Tests.UnitTests;

public class SmsSenderUnitTest
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<SmsSender>> _mockLogger;
    private readonly Mock<IOptions<SmsSenderOptions>> _mockOptions;
    private readonly SmsSender _smsSender;

    public SmsSenderUnitTest()
    {
        _mockHttpClientFactory = new();
        _mockHttpMessageHandler = new();
        _mockLogger = new();
        _mockOptions = new();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://localhost");
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _mockOptions.Setup(x => x.Value).Returns(new SmsSenderOptions() { ServiceURL = "https://localhost", ApiKey = "", Email = "", Sign = "" });

        _smsSender = new SmsSender(_mockOptions.Object, _mockHttpClientFactory.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData("12345678910", "text")]
    public async Task SendSmsAsync_ShouldReturnTrue_WhenResponseIsSuccess(string phoneNumber, string text)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _smsSender.SendSmsAsync(phoneNumber, text);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("12345678910", "text")]
    public async Task SendSmsAsync_ShouldReturnFalse_WhenResponseIsNotSuccess(string phoneNumber, string text)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        // Act
        var result = await _smsSender.SendSmsAsync(phoneNumber, text);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TestAuthAsync_ReturnFalse()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        var result = await _smsSender.TestAuthAsync();

        // Assert
        Assert.False(result);
    }
}