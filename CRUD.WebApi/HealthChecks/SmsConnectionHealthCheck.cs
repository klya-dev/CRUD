namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к СМС серверу.
/// </summary>
public class SmsConnectionHealthCheck : IHealthCheck
{
    private readonly ISmsSender _smsSender;

    public SmsConnectionHealthCheck(ISmsSender smsSender)
    {
        _smsSender = smsSender;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isHealthy = await _smsSender.TestAuthAsync(cancellationToken);

        return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the SMS server.");
    }
}