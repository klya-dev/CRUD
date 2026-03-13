using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class ImageSignatureCheckerIntegrationTest : IClassFixture<TestWebApplicationFactory>
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

    private IImageSingnatureChecker GenerateNewImageSingnatureChecker()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IImageSingnatureChecker>();
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


    // Конфликты параллельности


    [Theory]
    [InlineData("test.png")] // Корректные данные
    [InlineData("default.png")]
    public async Task IsFileValid_ConcurrencyConflict_ReturnsIsValidAndExtension(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", fileName);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var stream2 = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var avatarManager = GenerateNewImageSingnatureChecker();
        var avatarManager2 = GenerateNewImageSingnatureChecker();

        // Act
        var task = Task.Run(() => avatarManager.IsFileValid(stream));
        var task2 = Task.Run(() => avatarManager2.IsFileValid(stream2));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.FileExtension);

        Assert.Equivalent(result, result2);
    }

    [Theory]
    [InlineData("NVtest2.bmp")] // Обычный bmp
    [InlineData("NVtest3.png")] // bmp переименнованый в png
    [InlineData("NVtest4.png")] // Пустой
    public async Task IsFileValid_ConcurrencyConflict_ReturnsIsNotValidAndNullExtension(string fileName)
    {
        // Arrange
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", fileName);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var avatarManager = GenerateNewImageSingnatureChecker();
        var avatarManager2 = GenerateNewImageSingnatureChecker();

        // Act
        var task = Task.Run(() => avatarManager.IsFileValid(stream));
        var task2 = Task.Run(() => avatarManager2.IsFileValid(stream));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.FileExtension);

        Assert.Equivalent(result, result2);
    }
}