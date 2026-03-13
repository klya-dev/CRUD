namespace CRUD.Utility.Options;

/// <summary>
/// Опции фонового сервиса RevokeExpiredRefreshTokensBackgroundService.
/// </summary>
public class RevokeExpiredRefreshTokensBackgroundServiceOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "BackgroundServices:RevokeExpiredRefreshTokensBackgroundService";

    /// <summary>
    /// Промежуток между итерациями.
    /// </summary>
    /// <remarks>
    /// Например, 10 минут, значит раз в десять минут будут удаляться истёкшие Refresh-токены.
    /// </remarks>
    public required TimeSpan Timer { get; set; }
}