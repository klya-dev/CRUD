namespace CRUD.Models.Dtos.User;

/// <summary>
/// DTO-модель для изменения роли пользователя.
/// </summary>
public sealed record SetRoleDto
{
    /// <summary>
    /// Устанавливаемая роль пользователя.
    /// </summary>
    public required string Role { get; init; }
}