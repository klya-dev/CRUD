using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace CRUD.Tests.SystemTests;

public class MetricsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ITokenManager _tokenManager;
    private readonly IMeterFactory _meterFactory;

    public MetricsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _meterFactory = scopedServices.GetRequiredService<IMeterFactory>();
    }

    // Тесты метрики ещё есть в WebHookSystemTest (не работает), OrderIssuerIntegrationTest

    [Fact]
    public async Task Get_Metrics_ReturnsDoesMetrics()
    {
        // Arrange
        var client = _factory.HttpClient;
        var collector = new MetricCollector<double>(_meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.METRICS_URL);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync();
        AssertExtensions.IsNotNullOrNotWhiteSpace(response);

        await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Collection(collector.GetMeasurementSnapshot(),
            measurement =>
            {
                Assert.Equal("http", measurement.Tags["url.scheme"]);
                Assert.Equal("GET", measurement.Tags["http.request.method"]);
                Assert.Equal("/metrics", measurement.Tags["http.route"]);
            });
    }

    [Fact] // С метриками
    public async Task Get_Publications_ReturnsDoesMetrics()
    {
        // Arrange
        var client = _factory.HttpClient;
        var collector = new MetricCollector<double>(_meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        // Запрос
        var url = TestConstants.PUBLICATIONS_URL + "?count=1";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

        await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Collection(collector.GetMeasurementSnapshot(),
            measurement =>
            {
                Assert.Equal("http", measurement.Tags["url.scheme"]);
                Assert.Equal("GET", measurement.Tags["http.request.method"]);
                Assert.Equal("/v{version:apiVersion}/publications/", measurement.Tags["http.route"]);
            });
    }

    [Fact] // Без метрик
    public async Task Get_Healthz_ReturnsDoesntMetrics()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.PostConfigure<HealthCheckServiceOptions>(options =>
                {
                    // Удаляем все HealthCheck'и
                    foreach (var registration in options.Registrations.ToList())
                        options.Registrations.Remove(registration);
                });
            });
        }).CreateClient();
        var collector = new MetricCollector<double>(_meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.HEALTHZ_URL);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

        try
        {
            await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Fail("Метрики найдены, хотя не должны");
        }
        catch (TimeoutException) {}
    }
}