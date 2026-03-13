using CRUD.Models.Dtos;

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
    /// <para>Для валидации <see cref="Order"/> используется <see cref="IValidator{Order}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="paymentWebHook"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
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
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> UpdateOrderInfoAsync(PaymentWebHook paymentWebHook, CancellationToken ct = default);
}