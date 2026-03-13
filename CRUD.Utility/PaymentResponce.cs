using System.Text.Json.Serialization;

namespace CRUD.Utility;

/// <summary>
/// Объект оплаты.
/// </summary>
/// <remarks>
/// <seealso href="https://yookassa.ru/developers/api#payment_object"/>
/// </remarks>
public class PaymentResponse
{
    /// <summary>
    /// Id оплаты.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Статус оплаты.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="PaymentStatuses"/>.
    /// </remarks>
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    /// <summary>
    /// Оплачено ли.
    /// </summary>
    [JsonPropertyName("paid")]
    public required bool Paid { get; set; }

    /// <summary>
    /// Сумма.
    /// </summary>
    [JsonPropertyName("amount")]
    public required Amount Amount { get; set; }

    /// <summary>
    /// Подтверждение.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/api#payment_object_confirmation"/>
    /// </remarks>
    [JsonPropertyName("confirmation")]
    public required Confirmation Confirmation { get; set; } // Этот параметр необязательно может прийти, но я указываю, чтобы пришёл "Redirect", в теории обязательный

    /// <summary>
    /// Дата создания оплаты.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Описание.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; } // Тоже в теории обязательный. В оплате всегда указываю описание

    /// <summary>
    /// Аккаунт получателя.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required Recipient Recipient { get; set; }

    /// <summary>
    /// Можно ли вернуть деньги.
    /// </summary>
    [JsonPropertyName("refundable")]
    public required bool Refundable { get; set; }

    /// <summary>
    /// Тестовая ли оплата.
    /// </summary>
    [JsonPropertyName("test")]
    public required bool Test { get; set; }
}

/// <summary>
/// Сумма.
/// </summary>
public class Amount
{
    /// <summary>
    /// Значение.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    /// <summary>
    /// Валюта.
    /// </summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }
}

/// <summary>
/// Подтверждение.
/// </summary>
public class Confirmation
{
    /// <summary>
    /// Тип.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Ссылка на оплату заказа.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/api#payment_object_confirmation"/>
    /// </remarks>
    [JsonPropertyName("confirmation_url")]
    public required string ConfirmationUrl { get; set; } // Конкретно в случае "Redirect", это обязательное поле
}

/// <summary>
/// Аккаунт получателя.
/// </summary>
public class Recipient
{
    /// <summary>
    /// Идентификатор магазина.
    /// </summary>
    [JsonPropertyName("account_id")]
    public required string AccountId { get; set; }

    /// <summary>
    /// Идентификатор субаккаунта.
    /// </summary>
    [JsonPropertyName("gateway_id")]
    public required string GatewayId { get; set; }
}