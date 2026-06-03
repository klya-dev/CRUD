using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public sealed class TelegramIntegrationManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ITelegramIntegrationManager _telegramIntegrationManager;

    public TelegramIntegrationManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _telegramIntegrationManager = scopedServices.GetRequiredService<ITelegramIntegrationManager>();
    }


    [Fact]
    public async Task CheckConnectionAsync_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = await _telegramIntegrationManager.CheckConnectionAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }
}