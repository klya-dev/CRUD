using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CRUD.Infrastructure.S3.Tests;

public sealed class S3ManagerIntegrationTest
{
    private readonly S3Manager _s3Manager;

    public S3ManagerIntegrationTest()
    {
        var s3Options = TestSettingsHelper.GetConfigurationValue<S3Options, TestMarker>(S3Options.SectionName);
        var options = Options.Create(s3Options);
        ILogger<S3Manager> logger = NullLogger<S3Manager>.Instance;

        _s3Manager = new S3Manager(options, logger);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task GetObjectAsync_ReturnsServiceResult(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value);
        Assert.NotNull(result.Value.Stream);
    }

    [Theory] // Этих объектов не существует
    [InlineData("   ")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/none")]
    [InlineData("NONE")]
    public async Task GetObjectAsync_ReturnsErrorMessage_FileNotFound(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task GetPresignedUrlAsync_ReturnsServiceResult(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.GetPresignedUrlAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value);
    }

    [Theory] // Этих объектов не существует, возвращается ссылка, внутри которой XML с ошибкой "NoSuchKey"
    [InlineData("   ")]
    [InlineData("NONE")]
    public async Task GetPresignedUrlAsync_WhenObjectIsNotExists_ReturnsUrlWithErrorNoSuchKey(string key)
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var result = await _s3Manager.GetPresignedUrlAsync(key);

        // Assert
        Assert.NotNull(result);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value);

