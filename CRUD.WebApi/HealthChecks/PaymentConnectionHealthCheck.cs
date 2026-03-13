using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к платёжному серверу.
/// </summary>
public class PaymentConnectionHealthCheck : IHealthCheck
{
    private readonly IPayManager _payManager;

    public PaymentConnectionHealthCheck(IPayManager payManager)
    {
        _payManager = payManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isHealthy = await _payManager.CheckConnectionAsync(cancellationToken);

        return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the payment server.");
    }
}