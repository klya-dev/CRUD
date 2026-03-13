#nullable disable
using CRUD.Utility.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace CRUD.Tests.UnitTests;

public class PayManagerUnitTest
{
    private readonly PayManager _payManager;
    private readonly ApplicationDbContext _db;
    private readonly Mock<IOptions<PayManagerOptions>> _mockOptions;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<PayManager>> _mockPayManagerLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IOrderCreator> _mockOrderCreator;

    public PayManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockOptions = new();
        _mockHttpClientFactory = new();
        _mockHttpMessageHandler = new();
        _mockPayManagerLogger = new();
        _mockHttpContextAccessor = new();
        _mockOrderCreator = new();

        _mockOptions.Setup(x => x.Value).Returns(new PayManagerOptions() { ServiceURL = "https://localhost", ShopId = "", ApiKey = "", SafeListIp = ""});

        // Мокаем создание клиента через фабрику
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://localhost");
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _payManager = new PayManager(_mockOptions.Object, _mockHttpClientFactory.Object, _mockPayManagerLogger.Object, _mockHttpContextAccessor.Object, db, _mockOrderCreator.Object);
    }

    [Fact]
    public async Task PayAsync_ShouldReturnPaymentResponse_WhenCorrectData()
    {
        // Arrange
        string productName = Products.Premium;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        var userIdGuid = user.Id;

        // Просто не пустой контекст
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

        // Номер заказа 1
        _mockOrderCreator.Setup(x => x.GetOrderNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Ответ ЮКассы
        var responseContent = new
        {
            id = "30339774-000f-5000-b000-1f76d66116e3",
            status = "pending",
            amount = new { value = "750.00", currency = "RUB" },
            description = "Заказ №4",
            recipient = new { account_id = "1139801", gateway_id = "2508766" },
            created_at = DateTime.UtcNow,
            confirmation = new { type = "redirect", confirmation_url = "https://yoomoney.ru/checkout/payments/v2/contract?orderId=30339774-000f-5000-b000-1f76d66116e3" },
            test = true,
            paid = true,
            refundable = false,
            metadata = new { }
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Получаем не пустой PaymentResponse
        var paymentResonse = JsonSerializer.Deserialize<PaymentResponse>(responseJson);
        _mockOrderCreator.Setup(x => x.GetPaymentResponseFromApi(It.IsAny<JsonDocument>())).Returns(paymentResonse);

        // Act
        var result = await _payManager.PayAsync(productName, userIdGuid);

        // Assert
        Assert.NotNull(result);
    }

    [Fact] // Продукт не найден
    public async Task PayAsync_ShouldReturnNull_WhenProductNotFound()
    {
        // Arrange
        string productName = "Something";
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _payManager.PayAsync(productName, userIdGuid);

        // Assert
        Assert.Null(result);
    }

    [Fact] // Пользователь не найден
    public async Task PayAsync_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        string productName = Products.Premium;

        // Добавляем продукт в базу
        await DI.CreateProductAsync(_db);

        var userIdGuid = Guid.NewGuid();
        
        // Act
        var result = await _payManager.PayAsync(productName, userIdGuid);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task CheckConnectionAsync_ReturnsFalse()
    {
        // Arrange
        var responseContent = new
        {
            description = "Something"
        };
        var responseJson = JsonSerializer.Serialize(responseContent);
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(responseJson)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(responseMessage);

        // Act
        var result = await _payManager.CheckConnectionAsync();

        // Assert
        Assert.False(result);
    }
}