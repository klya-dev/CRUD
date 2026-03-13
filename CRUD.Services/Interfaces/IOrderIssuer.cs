namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с выдачей заказов.
/// </summary>
public interface IOrderIssuer
{
    /// <summary>
    /// Выдача заказа.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="Order"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
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
    /// <item>
    /// <term>Заказ уже выдан или отменён</term>
    /// <description><see cref="ErrorMessages.OrderAlreadyIssuedOrCanceled"/>.</description>
    /// </item>
    /// <item>
    /// <term>Оплата не завершена</term>
    /// <description><see cref="ErrorMessages.PaymentNotCompleted"/>.</description>
    /// </item>
    /// <item>
    /// <term>Заказ не может быть выдан</term>
    /// <description><see cref="ErrorMessages.OrderCannotBeIssued"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="orderId">Id заказа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если после изменений данных сущности <see cref="Order"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> IssueAsync(Guid orderId, CancellationToken ct = default);
}