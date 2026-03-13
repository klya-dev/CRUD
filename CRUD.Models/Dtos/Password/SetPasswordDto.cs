using System.Text.Json.Serialization;

namespace CRUD.Models.Dtos.Password;

/// <summary>
/// DTO-модель для изменения пароля пользователя.
/// </summary>
public class SetPasswordDto
{
    /// <summary>
    /// Новый пароль пользователя.
    /// </summary>
    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; set; }
}