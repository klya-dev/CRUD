#nullable disable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRUD.Infrastructure.S3.Tests;

public class S3ManagerIntegrationTest
{
    // #nullable disable

    private readonly S3Manager _s3Manager;

    public S3ManagerIntegrationTest()
    {
        var s3Options = TestSettingsHelper.GetConfigurationValue<S3Options, TestMarker>(S3Options.SectionName);
        var options = Options.Create(s3Options);
        ILogger<S3Manager> logger = NullLogger<S3Manager>.Instance;

        _s3Manager = new S3Manager(options, logger);
    }

    private static S3Manager GenerateNewS3Manager()
    {
        var s3Options = TestSettingsHelper.GetConfigurationValue<S3Options, TestMarker>(S3Options.SectionName);
        var options = Options.Create(s3Options);
        ILogger<S3Manager> logger = NullLogger<S3Manager>.Instance;

        return new S3Manager(options, logger);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task GetObjectAsync_ReturnsStream(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.GetObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
    }

    [Theory] // Этих файлов не существует
    [InlineData("   ")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/none")]
    [InlineData("NONE")]
    public async Task GetObjectAsync_ReturnsErrorMessage_FileNotFound(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.GetObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }

    [Theory] // Исключение из самого S3
    [InlineData("")] // System.ArgumentException : Key is a required property and must be set before making this call. (Parameter 'GetObjectRequest.Key')
    public async Task GetObjectAsync_ThrowsArgumentException(string key)
    {
        // Arrange

        // Act
        Func<Task> a = async () => 
        {
            await _s3Manager.GetObjectAsync(key);
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(a);

        // Assert
        Assert.Contains("Key is a required property and must be set before making this call", ex.Message);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/default.png", $"{TestConstants.TEST_FILES_PATH}/copy_default.png")]
    public async Task CopyObjectAsync_ReturnsServiceResult(string sourceKey, string destinationKey)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CopyObjectAsync(sourceKey, destinationKey);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(destinationKey);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(destinationKey);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/something.png", $"{TestConstants.TEST_FILES_PATH}/copy.png")]
    public async Task CopyObjectAsync_ReturnsErrorMessage_FileNotFound(string sourceKey, string destinationKey)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CopyObjectAsync(sourceKey, destinationKey);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsync_ReturnsServiceResult(string key)
    {
        // Arrange
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var result = await _s3Manager.CreateObjectAsync(memStream, key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/NVtest.png")] // Этот файл уже существует
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")] // Этот файл уже существует
    public async Task CreateObjectAsync_ReturnsErrorMessage_FileAlreadyExists(string key)
    {
        // Arrange
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _s3Manager.CreateObjectAsync(memStream, key);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileAlreadyExists, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsyncByKey_ReturnsServiceResult(string key)
    {
        // Arrange
        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/NVtest.png")] // Этот файл уже существует
    public async Task CreateObjectAsyncByKey_ReturnsErrorMessage_FileAlreadyExists(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CreateObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileAlreadyExists, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test2.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task DeleteObjectAsync_ReturnsServiceResult(string key)
    {
        // Arrange
        // Чтобы восстановить за собой
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var result = await _s3Manager.DeleteObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Файл и вправду удалился
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.True(existsObjectBeforeCreate);
        Assert.False(existsObjectAfterCreate);

        // Восстанавливаем за собой
        await _s3Manager.CreateObjectAsync(memStream, key);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test22.png")] // Этот объект не найден
    [InlineData($"{TestConstants.TEST_FILES_PATH}/somelog.txt")]
    public async Task DeleteObjectAsync_ReturnsErrorMessage_FileNotFound(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.DeleteObjectAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}")]
    [InlineData("")]
    public async Task IsObjectExistsAsync_ReturnsTrue(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.IsObjectExistsAsync(key);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/none.png")]
    public async Task IsObjectExistsAsync_ReturnsFalse(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.IsObjectExistsAsync(key);

        // Assert
        Assert.False(result);
    }


    [Fact]
    public async Task CheckConnectionAsync_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = await _s3Manager.CheckConnectionAsync();

        // Assert
        Assert.True(result);
    }
    // CheckConnectionAsync_ReturnsFalse в юнит тесте


    // Конфликты параллельности


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task GetObjectAsync_ConcurrencyConflict_ReturnsStream(string key)
    {
        // Arrange
        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        // Act
        var task = s3Manager.GetObjectAsync(key);
        var task2 = s3Manager2.GetObjectAsync(key);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        Assert.NotNull(result2);
        Assert.Null(result2.ErrorMessage);
        Assert.NotNull(result2.Value);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/default.png", $"{TestConstants.TEST_FILES_PATH}/copy_default.png")]
    public async Task CopyObjectAsync_ConcurrencyConflict_ReturnsErrorMessage_Nothing(string sourceKey, string destinationKey)
    {
        // Arrange
        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        // Act
        var task = s3Manager.CopyObjectAsync(sourceKey, destinationKey);
        var task2 = s3Manager2.CopyObjectAsync(sourceKey, destinationKey);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            // У обоих скопировалось без ошибок
            Assert.NotNull(result);
            Assert.Null(result.ErrorMessage);
        }

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(destinationKey);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(destinationKey);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrFileAlreadyExists(string key)
    {
        // Arrange
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        using var stream2 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream2 = new MemoryStream();
        stream2.CopyTo(memStream2);
        memStream2.Seek(0, SeekOrigin.Begin);

        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var task = s3Manager.CreateObjectAsync(memStream, key);
        var task2 = s3Manager2.CreateObjectAsync(memStream2, key);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Либо ничего, либо файл уже существует
            var errorMessage = result.ErrorMessage;
            string[] allowedErrors =
            [
                null,
                ErrorMessages.FileAlreadyExists
            ];

            Assert.Contains(errorMessage, allowedErrors);
        }

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsyncByKey_ConcurrencyConflict_ReturnsErrorMessage_NothingOrFileAlreadyExists(string key)
    {
        // Arrange
        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();
        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var task = s3Manager.CreateObjectAsync(key);
        var task2 = s3Manager2.CreateObjectAsync(key);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Либо ничего, либо файл уже существует
            var errorMessage = result.ErrorMessage;
            string[] allowedErrors =
            [
                null,
                ErrorMessages.FileAlreadyExists
            ];

            Assert.Contains(errorMessage, allowedErrors);
        }

        // Файл и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test2.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task DeleteObjectAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrFileNotFound(string key)
    {
        // Arrange
        // Чтобы восстановить за собой
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key);

        // Act
        var task = s3Manager.DeleteObjectAsync(key);
        var task2 = s3Manager2.DeleteObjectAsync(key);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Либо ничего, либо файл уже существует
            var errorMessage = result.ErrorMessage;
            string[] allowedErrors =
            [
                null,
                ErrorMessages.FileAlreadyExists
            ];

            Assert.Contains(errorMessage, allowedErrors);
        }

        // Файл и вправду удалился
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key);
        Assert.True(existsObjectBeforeCreate);
        Assert.False(existsObjectAfterCreate);

        // Восстанавливаем за собой
        await _s3Manager.CreateObjectAsync(memStream, key);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}")]
    [InlineData("")]
    public async Task IsObjectExistsAsync_ConcurrencyConflict_ReturnsTrue(string key)
    {
        // Arrange
        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        // Act
        var task = s3Manager.IsObjectExistsAsync(key);
        var task2 = s3Manager2.IsObjectExistsAsync(key);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.True(result);
        Assert.Equivalent(result, result2);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/none.png")]
    public async Task IsObjectExistsAsync_ConcurrencyConflict_ReturnsFalse(string key)
    {
        // Arrange
        var s3Manager = GenerateNewS3Manager();
        var s3Manager2 = GenerateNewS3Manager();

        // Act
        var task = s3Manager.IsObjectExistsAsync(key);
        var task2 = s3Manager2.IsObjectExistsAsync(key);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);
    }
}