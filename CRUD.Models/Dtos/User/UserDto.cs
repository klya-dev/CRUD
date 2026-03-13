using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель минимальных данных о пользователе.
/// </summary>
public class UserDto
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
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; set; }
}