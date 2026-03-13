using Microsoft.AspNetCore.Http.Timeouts;
using System.Net;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

public class RequestTimeoutsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RequestTimeoutsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_Login_Timeout_ReturnsGatewayTimeout()
    {
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            // Устанавливаем ограничение на 1 миллисекунду
            configuration.ConfigureServices(services =>
            {
                services.AddRequestTimeouts(options =>
                {
                    options.DefaultPolicy = new RequestTimeoutPolicy
                    {
                        Timeout = TimeSpan.FromMilliseconds(1)
                    };
                });
            });
        }).CreateClient();

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Тело запроса
        var loginData = new LoginDataDto() { Username = "user", Password = "pass" };
        var json = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, Application.Json);
        request.Content = json;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.GatewayTimeout, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }
}