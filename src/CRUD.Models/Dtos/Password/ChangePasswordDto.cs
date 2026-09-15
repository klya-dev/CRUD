using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Password;

/// <summary>
/// DTO-модель для изменения пароля пользователя.
/// </summary>
public sealed record ChangePasswordDto
{
    /// <summary>
    /// Текущий пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; init; }

    /// <summary>
    /// Новый пароль пользователя.
    /// </summary>
    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; init; }
}