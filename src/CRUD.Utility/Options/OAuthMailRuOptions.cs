namespace CRUD.Utility.Options;

/// <summary>
/// Опции OAuth MailRu.
/// </summary>
/// <remarks>
/// <seealso href="https://id.vk.com/about/business/go/docs/ru/vkid/latest/oauth/oauth-mail/index"/>.
/// </remarks>
public sealed class OAuthMailRuOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "OAuthMailRu";

    /// <summary>
    /// Id клиента.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Секрет клиента.
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Uri, на который перенаправляется пользователь после успешной авторизации.
    /// </summary>
    /// <remarks>
    /// Должен совпадать с указанным <c>redirect_uri</c> в настройке приложения.
    /// </remarks>
    public required string RedirectUri { get; init; }

    /// <summary>
    /// Url OpenId конфигурации.
    /// </summary>
    public required string OpenIdConfigurationUrl { get; init; }
}