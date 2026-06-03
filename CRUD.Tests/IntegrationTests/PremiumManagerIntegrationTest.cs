using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public sealed class PremiumManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPremiumManager _premiumManager;
    private readonly ApplicationDbContext _db;

    public PremiumManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _premiumManager = scopedServices.GetRequiredService<IPremiumManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Fact] // Корректные данные
    public async Task BuyPremiumAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium, ct: TestContext.Current.CancellationToken);

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value);
    }

    [Fact]
    public async Task BuyPremiumAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task BuyPremiumAsync_ReturnsErrorMessage_UserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyHasPremium, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task SetPremiumAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;
        var userFromDbBeforeBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Act
        var result = await _premiumManager.SetPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);
        Assert.False(userFromDbBeforeBuy.IsPremium);
        Assert.Null(userFromDbBeforeBuy.ApiKey);
        Assert.Null(userFromDbBeforeBuy.DisposableApiKey);

        Assert.True(userFromDbAfterBuy.IsPremium);
        Assert.NotNull(userFromDbAfterBuy.ApiKey);
        Assert.NotNull(userFromDbAfterBuy.DisposableApiKey);
    }

    [Fact]
    public async Task SetPremiumAsync_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _premiumManager.SetPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SetPremiumAsync_ReturnsErrorMessage_UserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true, ct: TestContext.Current.CancellationToken);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.SetPremiumAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyHasPremium, result.ErrorMessage);
    }
}