using RabbitMQ.Client;

namespace Microservice.EmailSender.HealthChecks;

/// <summary>
/// Проверяет подключение к RabbitMQ.
/// </summary>
public class RabbitMqConnectionHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConnectionHealthCheck> _logger;

    private readonly string Hostname;
    private readonly int Port;

    public RabbitMqConnectionHealthCheck(IConfiguration configuration, ILogger<RabbitMqConnectionHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Получаем строку подключения и разбиваем на части Hostname и Port
        var connectionString = configuration.GetConnectionString("RabbitMqConnection") ?? string.Empty;
        var parts = connectionString.Split(':');
        Hostname = parts[0];
        Port = parts.Length > 1 ? int.Parse(parts[1]) : 5672; // Если часть одна, то используем дефолтный порт
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Подключаемся к RabbitMQ
            var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };
            using var connection = await factory.CreateConnectionAsync(cancellationToken);

            bool isHealthy = connection.IsOpen;
            return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the RabbitMQ.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключится к RabbitMQ по причине: {message}.", ex.Message);
            return HealthCheckResult.Unhealthy("Failed to connect to the RabbitMQ.");
        }
    }
}