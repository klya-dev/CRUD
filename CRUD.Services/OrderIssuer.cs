using CRUD.Services.Interfaces;
using CRUD.Utility.Metrics;

namespace CRUD.Services;

/// <inheritdoc cref="IOrderIssuer"/>
public class OrderIssuer : IOrderIssuer
{
    private readonly ApplicationDbContext _db;
    private readonly IPremiumManager _premiumManager;
    private readonly IValidator<Order> _orderValidator;
    private readonly ApiMeters _metrics;

    public OrderIssuer(ApplicationDbContext db, IPremiumManager premiumManager, IValidator<Order> orderValidator, ApiMeters metrics)
    {
        _db = db;
        _premiumManager = premiumManager;
        _orderValidator = orderValidator;
        _metrics = metrics;
    }

    public async Task<ServiceResult> IssueAsync(Guid orderId, CancellationToken ct = default)
    {
        // Заказ не найден
        var orderFromDb = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (orderFromDb == null)
            return ServiceResult.Fail(ErrorMessages.OrderNotFound);

        // Заказ уже выдан или отменён
        if (orderFromDb.Status != OrderStatuses.Accept)
            return ServiceResult.Fail(ErrorMessages.OrderAlreadyIssuedOrCanceled);

        // Оплата не завершена
        if (orderFromDb.PaymentStatus != PaymentStatuses.Succeeded)
            return ServiceResult.Fail(ErrorMessages.PaymentNotCompleted);

        // Своя выдача для каждого продукта
        if (orderFromDb.ProductName == Products.Premium && orderFromDb.UserId != null)
        {
            var result = await _premiumManager.IssuePremiumAsync(orderId, ct);

            // Есть ошибка
            if (result.ErrorMessage != null)
                return ServiceResult.Fail(result.ErrorMessage);

            await SetOrderIsDoneAsync(orderFromDb, CancellationToken.None); // Если уже выдали заказ выше, то и статус заказа нужно обязательно обновить

            _metrics.IssueProduct(Products.Premium);
            return ServiceResult.Success();
        }

        return ServiceResult.Fail(ErrorMessages.OrderCannotBeIssued);
    }

    /// <summary>
    /// Устанавливает статус заказа на <see cref="OrderStatuses.Done"/>.
    /// </summary>
    /// <param name="order">Заказ.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если после изменений данных сущности <see cref="Order"/>, сущность окажется невалидна.</exception>
    private async Task SetOrderIsDoneAsync(Order order, CancellationToken ct = default)
    {
        order.Status = OrderStatuses.Done;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _orderValidator.ValidateAsync(order, ct);
        if (!validationResultUser.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Order), validationResultUser.Errors));

        _db.Orders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}