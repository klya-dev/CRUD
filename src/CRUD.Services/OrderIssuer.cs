using CRUD.Utility.Metrics;

namespace CRUD.Services;

/// <inheritdoc cref="IOrderIssuer"/>
public sealed class OrderIssuer : IOrderIssuer
{
    private readonly ApplicationDbContext _db;
    private readonly IPremiumManager _premiumManager;
    private readonly ApiMeters _metrics;

    public OrderIssuer(ApplicationDbContext db, IPremiumManager premiumManager, ApiMeters metrics)
    {
        _db = db;
        _premiumManager = premiumManager;
        _metrics = metrics;
    }

    public async Task<ServiceResult> IssueAsync(Guid orderId, CancellationToken ct = default)
    {
        // Заказ не найден
        var orderFromDb = await _db.Orders.Where(x => x.Id == orderId).Select(x => new { x.Status, x.PaymentStatus, x.ProductName, x.UserId }).FirstOrDefaultAsync(ct);
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

            await SetOrderIsDoneAsync(orderId, CancellationToken.None); // Если уже выдали заказ выше, то и статус заказа нужно обязательно обновить

            _metrics.IssueProduct(Products.Premium);
            return ServiceResult.Success();
        }

        return ServiceResult.Fail(ErrorMessages.OrderCannotBeIssued);
    }

    /// <summary>
    /// Устанавливает статус заказа на <see cref="OrderStatuses.Done"/>.
    /// </summary>
    /// <param name="orderId">Id заказа.</param>
    /// <param name="ct">Токен отмены.</param>
    private async Task SetOrderIsDoneAsync(Guid orderId, CancellationToken ct = default)
    {
        // Обновляем статус заказа
        await _db.Orders.Where(x => x.Id == orderId)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.Status, OrderStatuses.Done), ct);
    }
}