namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с оплатой.
/// </summary>
public interface IPayManager
{
    /// <summary>
    /// Возвращает объект оплаты.
    /// </summary>
    /// <remarks>
    /// <para>Если не удалось — <see langword="null"/>.</para>
    /// <para>Для валидации <see cref="Order"/> используется <see cref="IValidator{Order}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
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
    /// Внутренние проверки:
    /// <list type="bullet">
    /// <item>
    /// <term>Продукт не найден</term>
    /// <description><see cref="ErrorMessages.ProductNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="productName">Имя продукта из <see cref="Products"/>.</param>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="PaymentResponse"/> объект оплаты.</returns>
    Task<PaymentResponse?> PayAsync(string productName, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет подключение к сервису оплаты.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если удалось подключиться.</returns>
    Task<bool> CheckConnectionAsync(CancellationToken ct = default);
}