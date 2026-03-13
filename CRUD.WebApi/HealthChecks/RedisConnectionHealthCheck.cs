using StackExchange.Redis;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к Redis.
/// </summary>
public class RedisConnectionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisConnectionHealthCheck> _logger;

    public RedisConnectionHealthCheck(IConfiguration configuration, ILogger<RedisConnectionHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(_configuration.GetConnectionString("RedisConnection")!, configure =>
            {
                configure.ConnectTimeout = 1000;
                configure.ConnectRetry = 0;
            });

            bool isHealthy = connection.IsConnected;
            return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the redis.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключится к Redis по причине: {message}.", ex.Message);
            return HealthCheckResult.Unhealthy("Failed to connect to the redis.");
        }
    }
}