using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к Prometheus.
/// </summary>
public class PrometheusConnectionHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<MetricsOptions> _options;
    private readonly ILogger<PrometheusConnectionHealthCheck> _logger;

    public PrometheusConnectionHealthCheck(IHttpClientFactory httpClientFactory, IOptions<MetricsOptions> options, ILogger<PrometheusConnectionHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Создание клиента
        var client = _httpClientFactory.CreateClient(HttpClientNames.Prometheus);
        client.Timeout = TimeSpan.FromMilliseconds(1000);

        // Создаём запрос
        var url = "";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        try
        {
            // Отправляем запрос
            using var result = await client.SendAsync(request, cancellationToken);
            bool isHealthy = result.IsSuccessStatusCode;

            return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy($"Failed to connect to the prometheus. {result.ReasonPhrase}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключится к Prometheus по причине: {message}.", ex.Message);
            return HealthCheckResult.Unhealthy("Failed to connect to the prometheus.");
        }
    }
}