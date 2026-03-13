namespace CRUD.Utility.Options;

/// <summary>
/// Опции фонового сервиса DeleteExpiredRequestsBackgroundService.
/// </summary>
public class DeleteExpiredRequestsBackgroundServiceOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "BackgroundServices:DeleteExpiredRequestsBackgroundService";

    /// <summary>
    /// Промежуток между итерациями.
    /// </summary>
    /// <remarks>
    /// Например, 1 день, значит раз в день минут будут удаляться истёкшие запросы.
    /// </remarks>
    public required TimeSpan Timer { get; set; }
}