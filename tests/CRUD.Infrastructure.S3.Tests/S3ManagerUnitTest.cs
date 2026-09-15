using Microsoft.Extensions.Logging;
using Moq;

namespace CRUD.Infrastructure.S3.Tests;

public sealed class S3ManagerUnitTest
{
    private readonly S3Manager _s3Manager;
    private readonly Mock<IOptions<S3Options>> _mockOptions;
    private readonly Mock<ILogger<S3Manager>> _mockLogger;

    public S3ManagerUnitTest()
    {
        _mockOptions = new();
        _mockLogger = new();

        _mockOptions.Setup(x => x.Value).Returns(new S3Options() { ServiceURL = "https://localhost", BucketName = "", AccessKey = "", SecretKey = "", LogsDirectory = "", LogsInS3Directory = "" });

        _s3Manager = new S3Manager(_mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetObjectAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null;

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.GetObjectAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }

    [Fact]
    public async Task GetObjectAsync_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        string key = string.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.GetObjectAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }


    [Theory]
    [InlineData(null, "some")]
    [InlineData("some", null)]
    [InlineData(null, null)]
    public async Task CopyObjectAsync_NullObject_ThrowsArgumentNullException(string sourceKey, string destinationKey)
    {
        // Arrange

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.CopyObjectAsync(sourceKey, destinationKey);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.NotNull(ex);
    }


    [Theory]
    [InlineData(true, null)]
    [InlineData(false, null)]
    public async Task CreateObjectAsync_NullObject_ThrowsArgumentNullException(bool isNullStream, string key)
    {
        // Arrange
        Stream stream = null;
        if (!isNullStream)
            stream = new MemoryStream();

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.CreateObjectAsync(key, stream);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.NotNull(ex); // Не проверяю аргумент, т.к разные значения
    }


    [Fact]
    public async Task DeleteObjectAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null;

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.DeleteObjectAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }


    [Fact]
    public async Task IsObjectExistsAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string key = null;

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.IsObjectExistsAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }

    [Fact]
    public async Task IsObjectExistsAsync_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        string key = string.Empty;

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.IsObjectExistsAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(a);

        // Assert
        Assert.Contains(nameof(key), ex.ParamName);
    }


    [Fact] // Опция сломанная, поэтому даже без мока не подключится
    public async Task CheckConnectionAsync_ReturnsFalse()
    {
        // Arrange

        // Act
        var result = await _s3Manager.CheckConnectionAsync(ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }
}