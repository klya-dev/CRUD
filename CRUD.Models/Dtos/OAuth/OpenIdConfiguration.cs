using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.OAuth;

/// <summary>
/// OpenId конфигурация.
/// </summary>
/// <remarks>
/// <seealso href="https://account.mail.ru/.well-known/openid-configuration"/>.
/// </remarks>
public sealed record OpenIdConfiguration
{
    /// <summary>
    /// Издатель.
    /// </summary>
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    /// <summary>
    /// Конечная точка авторизации.
    /// </summary>
    [JsonPropertyName("authorization_endpoint")]
    public required string AuthorizationEndpoint { get; init; }

    /// <summary>
    /// Конечная точка получения/обновления токена.
    /// </summary>
    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    /// <summary>
    /// Конечная точка информации о пользователе.
    /// </summary>
    [JsonPropertyName("userinfo_endpoint")]
    public required string UserInfoEndpoint { get; init; }

    /// <summary>
    /// Конечная точка отзыва токенов.
    /// </summary>
    [JsonPropertyName("revocation_endpoint")]
    public required string RevocationEndpoint { get; init; }

    /// <summary>
    /// Конечная точка инспекции токена.
    /// </summary>
    [JsonPropertyName("introspection_endpoint")]
    public required string IntrospectionEndpoint { get; init; }

    /// <summary>
    /// Поддерживаемые типы ответов.
    /// </summary>
    [JsonPropertyName("response_types_supported")]
    public required List<string> ResponseTypesSupported { get; init; } // Для DTO логичнее использовать реализацию, а не интерфейс (https://softwareengineering.stackexchange.com/questions/356962/which-c-type-should-one-prefer-for-defining-lists-in-dtos)

    /// <summary>
    /// Поддерживаемые способы получения токена.
    /// </summary>
    [JsonPropertyName("grant_types_supported")]
    public required List<string> GrantTypesSupported { get; init; }

    /// <summary>
    /// Поддерживаемые методы аутентификации клиента для получения токена.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public required List<string> TokenEndpointAuthMethodsSupported { get; init; }
}