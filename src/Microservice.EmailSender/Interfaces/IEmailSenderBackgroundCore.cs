using MailKit.Net.Smtp;

namespace Microservice.EmailSender.Interfaces;

/// <summary>
/// Сервис реализации логики отправки электронных писем в фоне.
/// </summary>
public interface IEmailSenderBackgroundCore
{
    /// <summary>
    /// Создаёт несколько <see cref="SmtpClient"/>'ов и подключает их к серверу.
    /// </summary>
    /// <remarks>
    /// При отключении от сервера будет попытка переподключения.
    /// </remarks>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Список подключённых <see cref="SmtpClient"/>'ов.</returns>
    Task<List<SmtpClient>> CreateSmtpClientsAsync(CancellationToken ct = default);

    /// <summary>
    /// Отправляет электронные письма, скопившиеся в очереди.
    /// </summary>
    /// <remarks>
    /// <para>Реализованные функции:</para>
    /// <list type="bullet">
    /// <item>Одновременная обработка нескольких писем.</item>
    /// <item>Повторные попытки.</item>
    /// <item>Таймаут у повторных попыток.</item>
    /// <item>Пресечение возможности отправлять на одну и ту же почту более X писем в час.</item>
    /// </list>
    /// </remarks>
    /// <param name="smtpClients"><see cref="SmtpClient"/>'ы, полученные из метода <see cref="CreateSmtpClientsAsync"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DoWorkAsync(List<SmtpClient> smtpClients, CancellationToken ct);
}