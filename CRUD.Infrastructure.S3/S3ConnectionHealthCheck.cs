using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRUD.Infrastructure.S3;

/// <summary>
/// Проверяет подключение к S3.
/// </summary>
public class S3ConnectionHealthCheck : IHealthCheck
{
    private readonly IS3Manager _s3Manager;

    public S3ConnectionHealthCheck(IS3Manager s3Manager)
    {
        _s3Manager = s3Manager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isConnection = await _s3Manager.CheckConnectionAsync(cancellationToken);

        return isConnection ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to S3.");
    }
}