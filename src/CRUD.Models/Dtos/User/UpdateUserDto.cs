using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для обновления данных пользователя.
/// </summary>
public sealed record UpdateUserDto
{
    /// <summary>
    /// Новое имя пользователя.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; init; }

    /// <summary>
    /// Новый username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// Новый код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; init; }
}