namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class ImageSignatureCheckerIntegrationTest
{
    private readonly ImageSingnatureChecker _imageSingnatureChecker;

    public ImageSignatureCheckerIntegrationTest()
    {
        _imageSingnatureChecker = new ImageSingnatureChecker();
    }

    [Theory]
    [InlineData("test.png")] // Корректные данные
    [InlineData("default.png")]
    public void IsFileValid_ReturnsIsValidAndExtension(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", fileName);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        // Act
        var result = _imageSingnatureChecker.IsFileValid(stream);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.FileExtension);
    }

    [Theory]
    [InlineData("NVtest2.bmp")] // Обычный bmp
    [InlineData("NVtest3.png")] // bmp переименнованый в png
    [InlineData("NVtest4.png")] // Пустой
    public void IsFileValid_ReturnsIsNotValidAndNullExtension(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", fileName);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        // Act
        var result = _imageSingnatureChecker.IsFileValid(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.FileExtension);
    }
}