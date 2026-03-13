namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к базе данных.
/// </summary>
public class DatabaseConnectionHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DatabaseConnectionHealthCheck(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isHealthy = await _db.Database.CanConnectAsync(cancellationToken);

        return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the database.");
    }
}