namespace CRUD.Tests.UnitTests;

[Collection(nameof(GlobalDbContainerCollection))]
public sealed class OrderUpdaterUnitTest
{
    private readonly OrderUpdater _orderUpdater;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IOrderIssuer> _mockOrderIssuer;

    public OrderUpdaterUnitTest(DbContainerFixture fixture)
    {
        var db = DbContextGenerator.GenerateDbContextTestContainer(fixture.DbOptions);
        _db = db;

        _mockOrderIssuer = new();

        _orderUpdater = new OrderUpdater(_db, _mockOrderIssuer.Object);
    }

    [Fact]
    public async Task UpdateOrderInfoAsync_ShouldUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db, ct: TestContext.Current.CancellationToken);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending, ct: TestContext.Current.CancellationToken);

        var orderIdGuid = order.Id;

        var paymentWebHook = new PaymentWebHook()
        {
            Type = "notification",
            Event = "payment." + PaymentStatuses.Succeeded,
            Object = new { id = orderIdGuid, status = PaymentStatuses.Succeeded, paid = true }
        };

        // Успешная выдача заказа
        _mockOrderIssuer.Setup(x => x.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Act
        var result = await _orderUpdater.UpdateOrderInfoAsync(paymentWebHook, ct: TestContext.Current.CancellationToken);

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
        var result = await _orderUpdater.UpdateOrderInfoAsync(paymentWebHook, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderNotFound, result.ErrorMessage);
    }
}