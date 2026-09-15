using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для создания пользователя.
/// </summary>
public sealed record CreateUserDto
{
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
    /// Пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }

    /// <summary>
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; init; }
}