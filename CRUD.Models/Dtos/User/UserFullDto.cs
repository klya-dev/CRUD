using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель полных данных о пользователе.
/// </summary>
/// <remarks>
/// Для админ-панели.
/// </remarks>
public sealed record UserFullDto
{
    /// <summary>
    /// Id пользователя.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; init; }

    /// <summary>
    /// Username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Является ли пользователь премиумом.
    /// </summary>
    [JsonPropertyName("isPremium")]
    public required bool IsPremium { get; init; }

    /// <summary>
    /// API-ключ пользователя.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public required string? ApiKey { get; init; }

    /// <summary>
    /// Одноразовый API-ключ пользователя.
    /// </summary>
    [JsonPropertyName("disposableApiKey")]
    public required string? DisposableApiKey { get; init; }

    /// <summary>
    /// URL-путь аватарки пользователя.
    /// </summary>
    [JsonPropertyName("avatarUrl")]
    public required string AvatarURL { get; init; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Подтверждёна ли электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("isEmailConfirm")]
    public required bool IsEmailConfirm { get; init; }

    /// <summary>
    /// Телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// Подтверждён ли телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("isPhoneNumberConfirm")]
    public required bool IsPhoneNumberConfirm { get; init; }
}