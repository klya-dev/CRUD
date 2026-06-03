using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public sealed class ImageSignatureCheckerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IImageSingnatureChecker _imageSingnatureChecker;

    public ImageSignatureCheckerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _imageSingnatureChecker = scopedServices.GetRequiredService<IImageSingnatureChecker>();
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