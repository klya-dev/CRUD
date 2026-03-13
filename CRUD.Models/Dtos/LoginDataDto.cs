using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos;

/// <summary>
/// DTO-модель для авторизации пользователя.
/// </summary>
public class LoginDataDto
{
    /// <summary>
    /// Username пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }
}