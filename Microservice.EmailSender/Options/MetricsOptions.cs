namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции метрик.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Metrics";

    /// <summary>
    /// URL Prometheus'а с портом.
    /// </summary>
    public required string PrometheusURL { get; set; }
}