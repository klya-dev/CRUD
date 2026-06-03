using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// Вебхук оплаты.
/// </summary>
/// <remarks>
/// <seealso href="https://yookassa.ru/developers/using-api/webhooks#using"/>
/// </remarks>
public sealed record PaymentWebHook
{
    /// <summary>
    /// Тип.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Событие.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/using-api/webhooks#events"/>
    /// </remarks>
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    /// <summary>
    /// Объект платежа, с которым произошло указанное событие.
    /// </summary>
    [JsonPropertyName("object")]
    public required object Object { get; init; }
}