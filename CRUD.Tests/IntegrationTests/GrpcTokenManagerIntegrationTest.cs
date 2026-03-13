using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class GrpcTokenManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IGrpcTokenManager _grpctokenManager;

    public GrpcTokenManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _grpctokenManager = scopedServices.GetRequiredService<IGrpcTokenManager>();
    }

    [Fact]
    public void GenerateAuthEmailSenderToken_ReturnsString()
    {
        // Arrange

        // Act
        var result = _grpctokenManager.GenerateAuthEmailSenderToken();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
    }

    [Fact] // Оба вызова вернут один и тот же токен т.к Scoped
    public void GenerateAuthEmailSenderToken_TwoCall_ReturnsString()
    {
        // Arrange

        // Act
        var result = _grpctokenManager.GenerateAuthEmailSenderToken();
        var result2 = _grpctokenManager.GenerateAuthEmailSenderToken();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
        Assert.Equal(result, result2);
    }
}