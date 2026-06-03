namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с обновлениями заказов.
/// </summary>
public interface IOrderUpdater
{
    /// <summary>
    /// Обновляет данные заказа и выдаёт его.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="paymentWebHook"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Заказ не найден</term>
    /// <description><see cref="ErrorMessages.OrderNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="paymentWebHook">Вебхук оплаты.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="paymentWebHook"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> UpdateOrderInfoAsync(PaymentWebHook paymentWebHook, CancellationToken ct = default);
}