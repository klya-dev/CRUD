using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель минимальных данных о пользователе.
/// </summary>
public sealed record UserDto
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
    /// Код языка пользователя.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Временная ссылка на аватарку пользователя.
    /// </summary>
    /// <remarks>
    /// Может быть <see langword="null"/>, если не удалось получить.
    /// </remarks>
    [JsonPropertyName("avatarPresignedUrl")]
    public required string? AvatarPresignedUrl { get; init; }
}