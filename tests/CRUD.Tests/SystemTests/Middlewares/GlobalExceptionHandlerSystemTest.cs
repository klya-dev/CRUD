using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class GlobalExceptionHandlerSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GlobalExceptionHandlerSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Login_ThrowsException_ReturnsInternalServiceError()
    {
        var mockAuthManager = new Mock<IAuthManager>();
        mockAuthManager.Setup(x => x.LoginAsync(It.IsAny<LoginDataDto>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("something"));

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockAuthManager.Object);
            });
        }).CreateClient();

        // Arrange
        // Тело запроса
        var loginData = new LoginDataDto() { Username = "user", Password = "pass" };

        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL)
        {
            Content = JsonContent.Create(loginData)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(500, jsonDocument.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Server Error", jsonDocument.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Post_Login_ThrowsTimeoutException_ReturnsServiceUnavailable()
    {
        var mockAuthManager = new Mock<IAuthManager>();
        mockAuthManager.Setup(x => x.LoginAsync(It.IsAny<LoginDataDto>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("something"));

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => mockAuthManager.Object);
            });
        }).CreateClient();

        // Arrange
        // Тело запроса
        var loginData = new LoginDataDto() { Username = "user", Password = "pass" };

        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL)
        {
            Content = JsonContent.Create(loginData)
        };
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(503, jsonDocument.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Service Unavailable", jsonDocument.RootElement.GetProperty("title").GetString());
    }
}