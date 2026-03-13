#nullable disable
using Microsoft.AspNetCore.Mvc.Testing;

namespace CRUD.Tests.IntegrationTests;

public class AvatarManagerIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

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

    private IAvatarManager GenerateNewAvatarManager()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IAvatarManager>();
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Тестовая аватарка
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Дефолтная аватарка
    public async Task GetAvatarAsync_ReturnsStreamAndFileExtension(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Act
        var result = await _avatarManager.GetAvatarAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        Assert.NotNull(result.Value.Stream);
        Assert.True(result.Value.Stream.Length > 0);
        AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.FileExtension);
    }

    [Fact]
    public async Task GetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _avatarManager.GetAvatarAsync(userId);

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
        var user = await DI.CreateUserAsync(_db, avatarUrl: "NONE");

        // Act
        var result = await _avatarManager.GetAvatarAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Value.Stream);
        Assert.Null(result.Value.FileExtension);

        Assert.Contains(ErrorMessages.FileNotFound, result.ErrorMessage);
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас такая же, тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_ReturnsServiceResult(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        var result = await _avatarManager.SetAvatarAsync(user.Id, memStream);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду обновилась, а прошлая удалилась, если не дефолтная
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.NotEqual(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);

        if (userFromDbBeforeUpdate.AvatarURL != TestConstants.DefaultAvatarPath)
            Assert.False(await _s3Manager.IsObjectExistsAsync(userFromDbBeforeUpdate.AvatarURL));

        // Удаляем за собой
        if (userFromDbAfterUpdate.AvatarURL != $"{TestConstants.TEST_FILES_PATH}/test.png")
            await _s3Manager.DeleteObjectAsync(userFromDbAfterUpdate.AvatarURL);

        // AvatarManager удалит $"{TestConstants.TEST_FILES_PATH}/test.png", а мы восстановим
        if (userFromDbBeforeUpdate.AvatarURL == $"{TestConstants.TEST_FILES_PATH}/test.png")
        {
            using var stream2 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test2.png")).Value;
            using MemoryStream memStream2 = new MemoryStream();
            stream2.CopyTo(memStream2);
            memStream2.Seek(0, SeekOrigin.Begin);

            await _s3Manager.CreateObjectAsync(memStream2, $"{TestConstants.TEST_FILES_PATH}/test.png");
        }
    }

    [Theory] // Перед записью в базу выбросится исключение, о том, что User невалидный
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас такая же, тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_ThrowsInvalidOperationException_NotValidBeforeUpdate(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, role: "НЕВАЛИДНАЯ РОЛЬ");

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Результат валидации (о том, что роль невалидна)
        var validationResult = await new UserValidator().ValidateAsync(user);
        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        Func<Task> a = async () =>
        {
            await _avatarManager.SetAvatarAsync(user.Id, memStream);
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(a);

        // Assert
        Assert.Contains(ErrorMessages.ModelIsNotValid(nameof(User), validationResult.Errors), ex.Message);

        // Пользователь и аватарка и вправду не обновились, а прошлая аватарка удалилась, если не дефолтная
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equivalent(userFromDbBeforeUpdate, userFromDbAfterUpdate);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_WhenDoesNotMatchSignature_ReturnsErrorMessage_DoesNotMatchSignature(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp, пустой файл)
        string[] files = ["NVtest2.bmp", "NVtest3.png", "NVtest4.png"];
        foreach (var item in files)
        {
            using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/{item}")).Value;
            using MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

            // Act
            var result = await _avatarManager.SetAvatarAsync(user.Id, memStream);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(ErrorMessages.DoesNotMatchSignature, result.ErrorMessage);

            // Аватарка и вправду не обновилась
            var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
            Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
        }
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_WhenFileSizeLimitExceeded_ReturnsErrorMessage_FileSizeLimitExceeded(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _avatarManager.SetAvatarAsync(user.Id, memStream);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.FileSizeLimitExceeded, result.ErrorMessage);
    }

    [Fact]
    public async Task SetAvatarAsync_WhenUserNotFound_ReturnsErrorMessage_UserNotFound()
    {
        // Arrange
        var userIdGuid = Guid.NewGuid();

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        // Act
        var result = await _avatarManager.SetAvatarAsync(userIdGuid, memStream);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(ErrorMessages.UserNotFound, result.ErrorMessage);
    }


    [Fact] // Не дефолтная аватарка удалится
    public async Task DeleteAvatarAsync_WhenNotDefaultAvatar_ShouldDelete()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Устанавливаем ему не дефолтную аватарку
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        await _avatarManager.SetAvatarAsync(user.Id, memStream);

        // Act
        var result = await _avatarManager.DeleteAvatarAsync(user.AvatarURL);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду удалилась
        Assert.False(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }

    [Fact] // Дефолтная аватарка не удалится
    public async Task DeleteAvatarAsync_WhenDefaultAvatar_ShouldNotDelete()
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Act
        var result = await _avatarManager.DeleteAvatarAsync(user.AvatarURL);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ErrorMessage);

        // Аватарка и вправду не удалилась
        Assert.True(await _s3Manager.IsObjectExistsAsync(user.AvatarURL));
    }


    // Конфликты параллельности


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Тестовая аватарка
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Дефолтная аватарка
    public async Task GetAvatarAsync_ConcurrencyConflict_ReturnsErrorMessage_Nothing(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        var avatarManager = GenerateNewAvatarManager();
        var avatarManager2 = GenerateNewAvatarManager();

        // Act
        var task = avatarManager.GetAvatarAsync(user.Id);
        var task2 = avatarManager2.GetAvatarAsync(user.Id);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Null(result.ErrorMessage);

            Assert.NotNull(result.Value.Stream);
            Assert.True(result.Value.Stream.Length > 0);
            AssertExtensions.IsNotNullOrNotWhiteSpace(result.Value.FileExtension);
        }
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас такая же, тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас дефолтная)
    public async Task SetAvatarAsync_ConcurrencyConflict_ReturnsErrorMessage_NothingOrConflict(string currentAvatar)
    {
        // Arrange
        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        var avatarManager = GenerateNewAvatarManager();
        var avatarManager2 = GenerateNewAvatarManager();

        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream(); // https://stackoverflow.com/questions/42861816/copy-between-two-aws-buckets-stream-returned-from-aws-hashstream-does-not-sup
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        using var stream2 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream2 = new MemoryStream();
        stream2.CopyTo(memStream2);
        memStream2.Seek(0, SeekOrigin.Begin);

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        var task = avatarManager.SetAvatarAsync(user.Id, memStream);
        var task2 = avatarManager2.SetAvatarAsync(user.Id, memStream2);

        // Впервые в истории, сервис сам обрабатывает конфликт параллельности, try catch не нужен
        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Либо ничего, либо конфликт параллельности
            var errorMessage = result.ErrorMessage;
            string[] allowedErrors =
            [
                null,
                ErrorMessages.ConcurrencyConflicts
            ];

            Assert.Contains(errorMessage, allowedErrors);
        }

        // Аватарка и вправду обновилась, а прошлая удалилась, если не дефолтная
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.NotEqual(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);

        if (userFromDbBeforeUpdate.AvatarURL != TestConstants.DefaultAvatarPath)
            Assert.False(await _s3Manager.IsObjectExistsAsync(userFromDbBeforeUpdate.AvatarURL));

        // Удаляем за собой
        if (userFromDbAfterUpdate.AvatarURL != $"{TestConstants.TEST_FILES_PATH}/test.png")
            await _s3Manager.DeleteObjectAsync(userFromDbAfterUpdate.AvatarURL);

        // AvatarManager удалит $"{TestConstants.TEST_FILES_PATH}/test.png", а мы восстановим
        if (!await _s3Manager.IsObjectExistsAsync($"{TestConstants.TEST_FILES_PATH}/test.png"))
        {
            using var stream3 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test2.png")).Value;
            using MemoryStream memStream3 = new MemoryStream();
            stream3.CopyTo(memStream3);
            memStream3.Seek(0, SeekOrigin.Begin);

            await _s3Manager.CreateObjectAsync(memStream3, $"{TestConstants.TEST_FILES_PATH}/test.png");
        }
    }
}