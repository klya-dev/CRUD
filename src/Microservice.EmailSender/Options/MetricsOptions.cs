namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции метрик.
/// </summary>
public sealed class MetricsOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Metrics";

    /// <summary>
    /// URL Prometheus'а с портом.
    /// </summary>
    public required string PrometheusURL { get; init; }

    /// <summary>
    /// Имя пользователя для авторизации.
    /// </summary>
    public required string User { get; init; }

    /// <summary>
    /// Пароль пользователя для авторизации.
    /// </summary>
    public required string Password { get; init; }
}