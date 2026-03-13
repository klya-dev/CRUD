using Microsoft.AspNetCore.WebUtilities;

namespace CRUD.Tests.UnitTests;

public class OAuthHelperUnitTest
{
    [Fact]
    public void GenerateCodeVerifier_ReturnsCodeVerifier()
    {
        // Arrange

        // Act
        var result = OAuthHelper.GenerateCodeVerifier();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, WebEncoders.Base64UrlDecode(result).Length);
    }

    [Fact]
    public void GetCodeChallenge_ReturnsCodeChallenge()
    {
        // Arrange
        var codeVerifier = OAuthHelper.GenerateCodeVerifier();

        // Act
        var result = OAuthHelper.GetCodeChallenge(codeVerifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, WebEncoders.Base64UrlDecode(result).Length);
    }

    [Fact]
    public void GenerateNonce_ReturnsNonce()
    {
        // Arrange

        // Act
        var result = OAuthHelper.GenerateNonce();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, WebEncoders.Base64UrlDecode(result).Length);
    }

    [Fact]
    public async Task DownloadPictureAsync_ReturnsStream()
    {
        // Arrange
        var url = "https://filin.mail.ru/pic?d=LHWIAVI9Bmqq-UAzSRq6yA1J_o-rvlv1PSR85MXdulxodK9yOvgAj89nM5bITfA~&name=%D1%84%D0%B0%D0%BD%D1%82%D0%BE%D0%BC+%D0%B0%D1%81%D1%81%D1%81%D0%B8%D0%BD";

        // Act
        var result = await OAuthHelper.DownloadPictureAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        Assert.True(result.CanSeek);
    }
}