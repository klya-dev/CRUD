using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class PayManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPayManager _payManager;

    public PayManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _payManager = scopedServices.GetRequiredService<IPayManager>();
    }

    [Fact]
    public async Task CheckConnectionAsync_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = await _payManager.CheckConnectionAsync();

        // Assert
        Assert.True(result);
    }
}