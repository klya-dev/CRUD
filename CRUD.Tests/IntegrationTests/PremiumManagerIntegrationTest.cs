#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CRUD.Tests.IntegrationTests;

public class PremiumManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

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

    private IPremiumManager GenerateNewPremiumManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IPremiumManager>();
    }

    [Fact] // Корректные данные
    public async Task BuyPremiumAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium);

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid);

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
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task BuyPremiumAsync_ReturnsErrorMessage_UserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.BuyPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyHasPremium, result.ErrorMessage);
    }


    [Fact] // Корректные данные
    public async Task SetPremiumAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);

        var userIdGuid = user.Id;
        var userFromDbBeforeBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        var result = await _premiumManager.SetPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        var userFromDbAfterBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
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
        var result = await _premiumManager.SetPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SetPremiumAsync_ReturnsErrorMessage_UserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: true);

        var userIdGuid = user.Id;

        // Act
        var result = await _premiumManager.SetPremiumAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserAlreadyHasPremium, result.ErrorMessage);
    }


    // Конфликты параллельности


    [Fact] // Корректные данные
    public async Task BuyPremiumAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflictOrUserAlreadyHasPremium()
    {
        // Arrange
        // Добавляем продукт в базу
        var product = await DI.CreateProductAsync(_db, name: Products.Premium);

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, isPremium: false);

        var userIdGuid = user.Id;
        var premiumManager = GenerateNewPremiumManager();
        var premiumManager2 = GenerateNewPremiumManager();
        var userFromDbBeforeBuy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        var task = premiumManager.BuyPremiumAsync(userIdGuid);
        var task2 = premiumManager2.BuyPremiumAsync(userIdGuid);

        // Может выбросится исключение с конфликтом параллельности, в документации это написано
        try
        {
            var results = await Task.WhenAll(task, task2);

            // Assert
            foreach (var result in results)
            {
                Assert.NotNull(result);

                // Либо ничего, либо уже есть премиум
                var errorMessage = result.ErrorMessage;
                string[] allowedErrors =
                [
                    null,
                    ErrorMessages.UserAlreadyHasPremium
                ];

                Assert.Contains(errorMessage, allowedErrors);
            }
        }
        catch (DbUpdateException ex)
        {
            // Если не конфликт параллельности, не обрабатываем
            if (!DbExceptionHelper.IsConcurrencyConflict(ex))
                throw;
        }
    }
}