namespace CRUD.Utility.Options;

/// <summary>
/// Опции TelegramIntegrationManager'а.
/// </summary>
public sealed class TelegramIntegrationOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "TelegramIntegration";

    /// <summary>
    /// URL сервиса (шлюза).
    /// </summary>
    public required string ServiceURL { get; init; }

    /// <summary>
    /// API-ключ.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Если сообщение не будет доставлено или прочитано в течение этого времени (в секундах), плата за запрос будет возвращена.
    /// </summary>
    public required int TimeToLive { get; init; }
}