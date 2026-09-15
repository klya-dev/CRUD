using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Password;

/// <summary>
/// DTO-модель для изменения пароля пользователя.
/// </summary>
public sealed record SetPasswordDto
{
    /// <summary>
    /// Новый пароль пользователя.
    /// </summary>
    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; init; }
}