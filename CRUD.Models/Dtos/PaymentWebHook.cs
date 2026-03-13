using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// Вебхук оплаты.
/// </summary>
/// <remarks>
/// <seealso href="https://yookassa.ru/developers/using-api/webhooks#using"/>
/// </remarks>
public class PaymentWebHook
{
    /// <summary>
    /// Тип.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Событие.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/using-api/webhooks#events"/>
    /// </remarks>
    [JsonPropertyName("event")]
    public required string Event { get; set; }

    /// <summary>
    /// Объект платежа, с которым произошло указанное событие.
    /// </summary>
    [JsonPropertyName("object")]
    public required object Object { get; set; }
}