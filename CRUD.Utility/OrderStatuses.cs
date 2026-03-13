namespace CRUD.Utility;

/// <summary>
/// Статусы заказов.
/// </summary>
public static class OrderStatuses
{
    /// <summary>
    /// С моей стороны всё выдано и сделано.
    /// </summary>
    public const string Done = "done";

    /// <summary>
    /// Заказ принят.
    /// </summary>
    public const string Accept = "accept";

    /// <summary>
    /// Заказ отменён, деньги будут возвращены.
    /// </summary>
    public const string Canceled = "canceled";

    /// <summary>
    /// Возвращает коллекцию всех статусов.
    /// </summary>
    /// <returns>Коллекция всех статусов.</returns>
    public static IEnumerable<string> GetAllStatuses()
    {
        return [Done, Accept, Canceled];
    }
}