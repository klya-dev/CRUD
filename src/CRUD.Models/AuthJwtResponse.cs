using System.Text.Json.Serialization;

namespace CRUD.Models;

/// <summary>
/// Ответ клиенту на запрос получения токена авторизации и аутентификации.
/// </summary>
public sealed record AuthJwtResponse
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

// Можно использовать первичный конструктор
// Тогда создаются поля с { get; init; }, но при этом можно создать экземпляр только через конструктор
// Т.е new AuthJwtResponse { AccessToken = "", ...} не скомпилируется
// Поэтому придётся по всему проекту переделывать под конструктор (а у меня очень много DTO), что не очень хорошо
// Ну и в целом, гибче явно указывать, как я сделал выше
// Если, что record - это тот же класс, но с плюшками, например, сравнение по значению, переопределённый .ToString(). Для DTO'шек must have
// Можно через https://sharplab.io взглянуть под капот, я так и сделал

///// <summary>
///// Ответ клиенту на запрос получения токена авторизации и аутентификации.
///// </summary>
///// <param name="AccessToken">JWT-токен аутентификации и авторизации.</param>
///// <param name="Expires">Срок истечения <see cref="AccessToken"/>.</param>
///// <param name="RefreshToken">JWT-токен обновления.</param>
///// <param name="Username">Username получателя данного токена.</param>
//public record AuthJwtResponse(
//    [property: JsonPropertyName("access_token")] string AccessToken,
//    [property: JsonPropertyName("expires")] DateTime Expires,
//    [property: JsonPropertyName("refresh_token")] string RefreshToken,
//    [property: JsonPropertyName("username")] string Username
//);