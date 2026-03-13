#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;
using System.Security.Claims;

namespace CRUD.Tests.IntegrationTests;

public class TokenManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly ITokenManager _tokenManager;

    public TokenManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }

    private ITokenManager GenerateNewTokenManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<ITokenManager>();
    }

    [Theory] // Корректные данные
    [InlineData("1fa85f64-5717-4562-b3fc-2c963f66afa6", "klya", UserRoles.Admin, "ru", true)]
    public void GenerateAuthResponse_CorrectData_ReturnsAuthJwtResponse(string userId, string username, string role, string languageCode, bool isPremium)
    {
        // Arrange
        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("language_code", languageCode),
            new Claim("premium", isPremium.ToString())
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


    // Конфликты параллельности


    [Theory] // Корректные данные
    [InlineData("1fa85f64-5717-4562-b3fc-2c963f66afa6", "klya", UserRoles.Admin, "ru", true)]
    public async Task GenerateAuthResponse_ConcurrencyConflict_CorrectData_ReturnsAuthJwtResponse(string userId, string username, string role, string languageCode, bool isPremium)
    {
        // Arrange
        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("language_code", languageCode),
            new Claim("premium", isPremium.ToString())
        ];
        var tokenManager = GenerateNewTokenManager();
        var tokenManager2 = GenerateNewTokenManager();

        // Act
        var task = Task.Run(() => tokenManager.GenerateAuthResponse(claims, username));
        var task2 = Task.Run(() => tokenManager2.GenerateAuthResponse(claims, username));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Username);

        Assert.NotNull(result2);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.AccessToken);
        Assert.NotEqual(DateTime.MinValue, result2.Expires);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.RefreshToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result2.Username);
    }


    [Fact]
    public async Task GenerateUniqueToken_ConcurrencyConflict_CorrectData_ReturnsString()
    {
        // Arrange
        var tokenManager = GenerateNewTokenManager();
        var tokenManager2 = GenerateNewTokenManager();

        // Act
        var task = Task.Run(() => tokenManager.GenerateCode());
        var task2 = Task.Run(() => tokenManager2.GenerateCode());

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
            AssertExtensions.IsNotNullOrNotWhiteSpace(result);
    }


    [Fact]
    public async Task GenerateCode_ConcurrencyConflict_CorrectData_ReturnsString()
    {
        // Arrange
        var length = 6;
        var tokenManager = GenerateNewTokenManager();
        var tokenManager2 = GenerateNewTokenManager();

        // Act

        var task = Task.Run(() => tokenManager.GenerateCode(length));
        var task2 = Task.Run(() => tokenManager2.GenerateCode(length));

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            AssertExtensions.IsNotNullOrNotWhiteSpace(result);
            Assert.Equal(result.Length, length);
        }
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
    public async Task GenerateCode_ConcurrencyConflict_Length_CorrectData_ReturnsString(int length)
    {
        // Arrange
        var tokenManager = GenerateNewTokenManager();
        var tokenManager2 = GenerateNewTokenManager();

        // Act
        var task = Task.Run(() => tokenManager.GenerateCode(length));
        var task2 = Task.Run(() => tokenManager2.GenerateCode(length));

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            AssertExtensions.IsNotNullOrNotWhiteSpace(result);
            Assert.Equal(result.Length, length);
        }
    }
}