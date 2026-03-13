using CRUD.Services;
using CRUD.Services.Interfaces;
using CRUD.Tests.Helpers;
using FluentValidation;

namespace CRUD.Tests.UnitTests;

public class OrderUpdaterUnitTest
{
    private readonly OrderUpdater _orderUpdater;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IValidator<Order>> _mockOrderValidator;
    private readonly Mock<IOrderIssuer> _mockOrderIssuer;

    public OrderUpdaterUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockOrderValidator = new();
        _mockOrderIssuer = new();

        _orderUpdater = new OrderUpdater(_db, _mockOrderValidator.Object, _mockOrderIssuer.Object);
    }

    [Fact]
    public async Task UpdateOrderInfoAsync_ShouldUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending);

        var orderIdGuid = order.Id;

        var paymentWebHook = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Валидация проходит
        _mockOrderValidator.Setup(x => x.ValidateAsync(It.IsAny<Order>(), default)).ReturnsAsync(new ValidationResult());

        // Успешная выдача заказа
        _mockOrderIssuer.Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Act
        var result = await _orderUpdater.UpdateOrderInfoAsync(paymentWebHook);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateOrderInfoAsync_ShouldReturnsOrderNotFound()
    {
        // Arrange
        var orderIdGuid = Guid.NewGuid();
        var paymentWebHook = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Act
        var result = await _orderUpdater.UpdateOrderInfoAsync(paymentWebHook);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderNotFound, result.ErrorMessage);
    }
}