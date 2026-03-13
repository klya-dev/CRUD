using System.Security.Claims;

namespace CRUD.Tests.UnitTests;

public class TokenManagerUnitTest
{
    private readonly TokenManager _tokenManager;
    private readonly Mock<IOptionsMonitor<AuthOptions>> _mockAuthOptions;
    private readonly Mock<IOptionsMonitor<AuthWebApiOptions>> _mockAuthWebApiOptions;

    public TokenManagerUnitTest()
    {
        _mockAuthOptions = new();
        _mockAuthWebApiOptions = new();

        _tokenManager = new TokenManager(_mockAuthOptions.Object, _mockAuthWebApiOptions.Object);
    }

    [Fact] // Claims is null
    public void GenerateAuthResponse_NullObject_Claims_ThrowsArgumentNullException()
    {
        // Arrange
        string username = "username";
        IEnumerable<Claim> claims = null;

        // Act
        Action a = () =>
        {
            _tokenManager.GenerateAuthResponse(claims, username);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(claims), ex.ParamName);
    }

    [Fact] // Username is null
    public void GenerateAuthResponse_NullObject_Username_ThrowsArgumentNullException()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();
        string username = null;
        string role = "role";
        string languageCode = "ru";
        bool isPremium = true;

        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "username"), // Claim сам по себе не даст записать null
            new Claim(ClaimTypes.Role, role),
            new Claim("language_code", languageCode),
            new Claim("premium", isPremium.ToString())
        ];

        // Act
        Action a = () =>
        {
            _tokenManager.GenerateAuthResponse(claims, username);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(username), ex.ParamName);
    }

    [Fact] // Username is whitespace
    public void GenerateAuthResponse_NullData_Username_ThrowsArgumentException()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();
        string username = " ";
        string role = "role";
        string languageCode = "ru"; 
        bool isPremium = true;

        IEnumerable<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("language_code", languageCode),
            new Claim("premium", isPremium.ToString())
        ];

        // Act
        Action a = () =>
        {
            _tokenManager.GenerateAuthResponse(claims, username);
        };

        // Assert
        var ex = Assert.Throws<ArgumentException>(a);
        Assert.Contains(nameof(username), ex.ParamName);
    }

    [Fact] // Claims is empty
    public void GenerateAuthResponse_Empty_Claims_ThrowsInvalidOperationException()
    {
        // Arrange
        string username = "username";
        IEnumerable<Claim> claims = [];

        // Act
        Action a = () =>
        {
            _tokenManager.GenerateAuthResponse(claims, username);
        };

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(a);
        Assert.Contains("Sequence contains no elements", ex.Message);
    }
}