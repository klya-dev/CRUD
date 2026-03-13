using System.Text;

namespace CRUD.Tests.UnitTests;

public class AvatarManagerUnitTest
{
    private readonly AvatarManager _avatarManager;
    private readonly Mock<IS3Manager> _mockS3Manager;
    private readonly Mock<IOptions<AvatarManagerOptions>> _mockAvatarManagerOptions;
    private readonly Mock<ILogger<AvatarManager>> _mockLogger;
    private readonly Mock<IImageSingnatureChecker> _mockImageSignatureChecker;
    private readonly Mock<IValidator<User>> _mockUserValidator;
    private readonly ApplicationDbContext _db;

    public AvatarManagerUnitTest()
    {
        var db = DbContextGenerator.GenerateDbContextTestInMemory();
        _db = db;

        _mockS3Manager = new();
        _mockAvatarManagerOptions = new();
        _mockLogger = new();
        _mockImageSignatureChecker = new();
        _mockUserValidator = new();

        _mockAvatarManagerOptions.Setup(x => x.Value).Returns(TestSettingsHelper.GetConfigurationValue<AvatarManagerOptions, TestMarker>(AvatarManagerOptions.SectionName)!);

        _avatarManager = new AvatarManager(_mockS3Manager.Object, _mockAvatarManagerOptions.Object, db, _mockLogger.Object, _mockImageSignatureChecker.Object, _mockUserValidator.Object);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("test")]
    public async Task GetAvatarAsync_ReturnsStreamAndFileExtension(string fileName)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{_mockAvatarManagerOptions.Object.Value.AvatarsInS3Directory}/{fileName}.png");
        var userIdGuid = user.Id;

        // Успешно получаем объект (не пустой поток)
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("something"));
        _mockS3Manager.Setup(x => x.GetObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<Stream>.Success(stream));

        // Act
        var result = await _avatarManager.GetAvatarAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value.Stream);
        Assert.True(result.Value.Stream.Length > 0);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.FileExtension);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenEmptyGuid_ThrowsInvalidOperationException_EmptyUniqueIdentifier()
    {
        // Arrange
        var userIdGuid = Guid.Parse(TestConstants.EmptyGuidString);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.GetAvatarAsync(userIdGuid);
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
        var result = await _avatarManager.GetAvatarAsync(userIdGuid);

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
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Не удалось получить объект
        _mockS3Manager.Setup(x => x.GetObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<Stream>.Fail(ErrorMessages.FileNotFound));

        // Act
        var result = await _avatarManager.GetAvatarAsync(userIdGuid);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value.Stream);
        Assert.Null(result.Value.FileExtension);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Fact]
    public async Task SetAvatarAsync_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Пользователь до обновления
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Получаем поток дефолтной аватарки
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "default.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png"));

        // Успешно создаём объект
        _mockS3Manager.Setup(x => x.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Валидация проходит
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream);

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
            await _avatarManager.SetAvatarAsync(userIdGuid, stream);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.EmptyUniqueIdentifier, ex.Message);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task SetAvatarAsync_WhenUserNotValid_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Получаем поток дефолтной аватарки
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "default.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png"));

        // Успешно создаём объект
        _mockS3Manager.Setup(x => x.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Успешно удаляем напрасно созданный файл аватарки
        _mockS3Manager.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Валидация не проходит
        var validationResult = new ValidationResult() { Errors = [new ValidationFailure()] };
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.SetAvatarAsync(userIdGuid, stream);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и аватарка и вправду не обновились
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact] // Перед записью в базу выбросится исключение, о том, что User невалидный
    public async Task SetAvatarAsync_WhenUserNotValidAndFailToDeleteAvatar_ThrowsInvalidOperationException_NotValidBeforeUpdate()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);
        var userIdGuid = user.Id;

        // Получаем поток дефолтной аватарки
        var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "default.png");
        using var stream = new FileStream(filePath, FileMode.Open);

        // Подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png"));

        // Успешно создаём объект
        _mockS3Manager.Setup(x => x.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Success());

        // Успешно удаляем напрасно созданный файл аватарки
        _mockS3Manager.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult.Fail(ErrorMessages.FileNotFound));

        // Валидация не проходит
        var validationResult = new ValidationResult() { Errors = [new ValidationFailure()] };
        _mockUserValidator.Setup(x => x.ValidateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.SetAvatarAsync(userIdGuid, stream);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и аватарка и вправду не обновились
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userIdGuid);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenDoesNotMatchSignature_ReturnsErrorMessage_DoesNotMatchSignature()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        // Не подходит сигнатура файла
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((false, null!));

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp, пустой файл)
        string[] files = ["NVtest2.bmp", "NVtest3.png", "NVtest4.png"];
        foreach (var file in files)
        {
            // Получаем поток файла
            var filePath = Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", file);
            using var stream = new FileStream(filePath, FileMode.Open);

            // Act
            var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream);

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
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png"));

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream);

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
        _mockImageSignatureChecker.Setup(x => x.IsFileValid(It.IsAny<Stream>())).Returns((true, "png"));

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, stream);

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
            await _avatarManager.SetAvatarAsync(userIdGuid, stream);
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