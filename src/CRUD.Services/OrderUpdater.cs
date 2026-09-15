using CRUD.Models.Dtos;
using CRUD.Services.Interfaces;
using System.Text.Json;

namespace CRUD.Services;

/// <inheritdoc cref="IOrderUpdater"/>
public sealed class OrderUpdater : IOrderUpdater
{
    private readonly ApplicationDbContext _db;
    private readonly IOrderIssuer _orderIssuer;

    public OrderUpdater(ApplicationDbContext db, IOrderIssuer orderIssuer)
    {
        _db = db;
        _orderIssuer = orderIssuer;
    }

    public async Task<ServiceResult> UpdateOrderInfoAsync(PaymentWebHook paymentWebHook, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paymentWebHook);

        // Читаем содержимое ответа
        using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(paymentWebHook.Object));

        var orderId = jsonDocument.RootElement.GetProperty("id").GetGuid();
        var status = jsonDocument.RootElement.GetProperty("status").GetString() ?? throw new NullReferenceException("The status must not be null.");
        var paid = jsonDocument.RootElement.GetProperty("paid").GetBoolean();

        // Обновляем данные заказа
        var updatedRows = await _db.Orders.Where(x => x.Id == orderId)
            .ExecuteUpdateAsync(x => 
                x.SetProperty(p => p.PaymentStatus, status)
                .SetProperty(p => p.Paid, paid), ct);

        // Заказ не найден
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.OrderNotFound);

        // Выдача заказа
        var result = await _orderIssuer.IssueAsync(orderId, CancellationToken.None); // Обязательно выдаём заказ, т.к уже приняли новый статус оплаты

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage);

        return ServiceResult.Success();
    }
}