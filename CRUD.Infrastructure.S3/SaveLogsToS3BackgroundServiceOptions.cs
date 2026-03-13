namespace CRUD.Infrastructure.S3;

/// <summary>
/// Опции фонового сервиса <see cref="SaveLogsToS3BackgroundService"/>.
/// </summary>
public class SaveLogsToS3BackgroundServiceOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "BackgroundServices:SaveLogsToS3BackgroundService";

    /// <summary>
    /// Промежуток между итерациями.
    /// </summary>
    /// <remarks>
    /// Например, 1 день, значит раз в день будут записываться файл-логи в облачное хранилище.
    /// </remarks>
    public required TimeSpan Timer { get; set; }
}