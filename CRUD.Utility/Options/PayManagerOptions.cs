namespace CRUD.Utility.Options;

/// <summary>
/// Опции PayManager'а.
/// </summary>
public class PayManagerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "PayManager";

    /// <summary>
    /// URL сервиса (провайдера).
    /// </summary>
    public required string ServiceURL { get; set; }

    /// <summary>
    /// Id магазина.
    /// </summary>
    public required string ShopId { get; set; }

    /// <summary>
    /// API-ключ.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Список безопасных (разрешённых) IP-адресов через ';'.
    /// </summary>
    /// <remarks>
    /// Список отсюда <see href="https://yookassa.ru/developers/using-api/webhooks#ip"/>.
    /// </remarks>
    public required string SafeListIp { get; set; }
}