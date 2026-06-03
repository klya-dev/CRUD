using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель автора.
/// </summary>
public sealed record AuthorDto
{
    /// <summary>
    /// Имя автора.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; init; }

    /// <summary>
    /// Username автора.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// Код языка автора.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Количество публикаций автора.
    /// </summary>
    [JsonPropertyName("publicationsCount")]
    public required int PublicationsCount { get; init; }
}