using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель для авторизации пользователя.
/// </summary>
public sealed record LoginDataDto
{
    /// <summary>
    /// Username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }
}