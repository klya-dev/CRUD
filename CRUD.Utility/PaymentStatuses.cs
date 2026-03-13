namespace CRUD.Utility;

/// <summary>
/// Статусы оплаты.
/// </summary>
public static class PaymentStatuses
{
    /// <summary>
    /// Платеж создан и ожидает действий от пользователя.
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// Платеж оплачен, деньги авторизованы и ожидают списания (подтверждения).
    /// </summary>
    public const string WaitingForCapture = "waiting_for_capture";

    /// <summary>
    /// Платеж успешно завершен.
    /// </summary>
    public const string Succeeded = "succeeded";

    /// <summary>
    /// Платеж отменен.
    /// </summary>
    public const string Canceled = "canceled";

    /// <summary>
    /// Возвращает коллекцию всех статусов.
    /// </summary>
    /// <returns>Коллекция всех статусов.</returns>
    public static IEnumerable<string> GetAllStatuses()
    {
        return [Pending, WaitingForCapture, Succeeded, Canceled];
    }
}