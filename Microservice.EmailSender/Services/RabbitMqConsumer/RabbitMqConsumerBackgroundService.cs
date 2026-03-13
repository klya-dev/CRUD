using RabbitMQ.Client;

namespace Microservice.EmailSender.Services.RabbitMqConsumer;

/// <summary>
/// Сервис для обработки сообщений из RabbitMQ в фоне.
/// </summary>
public class RabbitMqConsumerBackgroundService : BackgroundService
{
    private readonly IRabbitMqConsumerBackgroundCore _rabbitMqConsumerBackgroundCore;
    private readonly ILogger<RabbitMqConsumerBackgroundService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly string Hostname;
    private readonly int Port;

    public RabbitMqConsumerBackgroundService(IRabbitMqConsumerBackgroundCore rabbitMqConsumerBackgroundCore, ILogger<RabbitMqConsumerBackgroundService> logger, IConfiguration configuration)
    {
        _rabbitMqConsumerBackgroundCore = rabbitMqConsumerBackgroundCore;
        _logger = logger;

        // Получаем строку подключения и разбиваем на части Hostname и Port
        var connectionString = configuration.GetConnectionString("RabbitMqConnection") ?? string.Empty;
        var parts = connectionString.Split(':');
        Hostname = parts[0];
        Port = parts.Length > 1 ? int.Parse(parts[1]) : 5672; // Если часть одна, то используем дефолтный порт

        _logger.StartedBackgroundServiceLog(nameof(RabbitMqConsumerBackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Подключаемся к RabbitMQ
        var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        try
        {
            await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_channel, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.StopedBackgroundServiceLog(nameof(RabbitMqConsumerBackgroundService));
        }
    }

    public override void Dispose()
    {
        _connection?.Dispose();
        _channel?.Dispose();
        base.Dispose();

        GC.SuppressFinalize(this);
    }
}