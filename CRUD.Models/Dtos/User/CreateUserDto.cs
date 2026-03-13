using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для создания пользователя.
/// </summary>
public class CreateUserDto
{
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
    /// Пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; set; }

    /// <summary>
    /// Электронная почта пользователя.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    /// <summary>
    /// Телефонный номер пользователя.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; set; }
}