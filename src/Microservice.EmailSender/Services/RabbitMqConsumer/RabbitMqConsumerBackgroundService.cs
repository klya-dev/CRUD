using RabbitMQ.Client;

namespace Microservice.EmailSender.Services.RabbitMqConsumer;

/// <summary>
/// Сервис для обработки сообщений из RabbitMQ в фоне.
/// </summary>
public sealed class RabbitMqConsumerBackgroundService : BackgroundService
{
    private readonly IRabbitMqConsumerBackgroundCore _rabbitMqConsumerBackgroundCore;
    private readonly ILogger<RabbitMqConsumerBackgroundService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly string Hostname;
    private readonly int Port;

    private readonly int MaxRetries = 5;
    private readonly int DelaySeconds = 3;

    public RabbitMqConsumerBackgroundService(IRabbitMqConsumerBackgroundCore rabbitMqConsumerBackgroundCore, ILogger<RabbitMqConsumerBackgroundService> logger, IConfiguration configuration)
    {
        _rabbitMqConsumerBackgroundCore = rabbitMqConsumerBackgroundCore;
        _logger = logger;

        // Получаем строку подключения и разбиваем на части Hostname и Port
        var connectionString = configuration.GetConnectionString("RabbitMqConnection") ?? string.Empty;

        // Пытаемся пропарсить строку подключения
        if (ConnectionParser.TryCreateUri(connectionString, out Uri? uri, "amqp", 5672))
        {
            Hostname = uri.Host;
            Port = uri.Port;
        }
        else
        {
            Hostname = "localhost";
            Port = 5672;
        }

        _logger.StartedBackgroundServiceLog(nameof(RabbitMqConsumerBackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Создаём фабрику
        var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };

        // Быстренькая интерпретация RetryPolicy, лучше через Polly, конечно
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                // Подключаемся к RabbitMQ
                _connection = await factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

                await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_channel, ct);
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.StopedBackgroundServiceLog(nameof(RabbitMqConsumerBackgroundService));
                break;
            }
            catch (Exception ex) when (i < MaxRetries - 1)
            {
                _logger.LogWarning("Не удалось подключится к RabbitMQ: {Message}. Повторная попытка через {Seconds}с...", ex.Message, DelaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(DelaySeconds), ct);
            }
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