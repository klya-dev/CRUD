using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// OpenId конфигурация.
/// </summary>
/// <remarks>
/// <seealso href="https://account.mail.ru/.well-known/openid-configuration"/>.
/// </remarks>
public class OpenIdConfiguration
{
    /// <summary>
    /// Издатель.
    /// </summary>
    [JsonPropertyName("issuer")]
    public required string Issuer { get; set; }

    /// <summary>
    /// Конечная точка авторизации.
    /// </summary>
    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Конечная точка получения/обновления токена.
    /// </summary>
    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; set; }

    /// <summary>
    /// Конечная точка информации о пользователе.
    /// </summary>
    [JsonPropertyName("userinfo_endpoint")]
    public required string UserInfoEndpoint { get; set; }

    /// <summary>
    /// Конечная точка отзыва токенов.
    /// </summary>
    [JsonPropertyName("revocation_endpoint")]
    public required string RevocationEndpoint { get; set; }

    /// <summary>
    /// Конечная точка инспекции токена.
    /// </summary>
    [JsonPropertyName("introspection_endpoint")]
    public required string IntrospectionEndpoint { get; set; }

    /// <summary>
    /// Поддерживаемые типы ответов.
    /// </summary>
    [JsonPropertyName("response_types_supported")]
    public required List<string> ResponseTypesSupported { get; set; }

    /// <summary>
    /// Поддерживаемые способы получения токена.
    /// </summary>
    [JsonPropertyName("grant_types_supported")]
    public required List<string> GrantTypesSupported { get; set; }

    /// <summary>
    /// Поддерживаемые методы аутентификации клиента для получения токена.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public required List<string> TokenEndpointAuthMethodsSupported { get; set; }
}