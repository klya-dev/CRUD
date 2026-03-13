namespace CRUD.Tests.UnitTests;

public class ImageSignatureCheckerUnitTest
{
    private readonly ImageSingnatureChecker _imageSingnatureChecker;

    public ImageSignatureCheckerUnitTest()
    {
        _imageSingnatureChecker = new ImageSingnatureChecker();
    }

    [Theory]
    [InlineData("test.png")]
    [InlineData("default.png")]
    public void IsFileValid_ReturnsIsValidAndExtension(string file)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", file);
        using var stream = new FileStream(filePath, FileMode.Open);

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
    public void IsFileValid_WhenNotFalidFile_ReturnsIsNotValidAndNullExtension(string file)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", file);
        using var stream = new FileStream(filePath, FileMode.Open);

        // Act
        var result = _imageSingnatureChecker.IsFileValid(stream);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.FileExtension);
    }

    [Fact]
    public void IsFileValid_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Stream stream = null;

        // Act
        Action a = () =>
        {
            _imageSingnatureChecker.IsFileValid(stream);
        };

        var ex = Assert.Throws<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(stream), ex.ParamName);
    }
}