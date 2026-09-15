namespace CRUD.Utility;

/// <summary>
/// Роли пользователей.
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Роль админа.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public const string User = "User";

    /// <summary>
    /// Возвращает коллекцию всех ролей.
    /// </summary>
    /// <returns>Коллекция всех ролей.</returns>
    public static IEnumerable<string> GetAllRoles()
    {
        return [Admin, User];
    }
}