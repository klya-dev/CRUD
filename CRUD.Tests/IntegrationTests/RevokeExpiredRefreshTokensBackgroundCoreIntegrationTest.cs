namespace CRUD.Tests.IntegrationTests;

public class RevokeExpiredRefreshTokensBackgroundCoreIntegrationTest
{
    private readonly RevokeExpiredRefreshTokensBackgroundCore _revokeExpiredRefreshTokensBackgroundCore;
    private readonly ApplicationDbContext _db;

    public RevokeExpiredRefreshTokensBackgroundCoreIntegrationTest()
    {
        var db = DbContextGenerator.GenerateDbContextTest(); // Тестовая база в памяти не поддерживает ExecuteDeleteAsync
        _db = db;

        _revokeExpiredRefreshTokensBackgroundCore = new(_db);
    }

    [Fact]
    public async Task DoWorkAsync_AllExpiredRefreshTokensIsDeleted()
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123");

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, expires: DateTime.MinValue); // Истёкший
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, token: "123");
        var authRefreshToken3 = await DI.CreateAuthRefreshTokenAsync(_db, user2.Id, token: "12345");
        var authRefreshToken4 = await DI.CreateAuthRefreshTokenAsync(_db, user2.Id, token: "1234567", expires: DateTime.MinValue); // Истёкший

        // Act
        await _revokeExpiredRefreshTokensBackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        // Все истёкшие Refresh-токены удалились
        var countExpiredRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.Expires < DateTime.UtcNow).CountAsync();
        Assert.Equal(0, countExpiredRefreshTokensFromDb);
    }
}