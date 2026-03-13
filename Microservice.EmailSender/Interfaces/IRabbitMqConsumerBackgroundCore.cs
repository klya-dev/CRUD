using RabbitMQ.Client;

namespace Microservice.EmailSender.Interfaces;

/// <summary>
/// Сервис реализации логики принятия сообщений RabbitMQ в фоне.
/// </summary>
public interface IRabbitMqConsumerBackgroundCore
{
    /// <summary>
    /// Подписывается на обработчик, и обрабатывает входящие сообщения.
    /// </summary>
    /// <param name="channel">Подключённый канал.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DoWorkAsync(IChannel channel, CancellationToken ct = default);
}