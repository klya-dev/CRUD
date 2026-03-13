namespace CRUD.Tests.UnitTests;

public class RandomDataGeneratorUnitTest
{
    [Fact]
    public void GenerateRandomUsername_ReturnsUsername()
    {
        // Arrange

        // Act
        var result = RandomDataGenerator.GenerateRandomUsername();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
        Assert.StartsWith("und-", result);
    }

    [Fact]
    public void GenerateRandomPassword_ReturnsPassword()
    {
        // Arrange

        // Act
        var result = RandomDataGenerator.GenerateRandomPassword();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length);
    }
}