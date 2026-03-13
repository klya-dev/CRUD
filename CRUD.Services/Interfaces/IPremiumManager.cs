namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с премиумом пользователя.
/// </summary>
public interface IPremiumManager
{
    /// <summary>
    /// Генерирует ссылку на покупку премиума для указанного пользователя.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>У пользователя уже есть премиум</term>
    /// <description><see cref="ErrorMessages.UserAlreadyHasPremium"/>.</description>
    /// </item>
    /// <item>
    /// <term>Не удалось создать платёж</term>
    /// <description><see cref="ErrorMessages.FailedToCreatePayment"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult<string>> BuyPremiumAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Выдаёт пользователю премиум.
    /// </summary>
    /// <remarks>
    /// <para>Пользователю устанавливается <see cref="User.IsPremium"/> <see langword="true"/>, а также генерируются <see cref="User.ApiKey"/> и <see cref="User.DisposableApiKey"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="orderId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
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
    /// <term>У пользователя уже есть премиум</term>
    /// <description><see cref="ErrorMessages.UserAlreadyHasPremium"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="orderId">Id оплаченного заказа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> IssuePremiumAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает пользователю премиум.
    /// </summary>
    /// <remarks>
    /// <para>Пользователю устанавливается <see cref="User.IsPremium"/> <see langword="true"/>, а также генерируются <see cref="User.ApiKey"/> и <see cref="User.DisposableApiKey"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
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
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>У пользователя уже есть премиум</term>
    /// <description><see cref="ErrorMessages.UserAlreadyHasPremium"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/>, результат сервиса.</returns>
    Task<ServiceResult> SetPremiumAsync(Guid userId, CancellationToken ct = default);
}