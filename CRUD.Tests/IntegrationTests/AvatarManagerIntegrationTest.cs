using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class AvatarManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // Перед запуском нужно убедиться, что все тестовые файлы из папки "test_files" загружены на S3

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IAvatarManager _avatarManager;
    private readonly IS3Manager _s3Manager;
    private readonly ApplicationDbContext _db;

    public AvatarManagerIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _avatarManager = scopedServices.GetRequiredService<IAvatarManager>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Тестовая аватарка
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Дефолтная аватарка
    public async Task GetAvatarAsync_ReturnsStreamAndFileExtension(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.GetAvatarAsync(user.Id, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value.Stream);
        Assert.True(result.Value.Stream.Length > 0);
        Assert.Empty(result.Value.FileExtension);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _avatarManager.GetAvatarAsync(userId, ct: TestContext.Current.CancellationToken);

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
        var user = await DI.CreateUserAsync(_db, avatarUrl: "NONE", ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.GetAvatarAsync(user.Id, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value.Stream);
        Assert.Null(result.Value.FileExtension);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Тестовая аватарка
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Дефолтная аватарка
    public async Task GetPresignedUrlAvatarAsync_ReturnsUrl(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(user.Id, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value);
    }

    [Fact]
    public async Task GetPresignedUrlAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(userId, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value);

        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }

    [Fact]
    public async Task GetPresignedUrlAvatarAsync_WhenFileNotFound_ReturnsUrl()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "NONE", ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.GetPresignedUrlAvatarAsync(user.Id, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value);
    }


    [Fact] // У пользователя сейчас тестовая аватарка, будем устанавливать любую (путь не поменяется)
    public async Task SetAvatarAsync_WhenAvatarUserIsNotDefaultAvatar_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken);

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.SetAvatarAsync(user.Id, memStream, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Путь до аватарки не изменился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
    }

    [Fact] // У пользователя дефолтная аватарка, будем устанавливать тестовую (путь поменяется)
    public async Task SetAvatarAsync_WhenAvatarUserIsDefaultAvatar_ReturnsServiceResult()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: TestConstants.DefaultAvatarPath, ct: TestContext.Current.CancellationToken);

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.SetAvatarAsync(user.Id, memStream, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Путь до аватарки изменился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(userFromDbAfterUpdate.AvatarURL, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_WhenDoesNotMatchSignature_ReturnsErrorMessage_DoesNotMatchSignature(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp, пустой файл)
        string[] files = ["NVtest2.bmp", "NVtest3.png", "NVtest4.png"];
        foreach (var item in files)
        {
            using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/{item}", ct: TestContext.Current.CancellationToken)).Value.Stream;
            using MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

            // Act
            var result = await _avatarManager.SetAvatarAsync(user.Id, memStream, ct: TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(ErrorMessages.DoesNotMatchSignature, result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_WhenFileSizeLimitExceeded_ReturnsErrorMessage_FileSizeLimitExceeded(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _avatarManager.SetAvatarAsync(user.Id, memStream, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileSizeLimitExceeded, result.ErrorMessage);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, memStream, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact] // Не дефолтная аватарка удалится
    public async Task DeleteAvatarAsync_WhenNotDefaultAvatar_ShouldDelete()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream, TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.DeleteAvatarAsync(user.AvatarURL, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }

    [Fact] // Дефолтная аватарка не удалится
    public async Task DeleteAvatarAsync_WhenDefaultAvatar_ShouldNotDelete()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Act
        var result = await _avatarManager.DeleteAvatarAsync(user.AvatarURL, ct: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду не удалилась
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL, TestContext.Current.CancellationToken));
    }
}