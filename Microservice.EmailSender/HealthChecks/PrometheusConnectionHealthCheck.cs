using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace Microservice.EmailSender.HealthChecks;

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
        var url = _options.Value.PrometheusURL;
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(1000);

        try
        {
            using var result = await client.GetAsync(url, cancellationToken);
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