namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель номера заказа.
/// </summary>
/// <remarks>
/// Чтобы избежать конфликтов параллельности, при одновременных оформлениях заказа.
/// </remarks>
public class OrderNumberSequence
{
    /// <summary>
    /// Номер заказа.
    /// </summary>
    /// <remarks>
    /// Достаточно создать экземпляр класса, т.к автоинкремент на стороне базы данных.
    /// </remarks>
    public int Number { get; set; }
}