        // Ошибка NoSuchKey
        var xmlStream = await (await httpClient.GetAsync(result.Value, TestContext.Current.CancellationToken)).Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        XDocument xmlDoc = XDocument.Load(xmlStream);
        var code = xmlDoc.Document.Root.Descendants("Code").First().Value;
        Assert.Equal("NoSuchKey", code.ToString());
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/default.png", $"{TestConstants.TEST_FILES_PATH}/copy_default.png")]
    public async Task CopyObjectAsync_ReturnsServiceResult(string sourceKey, string destinationKey)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CopyObjectAsync(sourceKey, destinationKey, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(destinationKey, ct: TestContext.Current.CancellationToken);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(destinationKey, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/something.png", $"{TestConstants.TEST_FILES_PATH}/copy.png")]
    public async Task CopyObjectAsync_ReturnsErrorMessage_FileNotFound(string sourceKey, string destinationKey)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CopyObjectAsync(sourceKey, destinationKey, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsync_ReturnsServiceResult(string key)
    {
        // Arrange
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, memStream, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    public async Task CreateObjectAsync_WithOptions_ReturnsServiceResult(string key)
    {
        // Arrange
        var contentType = "video/ogg";

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, memStream, options => options.ContentType = contentType, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект и вправду создался и тип контента совпадает
        var objectAfterCreate = await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.False(existsObjectBeforeCreate);
        Assert.Equal(contentType, objectAfterCreate.Value.ContentType);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key, ct: TestContext.Current.CancellationToken);
    }

    [Theory] // S3 перезапишет объект
    [InlineData($"{TestConstants.TEST_FILES_PATH}/NVtest.png")] // Этот объект уже существует
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")] // Этот объект уже существует
    public async Task CreateObjectAsync_WhenCheckExistsFalse_ReturnsServiceResult(string key)
    {
        // Arrange
        // Бекапим объект до перезаписи, чтобы в конце теста всё восстановить
        using var streamBackup = (await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStreamBackup = new MemoryStream();
        streamBackup.CopyTo(memStreamBackup);
        memStreamBackup.Seek(0, SeekOrigin.Begin);

        // Объект для перезаписи
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Т.е в NVtest.png вписываем test.png

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, memStream, checkExists: false, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект существует
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.True(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Объект и вправду перезаписался (сравниваем байты, которые должны были вписаться с байтами, которые вписались)
        var overridedObjectStream = (await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStreamOverride = new MemoryStream();
        overridedObjectStream.CopyTo(memStreamOverride);
        memStreamOverride.Seek(0, SeekOrigin.Begin);
        Assert.Equal(memStream.ToArray(), memStreamOverride.ToArray());

        // Восстанавливаем за собой
        await _s3Manager.CreateObjectAsync(key, memStreamBackup, checkExists: false, ct: TestContext.Current.CancellationToken);
    }

    [Theory] // S3 не создаст объект
    [InlineData($"{TestConstants.TEST_FILES_PATH}/NVtest.png")] // Этот объект уже существует
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")] // Этот объект уже существует
    public async Task CreateObjectAsync_WhenCheckExistsTrue_ReturnsErrorMessage_FileAlreadyExists(string key)
    {
        // Arrange
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, memStream, checkExists: true, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.FileAlreadyExists, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/newfile.txt")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/avatars")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/avatars/")]
    public async Task CreateObjectAsyncWithoutStream_ReturnsServiceResult(string key)
    {
        // Arrange
        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект и вправду создался
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.False(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(key, ct: TestContext.Current.CancellationToken);
    }

    [Theory] // S3 перезапишет объект (test.png превратится в пустой объект ("папку"))
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Этот объект уже существует
    public async Task CreateObjectAsyncWithoutStream_WhenCheckExistsFalse_ReturnsServiceResult(string key)
    {
        // Arrange
        // Бекапим объект до перезаписи, чтобы в конце теста всё восстановить
        using var streamBackup = (await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStreamBackup = new MemoryStream();
        streamBackup.CopyTo(memStreamBackup);
        memStreamBackup.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, checkExists: false, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект существует
        var existsObjectAfterCreate = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.True(existsObjectBeforeCreate);
        Assert.True(existsObjectAfterCreate);

        // Объект и вправду перезаписался
        var overridedObjectStream = (await _s3Manager.GetObjectAsync(key, ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStreamOverride = new MemoryStream();
        overridedObjectStream.CopyTo(memStreamOverride);
        memStreamOverride.Seek(0, SeekOrigin.Begin);
        Assert.Empty(memStreamOverride.ToArray()); // Пустой массив, т.к мы не передавали никакие байты

        // Восстанавливаем за собой
        await _s3Manager.CreateObjectAsync(key, memStreamBackup, checkExists: false, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/NVtest.png")] // Этот объект уже существует
    public async Task CreateObjectAsyncWithoutStream_WhenCheckExistsTrue_ReturnsErrorMessage_FileAlreadyExists(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.CreateObjectAsync(key, checkExists: true, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);
        Assert.Contains(ErrorMessages.FileAlreadyExists, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test2.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    public async Task DeleteObjectAsync_ReturnsServiceResult(string key)
    {
        // Arrange
        // Чтобы восстановить за собой
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var existsObjectBeforeDelete = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.DeleteObjectAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объект и вправду удалился
        var existsObjectAfterDelete = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.True(existsObjectBeforeDelete);
        Assert.False(existsObjectAfterDelete);

        // Восстанавливаем за собой
        await _s3Manager.CreateObjectAsync(key, memStream, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/somelog.txt")] // Этого объекта не существует
    public async Task DeleteObjectAsync_WhenObjectNotExists_ReturnsServiceResult(string key)
    {
        // Arrange
        var existsObjectBeforeDelete = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _s3Manager.DeleteObjectAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Value);

        // Объекта не существует
        var existsObjectAfterDelete = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);
        Assert.False(existsObjectBeforeDelete);
        Assert.False(existsObjectAfterDelete);
    }


    [Theory] // Обязательно прочитать "Просто ебанная Санта-Барбара" в проекте
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/log.txt")]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/")] // Ручное создание пустого объекта
    [InlineData("avatars/default.png")] // Объект в визуальной папке
    public async Task IsObjectExistsAsync_ReturnsTrue(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}")] // Ручное создание пустого объекта без слеша
    [InlineData($"{TestConstants.TEST_FILES_PATH}/none.png")]
    [InlineData("ava")] // Префикс (так-то используется для ListObjectsAsync)
    [InlineData("avatars/")] // Визуальная папка (создание через код). Конкретно такого объекта не существует
    [InlineData("avatars")] // Визуальная папка
    public async Task IsObjectExistsAsync_ReturnsFalse(string key)
    {
        // Arrange

        // Act
        var result = await _s3Manager.IsObjectExistsAsync(key, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("/")] // Такого объекта нет
    [InlineData("/avatars")] // Визуальная папка
    public async Task IsObjectExistsAsync_ThrowsForbidden(string key)
    {
        // Arrange

        // Act
        Func<Task> a = async () =>
        {
            await _s3Manager.IsObjectExistsAsync(key);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(a);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, ex.StatusCode);
    }


    [Fact]
    public async Task CheckConnectionAsync_ReturnsTrue()
    {
        // Arrange

        // Act
        var result = await _s3Manager.CheckConnectionAsync(ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }
    // CheckConnectionAsync_ReturnsFalse в юнит тесте
}