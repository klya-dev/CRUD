#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class OrderUpdaterIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IOrderUpdater _orderUpdater;
    private readonly ApplicationDbContext _db;

    public OrderUpdaterIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _orderUpdater = scopedServices.GetRequiredService<IOrderUpdater>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
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

        // Act
        var result = await _orderUpdater.UpdateOrderInfoAsync(paymentWebHook);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Данные заказа обновились и товар выдан
        var orderFromDbAfter = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderIdGuid);
        Assert.Equal(PaymentStatuses.Succeeded, orderFromDbAfter.PaymentStatus);
        Assert.Equal(OrderStatuses.Done, orderFromDbAfter.Status);
        Assert.True(orderFromDbAfter.Paid);
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