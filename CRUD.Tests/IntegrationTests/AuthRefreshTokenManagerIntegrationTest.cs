using CRUD.Models.Domains;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class AuthRefreshTokenManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IAuthRefreshTokenManager _authRefreshTokenManager;
    private readonly ApplicationDbContext _db;

    public AuthRefreshTokenManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _authRefreshTokenManager = scopedServices.GetRequiredService<IAuthRefreshTokenManager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Fact] // Корректные данные
    public async Task AddRefreshTokenAndDeleteOldersAsync_ReturnsTask()
    {
        // Arrange
        var newRefreshToken = "newtoken";
        var usedRefreshToken = "usedtoken";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем текущий (в будущем использованный) Refresh-токен в базу
        var usedAuthRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, usedRefreshToken);

        // Act
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(newRefreshToken, userIdGuid, usedRefreshToken);

        // Assert
        // Новый Refresh-токен добавился в базу
        var newRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Token == newRefreshToken);
        Assert.NotNull(newRefreshTokenFromDb);

        // Использованный токен удалился из базы
        var usedRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == usedAuthRefreshToken.Id);
        Assert.Null(usedRefreshTokenFromDb);
    }

    [Fact]
    public async Task AddRefreshTokenAndDeleteOldersAsync_NotValidData_ThrowsInvalidOperationException()
    {
        // Arrange
        var authRefreshToken = new AuthRefreshToken()
        {
            Token = "",
            UserId = Guid.NewGuid(),
            Expires = DateTime.UtcNow,
        };
        var validatorsLocalizer = new Models.Validators.ValidatorsLocalizer.ValidatorsLocalizer();
        var validationResult = await new AuthRefreshTokenValidator().ValidateAsync(authRefreshToken);

        // Act
        Func<Task> a = async () =>
        {
            await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(authRefreshToken.Token, authRefreshToken.Id);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(AuthRefreshToken), validationResult.Errors), ex.Message);
    }

    [Fact] // Пользователь уже имеет четыре токена в базе
    public async Task AddRefreshTokenAndDeleteOldersAsync_WhenUserHaveFourRefreshTokens_ReturnsTask()
    {
        // Arrange
        var newRefreshToken = "newtoken";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid); // Этот токен передастся в метод и удалится
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "12356"); // Этот будет считаться самым старым (раньше всех выпущен)
        var authRefreshToken3 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "1234567");
        var authRefreshToken4 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "123456789");

        // Act
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(newRefreshToken, userIdGuid, authRefreshToken.Token);

        // Assert
        // Новый Refresh-токен добавился в базу
        var newRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Token == newRefreshToken);
        Assert.NotNull(newRefreshTokenFromDb);

        // Использованный токен удалился из базы
        var usedRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken.Id);
        Assert.Null(usedRefreshTokenFromDb);

        // Удалился самый старый Refresh-токен, чтобы в сумме их стало 3
        var olderRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken2.Id);
        Assert.Null(olderRefreshTokenFromDb);
    }

    [Fact] // Пользователь уже имеет пять токенов в базе
    public async Task AddRefreshTokenAndDeleteOldersAsync_WhenUserHaveFiveRefreshTokens_ReturnsTask()
    {
        // Arrange
        var newRefreshToken = "newtoken";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid); // Этот токен передастся в метод и удалится
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "12356"); // Этот будет считаться самым старым (раньше всех выпущен)
        var authRefreshToken3 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "1234567"); // Этот будет считаться самым старым 2
        var authRefreshToken4 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "123456789");
        var authRefreshToken5 = await DI.CreateAuthRefreshTokenAsync(_db, userIdGuid, token: "1234567891011");

        // Act
        await _authRefreshTokenManager.AddRefreshTokenAndDeleteOldersAsync(newRefreshToken, userIdGuid, authRefreshToken.Token);

        // Assert
        // Новый Refresh-токен добавился в базу
        var newRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Token == newRefreshToken);
        Assert.NotNull(newRefreshTokenFromDb);

        // Использованный токен удалился из базы
        var usedRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken.Id);
        Assert.Null(usedRefreshTokenFromDb);

        // Удалилось два самый старых Refresh-токена, чтобы в сумме их стало 3
        var olderRefreshTokenFromDb = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken2.Id);
        var olderRefreshTokenFromDb2 = await _db.AuthRefreshTokens.FirstOrDefaultAsync(x => x.Id == authRefreshToken3.Id);
        Assert.Null(olderRefreshTokenFromDb);
        Assert.Null(olderRefreshTokenFromDb2);
    }
}