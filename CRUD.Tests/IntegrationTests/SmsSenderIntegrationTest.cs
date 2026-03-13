using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class SmsSenderIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ISmsSender _smsSender;

    public SmsSenderIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _smsSender = scopedServices.GetRequiredService<ISmsSender>();
    }

    [Fact]
    public async Task TestAuthAsync_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = await _smsSender.TestAuthAsync();

        // Assert
        Assert.True(result);
    }
}