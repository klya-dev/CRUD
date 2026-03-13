using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель полных данных о пользователе.
/// </summary>
/// <remarks>
/// Для админ-панели.
/// </remarks>
public class UserFullDto
{
    /// <summary>
    /// Id пользователя.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; set; }

    /// <summary>
    /// Username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; set; }

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// Является ли пользователь премиумом.
    /// </summary>
    [JsonPropertyName("isPremium")]
    public required bool IsPremium { get; set; }

    /// <summary>
    /// API-ключ пользователя.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public required string? ApiKey { get; set; }

    /// <summary>
    /// Одноразовый API-ключ пользователя.
    /// </summary>
    [JsonPropertyName("disposableApiKey")]
    public required string? DisposableApiKey { get; set; }

    /// <summary>
    /// URL-путь аватарки пользователя.
    /// </summary>
    [JsonPropertyName("avatarUrl")]
    public required string AvatarURL { get; set; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Подтверждёна ли электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("isEmailConfirm")]
    public required bool IsEmailConfirm { get; set; }

    /// <summary>
    /// Телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// Подтверждён ли телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("isPhoneNumberConfirm")]
    public required bool IsPhoneNumberConfirm { get; set; }
}