namespace CRUD.Tests.IntegrationTests;

public sealed class UserApiKeyManagerIntegrationTest
{
    private const int ApiKeyLenght = 100;

    private readonly UserApiKeyManager _userApiKeyManager;

    public UserApiKeyManagerIntegrationTest()
    {
        _userApiKeyManager = new UserApiKeyManager();
    }

    [Fact]
    public void GenerateUserApiKey_CorrectData_ReturnsString()
    {
        // Arrange

        // Act
        var result = _userApiKeyManager.GenerateUserApiKey();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result, nameof(result));
        Assert.Equal(ApiKeyLenght, result.Length);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
    }


    [Fact]
    public void GenerateDisposableUserApiKey_CorrectData_ReturnsString()
    {
        // Arrange

        // Act
        var result = _userApiKeyManager.GenerateDisposableUserApiKey();

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result, nameof(result));
        Assert.Equal(ApiKeyLenght, result.Length);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
    }
}