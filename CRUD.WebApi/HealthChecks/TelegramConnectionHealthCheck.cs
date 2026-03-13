using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к Telegram серверу.
/// </summary>
public class TelegramConnectionHealthCheck : IHealthCheck
{
    private readonly ITelegramIntegrationManager _telegramIntegrationManager;

    public TelegramConnectionHealthCheck(ITelegramIntegrationManager telegramIntegrationManager)
    {
        _telegramIntegrationManager = telegramIntegrationManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isHealthy = await _telegramIntegrationManager.CheckConnectionAsync(cancellationToken);

        return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the Telegram server.");
    }
}