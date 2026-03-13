#nullable disable
using CRUD.Utility.Metrics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Diagnostics.Metrics;

namespace CRUD.Tests.IntegrationTests;

public class OrderIssuerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IOrderIssuer _orderIssuer;
    private readonly ApplicationDbContext _db;
    private readonly IMeterFactory _meterFactory;

    public OrderIssuerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _orderIssuer = scopedServices.GetRequiredService<IOrderIssuer>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _meterFactory = scopedServices.GetRequiredService<IMeterFactory>();
    }

    [Fact]
    public async Task IssueAsync_ShouldIssue()
    {
        // Arrange
        var collector = new MetricCollector<int>(_meterFactory, ApiMeters.MeterName, ApiMeters.ProductIssueMeterName);

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, user.Id, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Succeeded);

        var orderIdGuid = order.Id;

        // Act
        var result = await _orderIssuer.IssueAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Статус заказа стал Done
        var orderFromDbBefore = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderIdGuid);
        Assert.Equal(OrderStatuses.Done, orderFromDbBefore.Status);

        // Метрика добавилась
        await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Collection(collector.GetMeasurementSnapshot(),
            measurement =>
            {
                Assert.Equal(Products.Premium, measurement.Tags["product"]);
                Assert.Equal(1, measurement.Value);
            });
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