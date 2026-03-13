#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class OrderCreatorIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IOrderCreator _orderCreator;
    private readonly ApplicationDbContext _db;

    public OrderCreatorIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _orderCreator = scopedServices.GetRequiredService<IOrderCreator>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    private IOrderCreator GenerateNewOrderCreator()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IOrderCreator>();
    }

    [Fact]
    public async Task AddOrderToDbAsync_ShouldAdd()
    {
        // Arrange
        string productName = Products.Premium;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: productName);

        // Модель оплаты
        var paymentResponse = new PaymentResponse()
        {
            Id = "1fa85f64-5717-4562-b3fc-2c963f66afa6",
            Status = PaymentStatuses.Pending,
            Paid = true,
            Amount = new Amount() { Value = "100", Currency = "RUB" },
            CreatedAt = DateTime.UtcNow,
            Description = "Description",
            Confirmation = new Confirmation() { Type = "", ConfirmationUrl = "" },
            Refundable = true,
            Recipient = new Recipient() { AccountId = "", GatewayId = "" },
            Test = true
        };

        var countOrdersWithThisProductBefore = (await _db.Products.AsNoTracking().Include(x => x.Orders).FirstOrDefaultAsync(x => x.Name == productName)).Orders.Count;

        // Act
        await _orderCreator.AddOrderToDbAsync(paymentResponse, userIdGuid, productName);

        // Assert
        // Стало на один заказ больше
        var countOrdersWithThisProductAfter = (await _db.Products.AsNoTracking().Include(x => x.Orders).FirstOrDefaultAsync(x => x.Name == productName)).Orders.Count;
        Assert.Equal(countOrdersWithThisProductBefore + 1, countOrdersWithThisProductAfter);
    }

    [Fact]
    public async Task AddOrderToDbAsync_ShouldNotAdd_WhenEmptyGuid()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);
        string productName = Products.Premium;

        var paymentResponse = new PaymentResponse()
        {
            Id = "1fa85f64-5717-4562-b3fc-2c963f66afa6",
            Status = PaymentStatuses.Pending,
            Paid = true,
            Amount = new Amount() { Value = "100", Currency = "RUB" },
            CreatedAt = DateTime.UtcNow,
            Description = "Description",
            Confirmation = new Confirmation() { Type = "", ConfirmationUrl = "" },
            Refundable = true,
            Recipient = new Recipient() { AccountId = "", GatewayId = "" },
            Test = true
        };

        // Act
        Func<Task> a = async () =>
        {
            await _orderCreator.AddOrderToDbAsync(paymentResponse, userIdGuid, productName);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }


    [Fact]
    public async Task GetOrderNumberAsync_ShouldReturnsInt_WhenCorrectData()
    {
        // Arrange
        var orderCount = await _db.Orders.CountAsync();

        // Act
        var result = await _orderCreator.GetOrderNumberAsync();

        // Assert
        Assert.Equal(orderCount + 1, result);
    }

    [Fact]
    public async Task GetOrderNumberAsync_Conflict_ShouldReturnsInt_WhenCorrectData()
    {
        // Arrange
        var orderCount = await _db.Orders.CountAsync();
        var orderCreator = GenerateNewOrderCreator();
        var orderCreator2 = GenerateNewOrderCreator();
        var orderCreator3 = GenerateNewOrderCreator();

        // Act
        var task = orderCreator.GetOrderNumberAsync();
        var task2 = orderCreator2.GetOrderNumberAsync();
        var task3 = orderCreator3.GetOrderNumberAsync();

        var results = await Task.WhenAll(task, task2, task3);

        // Assert
        var addedOrderNumbers = new List<int>(); // Уже добавленные номера заказов
        foreach (var result in results) // Т.к при параллельном выполнении номера заказов добавляются не точно в такой же последовательности, в которой я вызвал метод (result2 = 3; result3 = 2)
        {
            if (result == orderCount + 1
                || result == orderCount + 2
                || result == orderCount + 3)
                addedOrderNumbers.Add(result);
        }

        Assert.Equal(3, addedOrderNumbers.Count);

        // Сортируем и проверяем
        var ordered = addedOrderNumbers.OrderBy(x => x).ToList();
        Assert.Equal(orderCount + 1, ordered[0]);
        Assert.Equal(orderCount + 2, ordered[1]);
        Assert.Equal(orderCount + 3, ordered[2]);
    }
}