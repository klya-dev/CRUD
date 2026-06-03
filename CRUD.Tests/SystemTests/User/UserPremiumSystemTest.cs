using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public sealed class UserPremiumSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;

    public UserPremiumSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Fact] // Корректные данные
    public async Task Post_ReturnsString()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PREMIUM_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userIdGuid.ToString());

        var userFromDbBeforeBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var content = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        AssertExtensions.IsNotNullOrNotWhiteSpace(content);

        // Заказ и вправду добавился
        var orderFromDb = await _db.Orders.AsNoTracking().Where(x => x.UserId == userIdGuid).FirstOrDefaultAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(orderFromDb);
    }

    [Fact] // Корректные данные
    public async Task Post_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PREMIUM_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact] // Корректные данные
    public async Task Post_ReturnsUserAlreadyHasPremium()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, ct: TestContext.Current.CancellationToken);

        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PREMIUM_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_ALREADY_HAS_PREMIUM, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact] // Корректные данные
    public async Task Post_Mock_ReturnsFailedToCreatePayment()
    {
        // Arrange
        var mockPayManager = new Mock<IPayManager>();

        // Неудалось создать платёж
        PaymentResponse paymentResponse = null;
        mockPayManager.Setup(x => x.PayAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(paymentResponse);

        var client = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockPayManager.Object);
            });
        }).CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, ct: TestContext.Current.CancellationToken);

        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_PREMIUM_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.FAILED_TO_CREATE_PAYMENT, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}