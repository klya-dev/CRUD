namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции SmtpServer'а.
/// </summary>
public sealed class SmtpServerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "SmtpServer";

    /// <summary>
    /// Хост.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Порт.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Пароль аутентификации.
    /// </summary>
    public required string AuthPassword { get; init; }

    /// <summary>
    /// Отображаемое имя отправителя.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Почта отправителя.
    /// </summary>
    public required string Email { get; init; }
}