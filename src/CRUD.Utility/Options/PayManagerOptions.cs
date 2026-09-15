namespace CRUD.Utility.Options;

/// <summary>
/// Опции PayManager'а.
/// </summary>
public sealed class PayManagerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "PayManager";

    /// <summary>
    /// URL сервиса (провайдера).
    /// </summary>
    public required string ServiceURL { get; init; }

    /// <summary>
    /// Id магазина.
    /// </summary>
    public required string ShopId { get; init; }

    /// <summary>
    /// API-ключ.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Список безопасных (разрешённых) IP-адресов через ';'.
    /// </summary>
    /// <remarks>
    /// Список отсюда <see href="https://yookassa.ru/developers/using-api/webhooks#ip"/>.
    /// </remarks>
    public required string SafeListIp { get; init; }
}