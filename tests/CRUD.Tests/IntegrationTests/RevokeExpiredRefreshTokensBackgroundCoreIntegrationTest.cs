namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class RevokeExpiredRefreshTokensBackgroundCoreIntegrationTest
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
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем Refresh-токены в базу
        var authRefreshToken = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, expires: DateTime.MinValue, ct: TestContext.Current.CancellationToken); // Истёкший
        var authRefreshToken2 = await DI.CreateAuthRefreshTokenAsync(_db, user.Id, token: "123", ct: TestContext.Current.CancellationToken);
        var authRefreshToken3 = await DI.CreateAuthRefreshTokenAsync(_db, user2.Id, token: "12345", ct: TestContext.Current.CancellationToken);
        var authRefreshToken4 = await DI.CreateAuthRefreshTokenAsync(_db, user2.Id, token: "1234567", expires: DateTime.MinValue, ct: TestContext.Current.CancellationToken); // Истёкший

        // Act
        await _revokeExpiredRefreshTokensBackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        // Все истёкшие Refresh-токены удалились
        var countExpiredRefreshTokensFromDb = await _db.AuthRefreshTokens.Where(x => x.Expires < DateTime.UtcNow).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, countExpiredRefreshTokensFromDb);
    }
}