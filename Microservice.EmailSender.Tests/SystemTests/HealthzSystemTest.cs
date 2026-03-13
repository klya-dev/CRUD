using Microservice.EmailSender.HealthChecks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microservice.EmailSender.Tests.SystemTests;

public class HealthzSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public HealthzSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("text/plain", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync();

        Assert.NotNull(response);
        Assert.Equal("Healthy", response);
    }

    [Fact]
    public async Task Get_Mock_WhenFailedConnectS3_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to S3.";

        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<HealthCheckContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HealthCheckResult(HealthStatus.Unhealthy, description));
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Удаляем все HealthCheck'и
                    foreach (var registration in options.Registrations.ToList())
                        options.Registrations.Remove(registration);

                    // Добавляем тестовый
                    options.Registrations.Add(new HealthCheckRegistration(nameof(S3ConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal("text/plain", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync();

        Assert.NotNull(response);
        Assert.Equal("Unhealthy", response);
    }

    [Fact]
    public async Task Get_Mock_WhenFailedConnectEmail_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to email server.";

        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<HealthCheckContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HealthCheckResult(HealthStatus.Unhealthy, description));
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Удаляем все HealthCheck'и
                    foreach (var registration in options.Registrations.ToList())
                        options.Registrations.Remove(registration);

                    // Добавляем тестовый
                    options.Registrations.Add(new HealthCheckRegistration(nameof(EmailConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal("text/plain", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync();

        Assert.NotNull(response);
        Assert.Equal("Unhealthy", response);
    }

    [Fact]
    public async Task Get_Mock_WhenFailedConnectPrometheus_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to prometheus server.";

        var mockHealthCheck = new Mock<IHealthCheck>();
        mockHealthCheck.Setup(x => x.CheckHealthAsync(It.IsAny<HealthCheckContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HealthCheckResult(HealthStatus.Unhealthy, description));
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Удаляем все HealthCheck'и
                    foreach (var registration in options.Registrations.ToList())
                        options.Registrations.Remove(registration);

                    // Добавляем тестовый
                    options.Registrations.Add(new HealthCheckRegistration(nameof(PrometheusConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal("text/plain", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync();

        Assert.NotNull(response);
        Assert.Equal("Unhealthy", response);
    }
}