using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class DeleteExpiredRequestsBackgroundCoreIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IDeleteExpiredRequestsBackgroundCore _deleteExpiredRequestsBackgroundCore;
    private readonly ApplicationDbContext _db;

    public DeleteExpiredRequestsBackgroundCoreIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _deleteExpiredRequestsBackgroundCore = scopedServices.GetRequiredService<IDeleteExpiredRequestsBackgroundCore>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task DoWorkAsync_AllExpiredRequestsIsDeleted()
    {
        // Arrange
        // Добавляем пользователей в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var user2 = await DI.CreateUserAsync(_db, username: "test", email: "test", phoneNumber: "123", ct: TestContext.Current.CancellationToken);

        // Добавляем запросы в базу
        var request = await DI.CreateConfirmEmailRequestAsync(_db, user.Id, token: "123", ct: TestContext.Current.CancellationToken);
        var request2 = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user2.Id, code: "1234567", expires: DateTime.MinValue, ct: TestContext.Current.CancellationToken); // Истёкший
        var request3 = await DI.CreateVerificationPhoneNumberRequestAsync(_db, user.Id, code: "12345678", expires: DateTime.MinValue, ct: TestContext.Current.CancellationToken);

        // Act
        await _deleteExpiredRequestsBackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        // Все истёкшие запросы удалились
        var countExpiredRefreshTokensFromDb = await _db.Requests.Where(x => x.Expires < DateTime.UtcNow).CountAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, countExpiredRefreshTokensFromDb);
    }
}