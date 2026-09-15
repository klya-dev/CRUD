using MailKit.Net.Smtp;

namespace Microservice.EmailSender.Interfaces;

/// <summary>
/// Сервис для работы с электронной почтой.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Подключает и авторизует созданный <see cref="SmtpClient"/> на сервере.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Подключённый <see cref="SmtpClient"/>.</returns>
    Task<SmtpClient> ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Синхронно подключает указанный <see cref="SmtpClient"/> к серверу.
    /// </summary>
    /// <param name="smtpClient">Не подключённый <see cref="SmtpClient"/>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="smtpClient"/> <see langword="null"/>.</exception>
    void Connect(SmtpClient smtpClient);

    /// <summary>
    /// Отключает указанный <see cref="SmtpClient"/> от сервера.
    /// </summary>
    /// <param name="smtpClient">Подключённый <see cref="SmtpClient"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="smtpClient"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    Task DisconnectAsync(SmtpClient smtpClient, CancellationToken ct = default);

    /// <summary>
    /// Отправляет электронное письмо с указанным, подключённым <see cref="SmtpClient"/>.
    /// </summary>
    /// <remarks>
    /// Подходит для отправки нескольких писем без переподключения к серверу.
    /// </remarks>
    /// <param name="letter">Электронное письмо.</param>
    /// <param name="smtpClient">Подключённый <see cref="SmtpClient"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="letter"/> или <paramref name="smtpClient"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если письмо успешно отправлено.</returns>
    Task<bool> SendEmailAsync(Letter letter, SmtpClient smtpClient, CancellationToken ct = default);

    /// <summary>
    /// Отправляет электронное письмо.
    /// </summary>
    /// <remarks>
    /// Подходит для отправки одного письма, т.к. этот метод при каждом вызове переподключается к серверу.
    /// </remarks>
    /// <param name="letter">Электронное письмо.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="letter"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если письмо успешно отправлено.</returns>
    Task<bool> SendEmailAsync(Letter letter, CancellationToken ct = default);
}