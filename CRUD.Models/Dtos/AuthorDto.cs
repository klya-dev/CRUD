using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель автора.
/// </summary>
public class AuthorDto
{
    /// <summary>
    /// Имя автора.
    /// </summary>
    [JsonPropertyName("firstname")]
    public required string Firstname { get; set; }

    /// <summary>
    /// Username автора.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// Код языка автора.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public required string LanguageCode { get; set; }

    /// <summary>
    /// Количество публикаций автора.
    /// </summary>
    [JsonPropertyName("publicationsCount")]
    public required int PublicationsCount { get; set; }
}