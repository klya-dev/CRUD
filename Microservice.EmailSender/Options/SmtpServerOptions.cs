namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции SmtpServer'а.
/// </summary>
public class SmtpServerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "SmtpServer";

    /// <summary>
    /// Хост.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// Порт.
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// Пароль аутентификации.
    /// </summary>
    public required string AuthPassword { get; set; }

    /// <summary>
    /// Отображаемое имя отправителя.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Почта отправителя.
    /// </summary>
    public required string Email { get; set; }
}