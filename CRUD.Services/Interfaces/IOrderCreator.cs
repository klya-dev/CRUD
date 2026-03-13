using System.Text.Json;

namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с созданиями заказов.
/// </summary>
public interface IOrderCreator
{
    /// <summary>
    /// Добавляет созданный заказ в базу по предоставленному ответу API.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="paymentResponse"/> или <paramref name="productName"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="paymentResponse">Ответ API.</param>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="productName">Имя продукта из констант <see cref="Products"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="paymentResponse"/> или <paramref name="productName"/> являются <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или невалидная сущность <see cref="Order"/> перед записью в базу.</exception>
    Task AddOrderToDbAsync(PaymentResponse paymentResponse, Guid userId, string productName, CancellationToken ct = default);

    /// <summary>
    /// Возвращает ответ из API в виде класса <see cref="PaymentResponse"/> по <see cref="JsonDocument"/>.
    /// </summary>
    /// <param name="jsonDocument">Ответ из API в виде <see cref="JsonDocument"/>.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="jsonDocument"/> является <see langword="null"/>.</exception>
    /// <returns><see cref="PaymentResponse"/>, объект оплаты.</returns>
    PaymentResponse? GetPaymentResponseFromApi(JsonDocument jsonDocument);

    /// <summary>
    /// Возвращает следующий номер заказа.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns>Следующий номер заказа.</returns>
    Task<int> GetOrderNumberAsync(CancellationToken ct = default);
}