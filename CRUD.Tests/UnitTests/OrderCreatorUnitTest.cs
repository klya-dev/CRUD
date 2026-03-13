using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRUD.Tests.UnitTests;

public class OrderCreatorUnitTest
{
    private readonly OrderCreator _orderCreator;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IValidator<Order>> _mockOrderValidator;

    public OrderCreatorUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockOrderValidator = new();

        _orderCreator = new OrderCreator(db, _mockOrderValidator.Object);
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

        // Валидация проходит
        _mockOrderValidator.Setup(x => x.ValidateAsync(It.IsAny<Order>(), default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Количество заказов ДО
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
    public async Task AddOrderToDbAsync_ShouldThrowsInvalidOperationException_WhenModelIsNotNull()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
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

        // Результат модель невалидна
        _mockOrderValidator.Setup(x => x.ValidateAsync(It.IsAny<Order>(), default)).ReturnsAsync(new ValidationResult() { Errors = [new ValidationFailure()] });

        // Act
        Func<Task> a = async () =>
        {
            await _orderCreator.AddOrderToDbAsync(paymentResponse, userIdGuid, productName);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(Order)), ex.Message);
    }


    [Fact]
    public void GetPaymentResponseFromApi_ShouldReturnsNotNull()
    {
        // Arrange
        var jsonString = "{\r\n  \"id\" : \"30339774-000f-5000-b000-1f76d66116e3\",\r\n  \"status\" : \"pending\",\r\n  \"amount\" : {\r\n    \"value\" : \"750.00\",\r\n    \"currency\" : \"RUB\"\r\n  },\r\n  \"description\" : \"Заказ №4\",\r\n  \"recipient\" : {\r\n    \"account_id\" : \"1139801\",\r\n    \"gateway_id\" : \"2508766\"\r\n  },\r\n  \"created_at\" : \"2025-08-17T07:24:36.571Z\",\r\n  \"confirmation\" : {\r\n    \"type\" : \"redirect\",\r\n    \"confirmation_url\" : \"https://yoomoney.ru/checkout/payments/v2/contract?orderId=30339774-000f-5000-b000-1f76d66116e3\"\r\n  },\r\n  \"test\" : true,\r\n  \"paid\" : false,\r\n  \"refundable\" : false,\r\n  \"metadata\" : { }\r\n}";
        var jsonDocument = JsonDocument.Parse(jsonString);

        // Act
        var result = _orderCreator.GetPaymentResponseFromApi(jsonDocument);

        // Assert
        Assert.NotNull(result);
    }

    [Fact] // В Json нет обязательного параметра "amount"
    public void GetPaymentResponseFromApi_ShouldThrowsJsonException_WhenNotExistsRequiredProperty()
    {
        // Arrange
        var jsonString = "{\r\n  \"id\" : \"30339774-000f-5000-b000-1f76d66116e3\",\r\n  \"status\" : \"pending\",\r\n  \"description\" : \"Заказ №4\",\r\n  \"recipient\" : {\r\n    \"account_id\" : \"1139801\",\r\n    \"gateway_id\" : \"2508766\"\r\n  },\r\n  \"created_at\" : \"2025-08-17T07:24:36.571Z\",\r\n  \"confirmation\" : {\r\n    \"type\" : \"redirect\",\r\n    \"confirmation_url\" : \"https://yoomoney.ru/checkout/payments/v2/contract?orderId=30339774-000f-5000-b000-1f76d66116e3\"\r\n  },\r\n  \"test\" : true,\r\n  \"paid\" : false,\r\n  \"refundable\" : false,\r\n  \"metadata\" : { }\r\n}";
        var jsonDocument = JsonDocument.Parse(jsonString);

        // Act
        Action a = () =>
        {
            _orderCreator.GetPaymentResponseFromApi(jsonDocument);
        };

        // Assert
        var ex = Assert.Throws<JsonException>(a);
        Assert.Contains("amount", ex.Message);
    }

    [Fact]
    public void GetPaymentResponseFromApi_ShouldThrowsArgumentNullException_WhenJsonDocumentIsNull()
    {
        // Arrange
        JsonDocument jsonDocument = null;

        // Act
        Action a = () =>
        {
            _orderCreator.GetPaymentResponseFromApi(jsonDocument);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(jsonDocument), ex.ParamName);
    }


    // В методе GetOrderNumberAsync используются транзакции, InMemoryDatabase такое не поддерживает
}