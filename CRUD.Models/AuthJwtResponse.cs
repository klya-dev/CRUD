using System.Text.Json.Serialization;

namespace CRUD.Models;

/// <summary>
/// Ответ клиенту на запрос получения токена авторизации и аутентификации.
/// </summary>
public class AuthJwtResponse
{
    /// <summary>
    /// JWT-токен аутентификации и авторизации.
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>
    /// Срок истечения <see cref="AccessToken"/>.
    /// </summary>
    [JsonPropertyName("expires")]
    public required DateTime Expires { get; init; }

    /// <summary>
    /// JWT-токен обновления.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Username получателя данного токена.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }
}