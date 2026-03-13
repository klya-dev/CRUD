using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class S3InitializerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IS3Initializer _s3Initializer;
    private readonly IS3Manager _s3Manager;

    public S3InitializerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration =>
        {
            configuration.UseContentRoot(Directory.GetCurrentDirectory());
        });

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _s3Initializer = scopedServices.GetRequiredService<IS3Initializer>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
    }

    //[Fact]
    //public async Task InitializeAsync_CorrectData_ReturnsTask()
    //{
    //    // Arrange
    //    var existsFolderBefore = await _s3Manager.IsObjectExistsAsync("avatars_test");

    //    // Act
    //    await _s3Initializer.InitializeAsync();

    //    // Assert
    //    var existsFolderAfter = await _s3Manager.IsObjectExistsAsync("avatars_test");
    //    Assert.False(existsFolderBefore);
    //    Assert.True(existsFolderAfter);

    //    // Удаляем за собой
    //    await _s3Manager.DeleteObjectAsync("avatars_test");
    //}
}