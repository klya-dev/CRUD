using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Password;

/// <summary>
/// DTO-модель для изменения пароля пользователя.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// Текущий пароль пользователя.
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>
    /// Новый пароль пользователя.
    /// </summary>
    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; set; }
}