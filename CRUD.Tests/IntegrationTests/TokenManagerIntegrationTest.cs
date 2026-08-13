using Microsoft.AspNetCore.Mvc.Testing;
using System.Security.Claims;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class TokenManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ITokenManager _tokenManager;

    public TokenManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    [Theory] // Корректные данные
    [InlineData("1fa85f64-5717-4562-b3fc-2c963f66afa6", "klya", UserRoles.Admin, "ru", true, true, true)]
    public void GenerateAuthResponse_CorrectData_ReturnsAuthJwtResponse(string userId, string username, string role, string languageCode, bool isEmailConfirm, bool isPhoneNumberConfirm, bool isPremium)
    {
        // Arrange
        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(UserClaimTypes.LanguageCode, languageCode),
            new Claim(UserClaimTypes.IsEmailConfirm, isEmailConfirm.ToString()),
            new Claim(UserClaimTypes.IsPhoneNumberConfirm, isPhoneNumberConfirm.ToString()),
            new Claim(UserClaimTypes.IsPremium, isPremium.ToString())
        ];

        // Act
        var result = _tokenManager.GenerateAuthResponse(claims, username);

        // Assert
        Assert.NotNull(result);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.AccessToken, nameof(result.AccessToken));
        Assert.NotEqual(DateTime.MinValue, result.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.RefreshToken, nameof(result.RefreshToken));
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Username, nameof(result.Username));
    }


    [Fact]
    public void GenerateRefreshToken_CorrectData_ReturnsString()
    {
        // Arrange

        // Act
        var result = _tokenManager.GenerateRefreshToken();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
    }


    [Fact]
    public void GenerateUniqueToken_CorrectData_ReturnsString()
    {
        // Arrange

        // Act
        var result = _tokenManager.GenerateUniqueToken();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
    }


    [Fact]
    public void GenerateCode_CorrectData_ReturnsString()
    {
        // Arrange
        var length = 6;

        // Act
        var result = _tokenManager.GenerateCode();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
        Assert.Equal(result.Length, length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(100)]
    public void GenerateCode_Length_CorrectData_ReturnsString(int length)
    {
        // Arrange

        // Act
        var result = _tokenManager.GenerateCode(length);

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result);
        Assert.Equal(result.Length, length);
    }
}