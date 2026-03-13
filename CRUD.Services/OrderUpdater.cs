using CRUD.Models.Dtos;
using CRUD.Services.Interfaces;
using System.Text.Json;

namespace CRUD.Services;

/// <inheritdoc cref="IOrderUpdater"/>
public class OrderUpdater : IOrderUpdater
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Order> _orderValidator;
    private readonly IOrderIssuer _orderIssuer;

    public OrderUpdater(ApplicationDbContext db, IValidator<Order> orderValidator, IOrderIssuer orderIssuer)
    {
        _db = db;
        _orderValidator = orderValidator;
        _orderIssuer = orderIssuer;
    }

    public async Task<ServiceResult> UpdateOrderInfoAsync(PaymentWebHook paymentWebHook, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paymentWebHook);

        // Читаем содержимое ответа
        using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(paymentWebHook.Object));

        var orderId = jsonDocument.RootElement.GetProperty("id").GetGuid();
        var status = jsonDocument.RootElement.GetProperty("status").GetString() ?? string.Empty; // Валидатор перехватит невалидный статус в любом случае
        var paid = jsonDocument.RootElement.GetProperty("paid").GetBoolean();

        // Заказ не найден
        var orderFromDb = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (orderFromDb == null)
            return ServiceResult.Fail(ErrorMessages.OrderNotFound);

        // Обновляем данные заказа
        orderFromDb.PaymentStatus = status;
        orderFromDb.Paid = paid;

        // Проверка валидности данных перед записью в базу
        var validationResult = await _orderValidator.ValidateAsync(orderFromDb, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Order), validationResult.Errors));

        _db.Orders.Update(orderFromDb);
        await _db.SaveChangesAsync(ct);

        // Выдача заказа
        var result = await _orderIssuer.IssueAsync(orderId, CancellationToken.None); // Обязательно выдаём заказ, т.к уже приняли новый статус оплаты

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage);

        return ServiceResult.Success();
    }
}