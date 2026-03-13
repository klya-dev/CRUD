using CRUD.Services.Interfaces;
using System.Text.Json;

namespace CRUD.Services;

/// <inheritdoc cref="IOrderCreator"/>
public class OrderCreator : IOrderCreator
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<Order> _orderValidator;

    public OrderCreator(ApplicationDbContext db, IValidator<Order> orderValidator)
    {
        _db = db;
        _orderValidator = orderValidator;
    }

    public async Task AddOrderToDbAsync(PaymentResponse paymentResponse, Guid userId, string productName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paymentResponse);
        ArgumentNullException.ThrowIfNull(productName);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Создаём заказ
        var order = new Order()
        {
            Id = Guid.Parse(paymentResponse.Id),
            UserId = userId,
            Status = OrderStatuses.Accept,
            PaymentStatus = paymentResponse.Status,
            ProductName = productName,
            Paid = paymentResponse.Paid,
            Amount = decimal.Parse(paymentResponse.Amount.Value.Replace(".", ",")),
            Currency = paymentResponse.Amount.Currency,
            CreatedAt = DateTime.UtcNow,
            Description = paymentResponse.Description,
            Refundable = paymentResponse.Refundable,
        };

        // Проверка валидности данных перед записью в базу
        var validationResult = await _orderValidator.ValidateAsync(order, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(Order), validationResult.Errors));

        // Добавляем заказ в базу
        await _db.Orders.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
    }

    public PaymentResponse? GetPaymentResponseFromApi(JsonDocument jsonDocument)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);

        return jsonDocument.Deserialize<PaymentResponse>();
    }

    public async Task<int> GetOrderNumberAsync(CancellationToken ct = default)
    {
        // await _db.Orders.CountAsync() + 1; - так просто не выйдет, т.к, если параллельно, то жопа

        var executionStrategy = _db.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async (ct) =>
        {
            // Используем транзакцию с уровнем изоляции Serializable для атомарности
            await using (var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct))
            {
                // Автоинкремент на стороне базы
                var orderNumberSequence = new OrderNumberSequence();

                await _db.OrderNumberSequences.AddAsync(orderNumberSequence, ct);
                await _db.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return orderNumberSequence.Number;
            }
        }, ct);
    }
}