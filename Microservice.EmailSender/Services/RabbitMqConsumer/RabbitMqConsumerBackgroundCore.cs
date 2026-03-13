using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Microservice.EmailSender.Services.RabbitMqConsumer;

/// <inheritdoc cref="IEmailSenderBackgroundCore"/>
public class RabbitMqConsumerBackgroundCore : IRabbitMqConsumerBackgroundCore
{
    private readonly IQueueEmail _queueEmail;
    private readonly ILogger<RabbitMqConsumerBackgroundCore> _logger;

    /// <summary>
    /// Название обменника.
    /// </summary>
    private const string ExchangeName = "informs";

    /// <summary>
    /// Название очереди обменника <see cref="ExchangeName"/>.
    /// </summary>
    private const string QueueName = "informs-consumer-1";

    public RabbitMqConsumerBackgroundCore(IQueueEmail queueEmail, ILogger<RabbitMqConsumerBackgroundCore> logger)
    {
        _queueEmail = queueEmail;
        _logger = logger;
    }

    public async Task DoWorkAsync(IChannel channel, CancellationToken ct = default)
    {
        // Объявляем Exchange (обменник)
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout, // Каждое сообщение, которое попадает в обменник, копируется во все связанные очереди это обменника (т.е у потребителей свои очереди, но с одинаковым содержанием)
            durable: true, // Очередь не удалится после перезапуска Rabbit'а (сообщения внутри удалятся, если только не указать Persistent в BasicPublishAsync)
            autoDelete: false, // Не удалять очередь даже, когда все потребители отключатся
            cancellationToken: ct);

        // Объявляем очередь для этого потребителя
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true, // Очередь не удалится после перезапуска Rabbit'а (сообщения внутри удалятся, если только не указать Persistent в BasicPublishAsync)
            exclusive: false, // Очередь может использоваться другими соединениями, а не только текущим (получить, удалить и тд)
            autoDelete: false, // Не удалять очередь даже, когда все потребители отключатся
            arguments: null,
            cancellationToken: ct);

        // Привязываем очередь к обменнику
        await channel.QueueBindAsync(QueueName, ExchangeName, routingKey: string.Empty, cancellationToken: ct);

        // Начинаем прослушивать сообщения из очереди
        var consumer = new AsyncEventingBasicConsumer(channel); // Создаём потребителя
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var ct = eventArgs.CancellationToken;
            var channel = ((AsyncEventingBasicConsumer)sender).Channel;

            try
            {
                // Десериализуем сообщение
                var receivedLetter = JsonSerializer.Deserialize<EnqueueLetterRequest>(eventArgs.Body.Span);

                _logger.LogDebug("Сообщение с тегом \"{tag}\" получено.", eventArgs.DeliveryTag);

                // Не удалось пропарсить Guid
                if (receivedLetter == null || !Guid.TryParse(receivedLetter.Id, out Guid letterId))
                {
                    _logger.LogError("Некорректные данные сообщения \"{tag}\".", eventArgs.DeliveryTag);

                    // Отклоняем сообщение без повторного добавления в очередь (requeue: false), т.к некорректные данные
                    await channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false, ct);
                    return;
                }

                // Добавляем письмо в очередь на отправку
                var letter = new Letter(letterId, receivedLetter.Email, receivedLetter.Subject, receivedLetter.Body);
                await _queueEmail.EnqueueAsync(letter, ct);

                // Подтверждаем получение сообщения
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, ct); // multiple: false, подтверждаем конкретно это сообщение, а не всю пачку

                _logger.LogInformation("Письмо \"{id}\" успешно поставлено в очередь на отправку.", receivedLetter.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке сообщения \"{tag}\". Добавлялось ли сообщение в очередь повторно ранее: {redelivered}.", eventArgs.DeliveryTag, eventArgs.Redelivered);

                // Если сообщение до этого уже повторно добавлялось в очередь
                if (eventArgs.Redelivered) // Отклоняем сообщение без повторного добавления в очередь
                    await channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: false, ct);
                else // Если же до этого не было повторных попыток, то добавляем в очередь
                    await channel.BasicRejectAsync(eventArgs.DeliveryTag, requeue: true, ct);
            }
        };

        // Подписка на очередь
        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, ct); // autoAck: false, без автоподтверждения получения сообщений
    }
}