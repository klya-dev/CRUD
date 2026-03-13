using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для обновления данных пользователя.
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Новое имя пользователя.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; set; }

    /// <summary>
    /// Новый username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// Новый код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; set; }
}