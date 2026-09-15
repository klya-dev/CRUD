using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Net.Http.Headers;
using System.Text;

namespace Microservice.EmailSender.Tests.SystemTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class MetricsSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<Options.MetricsOptions> _metricsOptions;

    public MetricsSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _meterFactory = scopedServices.GetRequiredService<IMeterFactory>();
        _metricsOptions = scopedServices.GetRequiredService<IOptions<Options.MetricsOptions>>();
    }

    [Fact]
    public async Task Get_Metrics_ReturnsDoesMetrics()
    {
        // Arrange
        var client = _factory.HttpClient;
        var collector = new MetricCollector<double>(_meterFactory, "Microsoft.AspNetCore.Hosting", "http.server.request.duration");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.METRICS_URL);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_metricsOptions.Value.User}:{_metricsOptions.Value.Password}")));

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

        // Читаем содержимое ответа
        var response = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(response);

        await collector.WaitForMeasurementsAsync(minCount: 1, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Collection(collector.GetMeasurementSnapshot(),
            measurement =>
            {
                Assert.Equal("http", measurement.Tags["url.scheme"]);
                Assert.Equal("GET", measurement.Tags["http.request.method"]);
                Assert.Equal("/metrics", measurement.Tags["http.route"]);
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
        TestConstants.AddBearerToken(request);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

        try
        {
            await collector.WaitForMeasurementsAsync(minCount: 1, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Fail("Метрики найдены, хотя не должны");
        }
        catch (TimeoutException) {}
    }
}