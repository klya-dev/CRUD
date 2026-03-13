using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CRUD.Services;

/// <inheritdoc cref="IPremiumInformator"/>
public class PremiumInformator : IPremiumInformator
{
    private readonly ILogger<PremiumInformator> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly string Hostname;
    private readonly int Port;

    /// <summary>
    /// Название обменника.
    /// </summary>
    private const string ExchangeName = "informs";

    public PremiumInformator(ILogger<PremiumInformator> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        // Получаем строку подключения и разбиваем на части Hostname и Port
        var connectionString = configuration.GetConnectionString("RabbitMqConnection") ?? string.Empty;
        var parts = connectionString.Split(':');
        Hostname = parts[0];
        Port = parts.Length > 1 ? int.Parse(parts[1]) : 5672; // Если часть одна, то используем дефолтный порт
    }

    public async Task InformateAsync(string email, string languageCode, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(languageCode);

        // Формируем письмо
        var letter = EmailLetters.GetLetter(EmailLetters.InformGettingPremium, email, languageCode, _httpContextAccessor.GetBaseUrl());

        // Подключаемся к RabbitMQ
        var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };
        using var connection = await factory.CreateConnectionAsync(ct);
        using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        // Объявляем Exchange (обменник)
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout, // Каждое сообщение, которое попадает в обменник, копируется во все связанные очереди это обменника (т.е у потребителей свои очереди, но с одинаковым содержанием)
            durable: true, // Очередь не удалится после перезапуска Rabbit'а (сообщения внутри удалятся, если только не указать Persistent в BasicPublishAsync)
            autoDelete: false, // Не удалять очередь даже, когда все потребители отключатся
            cancellationToken: ct);

        // Создаём сообщение
        // Тут я использую класс из gRPC .proto, что не очень хорошо, т.к прямая зависимость к gRPC. Решил не создавать отдельный класс
        var request = new EnqueueLetterRequest
        {
            Id = letter.Id.ToString(),
            Email = letter.Email,
            Subject = letter.Subject,
            Body = letter.Body
        };
        var message = JsonSerializer.Serialize(request);
        var body = Encoding.UTF8.GetBytes(message);

        // Опубликовываем сообщение
        await channel.BasicPublishAsync(
            exchange: ExchangeName, // Сообщения отправляются в обменник
            routingKey: string.Empty, // Для типа ExchangeType.Fanout неважен ключ маршрутизации, т.к все сообщения попадают во все связанные очереди 
            mandatory: true, // Если сообщение не смогло дойти до обменника/очереди, то сообщение не пропадает, а приходит в BasicReturnAsync
            basicProperties: new BasicProperties { Persistent = true }, // Сообщение будет сохранятся на диск, чтобы не удалилось при перезагрузке брокера (работает вместе с durable у очереди)
            body: body,
            ct);

        _logger.LogInformation("Сообщение: \"{id}\", добавлено в RabbitMQ обменник \"{queue}\".", letter.Id, ExchangeName);
    }
}