using System.Text.Json.Serialization;

namespace CRUD.Models;

/// <summary>
/// Объект оплаты.
/// </summary>
/// <remarks>
/// <seealso href="https://yookassa.ru/developers/api#payment_object"/>
/// </remarks>
public sealed record PaymentResponse
{
    /// <summary>
    /// Id оплаты.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Статус оплаты.
    /// </summary>
    /// <remarks>
    /// Из констант <see cref="PaymentStatuses"/>.
    /// </remarks>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Оплачено ли.
    /// </summary>
    [JsonPropertyName("paid")]
    public required bool Paid { get; init; }

    /// <summary>
    /// Сумма.
    /// </summary>
    [JsonPropertyName("amount")]
    public required Amount Amount { get; init; }

    /// <summary>
    /// Подтверждение.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/api#payment_object_confirmation"/>
    /// </remarks>
    [JsonPropertyName("confirmation")]
    public required Confirmation Confirmation { get; init; } // Этот параметр необязательно может прийти, но я указываю, чтобы пришёл "Redirect", в теории обязательный

    /// <summary>
    /// Дата создания оплаты.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Описание.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; } // Тоже в теории обязательный. В оплате всегда указываю описание

    /// <summary>
    /// Аккаунт получателя.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required Recipient Recipient { get; init; }

    /// <summary>
    /// Можно ли вернуть деньги.
    /// </summary>
    [JsonPropertyName("refundable")]
    public required bool Refundable { get; init; }

    /// <summary>
    /// Тестовая ли оплата.
    /// </summary>
    [JsonPropertyName("test")]
    public required bool Test { get; init; }
}

/// <summary>
/// Сумма.
/// </summary>
public sealed record Amount
{
    /// <summary>
    /// Значение.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>
    /// Валюта.
    /// </summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }
}

/// <summary>
/// Подтверждение.
/// </summary>
public sealed record Confirmation
{
    /// <summary>
    /// Тип.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Ссылка на оплату заказа.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://yookassa.ru/developers/api#payment_object_confirmation"/>
    /// </remarks>
    [JsonPropertyName("confirmation_url")]
    public required string ConfirmationUrl { get; init; } // Конкретно в случае "Redirect", это обязательное поле
}

/// <summary>
/// Аккаунт получателя.
/// </summary>
public sealed record Recipient
{
    /// <summary>
    /// Идентификатор магазина.
    /// </summary>
    [JsonPropertyName("account_id")]
    public required string AccountId { get; init; }

    /// <summary>
    /// Идентификатор субаккаунта.
    /// </summary>
    [JsonPropertyName("gateway_id")]
    public required string GatewayId { get; init; }
}