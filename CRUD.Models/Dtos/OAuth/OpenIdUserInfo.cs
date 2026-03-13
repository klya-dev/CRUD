using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// Информация о пользователе OpenId.
/// </summary>
/// <remarks>
/// <seealso href="https://id.vk.com/about/business/go/docs/ru/vkid/latest/oauth/oauth-mail/index#Kak-poluchit-informaciyu-o-polzovatele-po-tokenu"/>.
/// </remarks>
public class OpenIdUserInfo
{
    /// <summary>
    /// Идентификатор учетной записи.
    /// </summary>
    [JsonPropertyName("sub")]
    public required string Sub { get; set; }

    /// <summary>
    /// Имя и фамилия.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Имя.
    /// </summary>
    [JsonPropertyName("given_name")]
    public required string GivenName { get; set; }

    /// <summary>
    /// Фамилия.
    /// </summary>
    [JsonPropertyName("family_name")]
    public required string FamilyName { get; set; }

    /// <summary>
    /// Никнейм (псевдоним).
    /// </summary>
    [JsonPropertyName("nickname")]
    public required string Nickname { get; set; }

    /// <summary>
    /// Аватар пользователя.
    /// </summary>
    [JsonPropertyName("picture")]
    public required string Picture { get; set; }

    /// <summary>
    /// Пол.
    /// </summary>
    [JsonPropertyName("gender")]
    public required string Gender { get; set; }

    /// <summary>
    /// День рождения.
    /// </summary>
    [JsonPropertyName("birthdate")]
    public required DateTime? Birthdate { get; set; }

    /// <summary>
    /// Язык и регион.
    /// </summary>
    [JsonPropertyName("locale")]
    public required string Locale { get; set; }

    /// <summary>
    /// Электронная почта.
    /// </summary>
    [JsonPropertyName("email")]
    public required string Email { get; set; }
}