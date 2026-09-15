using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для удаления пользователя.
/// </summary>
public sealed record DeleteUserDto
{
    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }
}