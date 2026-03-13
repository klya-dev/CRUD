#nullable disable

namespace CRUD.Tests.IntegrationTests;

public class UserApiKeyManagerIntegrationTest
{
    // #nullable disable

    private const int ApiKeyLenght = 100;

    private readonly UserApiKeyManager _userApiKeyManager;

    public UserApiKeyManagerIntegrationTest()
    {
        _userApiKeyManager = new UserApiKeyManager();
    }

    private static UserApiKeyManager GenerateNewUserApiKeyManager()
    {
        return new UserApiKeyManager();
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


    // Конфликты параллельности


    [Fact]
    public async Task GenerateUserApiKey_ConcurrencyConflict_CorrectData_ReturnsString()
    {
        // Arrange
        var userApiKeyManager = GenerateNewUserApiKeyManager();
        var userApiKeyManager2 = GenerateNewUserApiKeyManager();

        // Act
        var task = Task.Run(() => userApiKeyManager.GenerateUserApiKey());
        var task2 = Task.Run(() => userApiKeyManager2.GenerateUserApiKey());

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result, nameof(result));
        Assert.Equal(ApiKeyLenght, result.Length);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);

        AssertExtensions.IsNotNullOrNotWhiteSpace(result2, nameof(result2));
        Assert.Equal(ApiKeyLenght, result2.Length);
        Assert.DoesNotContain("+", result2);
        Assert.DoesNotContain("/", result2);
    }


    [Fact]
    public async Task GenerateDisposableUserApiKey_ConcurrencyConflict_CorrectData_ReturnsString()
    {
        // Arrange
        var userApiKeyManager = GenerateNewUserApiKeyManager();
        var userApiKeyManager2 = GenerateNewUserApiKeyManager();

        // Act
        var task = Task.Run(() => userApiKeyManager.GenerateDisposableUserApiKey());
        var task2 = Task.Run(() => userApiKeyManager2.GenerateDisposableUserApiKey());

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result, nameof(result));
        Assert.Equal(ApiKeyLenght, result.Length);
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);

        AssertExtensions.IsNotNullOrNotWhiteSpace(result2, nameof(result2));
        Assert.Equal(ApiKeyLenght, result2.Length);
        Assert.DoesNotContain("+", result2);
        Assert.DoesNotContain("/", result2);
    }
}