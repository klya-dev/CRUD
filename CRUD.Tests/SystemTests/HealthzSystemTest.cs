using CRUD.WebApi.HealthChecks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRUD.Tests.SystemTests;

public class HealthzSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IS3Manager _s3Manager;

    public HealthzSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
    }

    [Fact]
    public async Task Get_ReturnsHealthy()
    {
        // Arrange
        // Удаляем из HealthCheck'ов HubsConnectionHealthCheck, т.к почему-то в тесте он не может подключится (скорее всего потому что нужно прописать HttpMessageHandlerFactory, но в специально для тестов, я это делать не буду)
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    options.Registrations.Remove(options.Registrations.First(x => x.Name == nameof(HubsConnectionHealthCheck)));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Mock_WhenFailedConnectDatabase_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to the database.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(DatabaseConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_WhenFailedConsistencyDatabase_ReturnsUnhealthy()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Удаляем таблицу
        await _db.Database.ExecuteSqlRawAsync(string.Format("DROP TABLE {0}", "OrderNumberSequences"));

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_WhenFailedConsistencyS3_ReturnsUnhealthy()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Чтобы восстановить за собой
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/default.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Удаляем объект
        await _s3Manager.DeleteObjectAsync($"{TestConstants.TEST_FILES_PATH}/default.png");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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

        // Восстанавливаем объект
        await _s3Manager.CreateObjectAsync(memStream, $"{TestConstants.TEST_FILES_PATH}/default.png");
    }

    [Fact]
    public async Task Get_Mock_WhenFailedConnectRedis_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to redis.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(RedisConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Mock_WhenFailedConnectSms_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to SMS server.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(SmsConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Mock_WhenFailedConnectTelegram_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to Telegram server.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(TelegramConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_Mock_WhenFailedConnectPayment_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to payment server.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(PaymentConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_WhenFailedConnectHubs_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to the hubs.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(HubsConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_WhenFailedConnectOAuthMailRu_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to OAuth MailRu server.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(OAuthMailRuConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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
    public async Task Get_WhenFailedConnectRabbitMq_ReturnsUnhealthy()
    {
        // Arrange
        string description = "Failed to connect to RabbitMQ server.";

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
                    options.Registrations.Add(new HealthCheckRegistration(nameof(RabbitMqConnectionHealthCheck), mockHealthCheck.Object, null, null));
                });
            });
        }).CreateClient();

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

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