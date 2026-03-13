using Amazon.Runtime.Telemetry.Metrics;
using CRUD.Services;
using CRUD.Services.Interfaces;
using CRUD.Tests.Helpers;
using CRUD.Utility.Metrics;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace CRUD.Tests.UnitTests;

public class OrderIssuerUnitTest
{
    private readonly OrderIssuer _orderIssuer;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IPremiumManager> _mockPremiumManager;
    private readonly Mock<IValidator<Order>> _mockOrderValidator;
    private readonly Mock<IMeterFactory> _mockMetrics;

    public OrderIssuerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockPremiumManager = new();
        _mockOrderValidator = new();
        _mockMetrics = new();

        // Мок IMeterFactory
        _mockMetrics.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(new System.Diagnostics.Metrics.Meter("name"));

        _orderIssuer = new OrderIssuer(db, _mockPremiumManager.Object, _mockOrderValidator.Object, new ApiMeters(_mockMetrics.Object));
    }

    [Fact]
    public async Task IssueAsync_ShouldIssue()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Succeeded);

        var orderIdGuid = order.Id;

        // Успешная выдача премиума
        _mockPremiumManager.Setup(x => x.IssuePremiumAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Валидация проходит
        _mockOrderValidator.Setup(x => x.ValidateAsync(It.IsAny<Order>(), default)).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Статус заказа стал Done
        var orderFromDbBefore = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderIdGuid);
        Assert.Equal(OrderStatuses.Done, orderFromDbBefore.Status);
    }

    [Fact]
    public async Task IssueAsync_ShouldOrderNotFound_WhenOrderNotExists()
    {
        // Arrange
        var orderIdGuid = Guid.NewGuid();

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task IssueAsync_WhenOrderAlreadyIssued_ShouldOrderAlreadyIssuedOrCanceled()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Done, paymentStatus: PaymentStatuses.Succeeded);

        var orderIdGuid = order.Id;

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderAlreadyIssuedOrCanceled, result.ErrorMessage);
    }

    [Fact]
    public async Task IssueAsync_WhenOrderCanceled_ShouldOrderAlreadyIssuedOrCanceled()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Canceled, paymentStatus: PaymentStatuses.Succeeded);

        var orderIdGuid = order.Id;

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderAlreadyIssuedOrCanceled, result.ErrorMessage);
    }

    [Fact]
    public async Task IssueAsync_ShouldPaymentNotCompleted()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending);

        var orderIdGuid = order.Id;

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PaymentNotCompleted, result.ErrorMessage);
    }
}