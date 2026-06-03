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
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> IssueAsync(Guid orderId, CancellationToken ct = default);
}