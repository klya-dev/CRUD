using Amazon.S3.Model;
using System.Text;

namespace CRUD.Tests.UnitTests;

public sealed class AvatarManagerUnitTest
{
    private readonly AvatarManager _avatarManager;
    private readonly Mock<IS3Manager> _mockS3Manager;
    private readonly Mock<IOptions<AvatarManagerOptions>> _mockAvatarManagerOptions;
    private readonly Mock<ILogger<AvatarManager>> _mockLogger;
    private readonly Mock<IImageSingnatureChecker> _mockImageSignatureChecker;
    private readonly ApplicationDbContext _db;

    public AvatarManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockS3Manager = new();
        _mockAvatarManagerOptions = new();
        _mockLogger = new();
        _mockImageSignatureChecker = new();

        _mockAvatarManagerOptions.Setup(x => x.Value).Returns(TestSettingsHelper.GetConfigurationValue<AvatarManagerOptions, TestMarker>(AvatarManagerOptions.SectionName)!);

        _avatarManager = new AvatarManager(_mockS3Manager.Object, _mockAvatarManagerOptions.Object, db, _mockLogger.Object, _mockImageSignatureChecker.Object);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("test")]
    public async Task GetAvatarAsync_ReturnsStreamAndFileExtension(string fileName)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{_mockAvatarManagerOptions.Object.Value.AvatarsInS3Directory}/{fileName}.png", ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Успешно получаем объект (не пустой поток)
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("something"));
        _mockS3Manager.Setup(x => x.GetObjectAsync(It.IsAny<string>(), It.IsAny<Action<GetObjectRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<S3FileContent>.Success(new S3FileContent(stream, null, null, 0, null)));

        // Act
        var result = await _avatarManager.GetAvatarAsync(userIdGuid, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value.Stream);
        Assert.True(result.Value.Stream.Length > 0);
        Assert.Empty(result.Value.FileExtension);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenEmptyGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.GetAvatarAsync(userIdGuid, TestContext.Current.CancellationToken);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _avatarManager.GetAvatarAsync(userIdGuid, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value.Stream);
        Assert.Null(result.Value.FileExtension);

        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenFileNotFound_ReturnsErrorMessage_FileNotFound()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Не удалось получить объект
        _mockS3Manager.Setup(x => x.GetObjectAsync(It.IsAny<string>(), It.IsAny<Action<GetObjectRequest>>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<S3FileContent>.Fail(ErrorMessages.FileNotFound));

        // Act
        var result = await _avatarManager.GetAvatarAsync(userIdGuid, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value.Stream);
        Assert.Null(result.Value.FileExtension);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData("default")]
    [InlineData("test")]
    public async Task GetPresignedUrlAvatarAsync_ReturnsUrl(string fileName)
    {
        // Arrange
        var expectedUrl = "some";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{_mockAvatarManagerOptions.Object.Value.AvatarsInS3Directory}/{fileName}.png", ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Успешно получаем url
        _mockS3Manager.Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<Action<GetPreSignedUrlRequest>>())).ReturnsAsync(ServiceResult<string>.Success(expectedUrl));

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.Equal(expectedUrl, result.Value);
    }

    [Fact]
    public async Task GetPresignedUrlAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task GetPresignedUrlAvatarAsync_WhenFileNotFound_ReturnsUrl()
    {
        // Arrange
        var expectedUrl = "some";

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Успешно получаем url
        _mockS3Manager.Setup(x => x.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<Action<GetPreSignedUrlRequest>>())).ReturnsAsync(ServiceResult<string>.Success(expectedUrl));

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(userIdGuid, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.Equal(expectedUrl, result.Value);
    }


    [Fact]
    public async Task SetAvatarAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);
        var userIdGuid = user.Id;

        // Пользователь до обновления
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid, TestContext.Current.CancellationToken);

        // Получаем поток дефолтной аватарки
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "default.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png", "image/png"));

        // Успешно создаём объект
        _mockS3Manager.Setup(x => x.CreateObjectAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<Action<PutObjectRequest>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<S3OperationResult>.Success(new S3OperationResult(null, System.Net.HttpStatusCode.NoContent, 0)));

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду обновилась
        Assert.NotEqual(user.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenEmptyGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);

        // Получаем поток дефолтной аватарки
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "default.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenDoesNotMatchSignature_ReturnsErrorMessage_DoesNotMatchSignature()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Не подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((false, null!, null!));

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp, пустой файл)
        string[] files = ["NVtest2.bmp", "NVtest3.png", "NVtest4.png"];
        foreach (var file in files)
        {
            // Получаем поток файла
            var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", file);
            using var stream = new FileStream(filePath, FileMode.Open);

            // Act
            var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(ErrorMessages.DoesNotMatchSignature, result.ErrorMessage);
        }
    }

    [Fact]
    public async Task SetAvatarAsync_WhenFileSizeLimitExceeded_ReturnsErrorMessage_FileSizeLimitExceeded()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Получаем поток файла
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "NVtest.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png", "image/png"));

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileSizeLimitExceeded, result.ErrorMessage);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "test.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png", "image/png"));

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();
        Stream stream = null;

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.SetAvatarAsync(userIdGuid, stream, TestContext.Current.CancellationToken);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(stream), ex.ParamName);
    }


    [Fact]
    public async Task DeleteAvatarAsync_WhenNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string avatarUrl = null;

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.DeleteAvatarAsync(avatarUrl);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(avatarUrl), ex.ParamName);
    }
}