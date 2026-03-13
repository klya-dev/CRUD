#nullable disable
using Microsoft.AspNetCore.Http;

namespace CRUD.Tests.IntegrationTests;

public class EmailLettersIntegrationTest
{
    // #nullable disable

    private readonly IHttpContextAccessor _httpContextAccessor = TestConstants.CreateHttpContextAccessor();

    [Theory]
    [InlineData(EmailLetters.EmailConfirm, "test@mail.ru", "ru")]
    public void GetLetter_CorrectData_ReturnsString(string key, string email, string languageCode)
    {
        // Arrange
        string[] args = [ "token" ];

        // Act
        var result = EmailLetters.GetLetter(key, email, languageCode, _httpContextAccessor.GetBaseUrl(), args);

        // Assert
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Subject);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Body);
    }

    [Theory] // Необработаный ключ
    [InlineData("НЕТ ТАКОГО КЛЮЧА", "test@mail.ru", "ru")]
    [InlineData("", "", "")]
    public void GetLetter_UnhandledError_ThrowsInvalidOperationException(string key, string email, string languageCode)
    {
        // Arrange

        // Act
        Action a = () =>
        {
            EmailLetters.GetLetter(key, email, languageCode, _httpContextAccessor.GetBaseUrl());
        };

        var ex = Assert.Throws<InvalidOperationException>(a);

        // Assert
        Assert.Contains("Raw outcome: " + key, ex.Message);
    }

    [Fact]
    public void GetLetter_NullObject_Key_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null;
        string email = "test@mail.ru";
        string languageCode = "ru";

        // Act
        Action a = () =>
        {
            EmailLetters.GetLetter(key, email, languageCode, _httpContextAccessor.GetBaseUrl());
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }

    [Fact]
    public void GetLetter_NullObject_Email_ThrowsArgumentNullException()
    {
        // Arrange
        string key = EmailLetters.EmailConfirm;
        string email = null;
        string languageCode = "ru";
        string token = "";

        // Act
        Action a = () =>
        {
            EmailLetters.GetLetter(key, email, languageCode, _httpContextAccessor.GetBaseUrl(), token);
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(email), ex.ParamName);
    }

    [Fact]
    public void GetLetter_NullObject_LanguageCode_ThrowsArgumentNullException()
    {
        // Arrange
        string key = EmailLetters.EmailConfirm;
        string email = "test@mail.ru";
        string languageCode = null;

        // Act
        Action a = () =>
        {
            EmailLetters.GetLetter(key, email, languageCode, _httpContextAccessor.GetBaseUrl());
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(languageCode), ex.ParamName);
    }
}