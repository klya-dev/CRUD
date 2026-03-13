namespace CRUD.Tests.UnitTests;

public class DeleteExpiredRequestsBackgroundCoreUnitTest
{
    private readonly DeleteExpiredRequestsBackgroundCore _deleteExpiredRequestsBackgroundCore;
    private readonly ApplicationDbContext _db;

    public DeleteExpiredRequestsBackgroundCoreUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _deleteExpiredRequestsBackgroundCore = new(_db);
    }

    [Fact]
    public async Task DoWorkAsync_AllExpiredRequestsIsDeleted()
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123");

        // Добавляем запросы в базу
        var request = await DI.CreateChangePasswordRequestAsync(_db, user.Id, expires: DateTime.MinValue); // Истёкший
        var request2 = await DI.CreateConfirmEmailRequestAsync(_db, user.Id, token: "123");
        var request3 = await DI.CreateChangePasswordRequestAsync(_db, user2.Id, token: "12345");
        var request4 = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user2.Id, code: "1234567", expires: DateTime.MinValue); // Истёкший

        // Act
        await _deleteExpiredRequestsBackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        // Все истёкшие запросы удалились
        var countExpiredRefreshTokensFromDb = await _db.Requests.Where(x => x.Expires < DateTime.UtcNow).CountAsync();
        Assert.Equal(0, countExpiredRefreshTokensFromDb);
    }
}