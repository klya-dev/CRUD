using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// Информация о пользователе OpenId.
/// </summary>
/// <remarks>
/// <seealso href="https://id.vk.com/about/business/go/docs/ru/vkid/latest/oauth/oauth-mail/index#Kak-poluchit-informaciyu-o-polzovatele-po-tokenu"/>.
/// </remarks>
public sealed record OpenIdUserInfo
{
    /// <summary>
    /// Идентификатор учетной записи.
    /// </summary>
    [JsonPropertyName("sub")]
    public required string Sub { get; init; }

    /// <summary>
    /// Имя и фамилия.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Имя.
    /// </summary>
    [JsonPropertyName("given_name")]
    public required string GivenName { get; init; }

    /// <summary>
    /// Фамилия.
    /// </summary>
    [JsonPropertyName("family_name")]
    public required string FamilyName { get; init; }

    /// <summary>
    /// Никнейм (псевдоним).
    /// </summary>
    [JsonPropertyName("nickname")]
    public required string Nickname { get; init; }

    /// <summary>
    /// Аватар пользователя.
    /// </summary>
    [JsonPropertyName("picture")]
    public required string Picture { get; init; }

    /// <summary>
    /// Пол.
    /// </summary>
    [JsonPropertyName("gender")]
    public required string Gender { get; init; }

    /// <summary>
    /// День рождения.
    /// </summary>
    [JsonPropertyName("birthdate")]
    public required DateTime? Birthdate { get; init; }

    /// <summary>
    /// Язык и регион.
    /// </summary>
    [JsonPropertyName("locale")]
    public required string Locale { get; init; }

    /// <summary>
    /// Электронная почта.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; init; }
}