namespace CRUD.Tests.IntegrationTests;

public sealed class PasswordHasherIntegrationTest
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherIntegrationTest()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Theory]
    [InlineData("")]
    [InlineData("password")]
    [InlineData("@123$$@!_")]
    public void GenerateHashedPassword_ReturnsString(string password)
    {
        // Arrange
        int hashedPasswordLenght = 69;

        // Act
        var result = _passwordHasher.GenerateHashedPassword(password);

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result, nameof(result));
        Assert.Equal(hashedPasswordLenght, result.Length);
    }

    [Fact]
    public void GenerateHashedPassword_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string password = null;

        // Act
        Action a = () =>
        {
            _passwordHasher.GenerateHashedPassword(password);
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(password), ex.ParamName);
    }


    [Theory]
    [InlineData("123")]
    [InlineData("kekpass")]
    [InlineData("qwerty")]
    public void Verify_ReturnsTrue(string password)
    {
        // Arrange
        var hashedPassword = _passwordHasher.GenerateHashedPassword(password);

        // Act
        var result = _passwordHasher.Verify(password, hashedPassword);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("1")] // Неверный пароль
    [InlineData("kekpass_")]
    [InlineData("qwerty100")]
    public void Verify_ReturnsFalse(string password)
    {
        // Arrange
        var hashedPassword = _passwordHasher.GenerateHashedPassword(password + "SOMETHING_WRONG");

        // Act
        var result = _passwordHasher.Verify(password, hashedPassword);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "123")]
    [InlineData("123", null)]
    [InlineData(null, null)]
    public void Verify_NullObject_ThrowsArgumentNullException(string password, string hashedPassword)
    {
        // Arrange

        // Act
        Action a = () =>
        {
            _passwordHasher.Verify(password, hashedPassword);
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        if (!ex.ParamName.Contains(nameof(password)) && !ex.ParamName.Contains(nameof(hashedPassword)))
            Assert.Fail("Invalid paramName: " + ex.ParamName);
    }
}