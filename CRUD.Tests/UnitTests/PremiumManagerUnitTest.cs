#nullable disable
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.UnitTests;

public class PremiumManagerUnitTest
{
    private readonly PremiumManager _premiumManager;
    private readonly Mock<IUserApiKeyManager> _mockUserApiKeyManager;
    private readonly Mock<IValidator<User>> _mockUserValidator;
    private readonly Mock<IPayManager> _mockPayManager;
    private readonly Mock<IPremiumInformator> _mockPremiumInformator;
    private readonly ApplicationDbContext _db;

    public PremiumManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockUserApiKeyManager = new();
        _mockUserValidator = new();
        _mockPayManager = new();
        _mockPremiumInformator = new();

        _premiumManager = new PremiumManager(db, _mockUserApiKeyManager.Object, _mockUserValidator.Object, _mockPayManager.Object, _mockPremiumInformator.Object);
    }


    [Fact]
    public async Task BuyPremiumAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _premiumManager.BuyPremiumAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact] // Ссылка на оплату вернулась
    public async Task BuyPremiumAsync_ShouldReturnsString_WhenPaySuccess()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        var paymentResponse = new PaymentResponse()
        {
            Id = "1fa85f64-5717-4562-b3fc-2c963f66afa6",
            Status = PaymentStatuses.Pending,
            Paid = true,
            Amount = new Amount() { Value = "100", Currency = "RUB" },
            CreatedAt = DateTime.UtcNow,
            Description = "Description",
            Confirmation = new Confirmation() { Type = "", ConfirmationUrl = "something" },
            Refundable = true,
            Recipient = new Recipient() { AccountId = "", GatewayId = "" },
            Test = true
        };

        _mockPayManager.Setup(x => x.PayAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(paymentResponse);

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(paymentResponse.Confirmation.ConfirmationUrl, result.Value);
    }

    [Fact] // Ссылка на оплату не вернулась
    public async Task BuyPremiumAsync_ShouldReturnsFailedToCreatePayment_WhenPayNotSuccess()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Не удалось создать платёж
        PaymentResponse paymentResponse = null;
        _mockPayManager.Setup(x => x.PayAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(paymentResponse);

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FailedToCreatePayment, result.ErrorMessage);
    }


    [Fact]
    public async Task IssuePremiumAsync_ShouldNoProblem_WhenCorrectData()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);
        var userId = user.Id;

        // Добавляем пользователя в базу
        var product = await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, userId);

        var orderIdGuid = order.Id;
        var userFromDbBeforeBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);

        // Генерация валидных ключей ключей
        _mockUserApiKeyManager.Setup(x => x.GenerateUserApiKey()).Returns(TestConstants.UserApiKey);
        _mockUserApiKeyManager.Setup(x => x.GenerateDisposableUserApiKey()).Returns(TestConstants.UserDisposableApiKey);

        // Валидация проходит
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), default)).ReturnsAsync(new ValidationResult());

        // Успешное проинформирование пользователя
        _mockPremiumInformator.Setup(x => x.InformateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _premiumManager.IssuePremiumAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Все нужные поля и вправду обновились
        var userFromDbAfterBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        Assert.False(userFromDbBeforeBuy.IsPremium); // IsPremium был false
        Assert.True(userFromDbAfterBuy.IsPremium); // IsPremium стал true

        Assert.Null(userFromDbBeforeBuy.ApiKey); // ApiKey был null
        Assert.NotNull(userFromDbAfterBuy.ApiKey); // ApiKey стал not null

        Assert.Null(userFromDbBeforeBuy.DisposableApiKey); // DisposableApiKey был null
        Assert.NotNull(userFromDbAfterBuy.DisposableApiKey); // DisposableApiKey стал not null
    }

    [Fact]
    public async Task IssuePremiumAsync_ShouldReturnsUserNotFound_WhenUserNotFound()
    {
        // Arrange
        // Добавляем пользователя в базу
        var product = await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, null);

        var orderIdGuid = order.Id;

        // Act
        var result = await _premiumManager.IssuePremiumAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task IssuePremiumAsync_ShouldReturnsOrderAlreadyIssuedOrCanceled_WhenOrderCanceled()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);
        var userId = user.Id;

        // Добавляем пользователя в базу
        var product = await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, userId, status: OrderStatuses.Canceled);

        var orderIdGuid = order.Id;

        // Act
        var result = await _premiumManager.IssuePremiumAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.OrderAlreadyIssuedOrCanceled, result.ErrorMessage);
    }

    [Fact]
    public async Task IssuePremiumAsync_ShouldReturnsPaymentNotCompleted_WhenPaymentNotCompleted()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);
        var userId = user.Id;

        // Добавляем пользователя в базу
        var product = await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, userId, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Pending);

        var orderIdGuid = order.Id;

        // Act
        var result = await _premiumManager.IssuePremiumAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.PaymentNotCompleted, result.ErrorMessage);
    }

    [Fact]
    public async Task IssuePremiumAsync_ShouldReturnsUserAlreadyHasPremium_WhenUserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true);
        var userId = user.Id;

        // Добавляем пользователя в базу
        var product = await DI.CreateProductAsync(_db);

        // Добавляем заказ в базу
        var order = await DI.CreateOrderAsync(_db, userId, status: OrderStatuses.Accept, paymentStatus: PaymentStatuses.Succeeded);

        var orderIdGuid = order.Id;

        // Act
        var result = await _premiumManager.IssuePremiumAsync(orderIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyHasPremium, result.ErrorMessage);
    }


    [Fact]
    public async Task SetPremiumAsync_NotValidGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _premiumManager.SetPremiumAsync(userIdGuid);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }
}