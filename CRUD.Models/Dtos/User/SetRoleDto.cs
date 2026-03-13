namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для изменения роли пользователя.
/// </summary>
public class SetRoleDto
{
    /// <summary>
    /// Устанавливаемая роль пользователя.
    /// </summary>
    public required string Role { get; set; }
}