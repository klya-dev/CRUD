namespace CRUD.Utility;

/// <summary>
/// Продукты.
/// </summary>
public static class Products
{
    /// <summary>
    /// Премиум для пользователя.
    /// </summary>
    public const string Premium = "premium";

    /// <summary>
    /// Возвращает коллекцию всех продуктов.
    /// </summary>
    /// <returns>Коллекция всех продуктов.</returns>
    public static IEnumerable<string> GetAllProductNames()
    {
        return [Premium];
    }
}