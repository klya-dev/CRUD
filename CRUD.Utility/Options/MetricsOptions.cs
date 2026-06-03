namespace CRUD.Utility.Options;

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
}