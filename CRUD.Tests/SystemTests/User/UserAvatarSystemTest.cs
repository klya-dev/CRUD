using System.Net.Http.Headers;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

public class UserAvatarSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IS3Manager _s3Manager;
    private readonly AvatarManagerOptions _avatarManagerOptions;

    public UserAvatarSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
        _avatarManagerOptions = scopedServices.GetRequiredService<IOptions<AvatarManagerOptions>>().Value;
    }

    [Fact]
    public async Task Get_ReturnsFileStream()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/octet-stream", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();

        Assert.True(contentStream.Length > 0);
    }

    [Fact]
    public async Task Get_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_ReturnsFileNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.FILE_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас такая же, тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас дефолтная)
    public async Task Post_ReturnsNoContent(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

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

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task Post_WhenDoesNotMatchSignature_ReturnsDoesNotMatchSignature(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp)
        string[] files = ["NVtest2.bmp", "NVtest3.png"];
        foreach (var item in files)
        {
            // Запрос
            var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
            TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

            // Контент
            using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/{item}")).Value;
            using MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(memStream.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(fileContent, "file", "test.png"); // Название обязательно file
            request.Content = content;

            var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

            // Act
            using var result = await client.SendAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            Assert.Equal(ErrorCodes.DOES_NOT_MATCH_SIGNATURE, jsonDocument.RootElement.GetProperty("code").GetString());

            // Аватарка и вправду не обновилась
            var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
            Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
        }
    }

    [Fact]
    public async Task Post_WhenEmptyFile_ReturnsFileIsEmpty()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{_avatarManagerOptions.AvatarsInS3Directory}/test.png");

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest4.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.FILE_IS_EMPTY, jsonDocument.RootElement.GetProperty("code").GetString());

        // Аватарка и вправду не обновилась
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task Post_WhenFileSizeLimitExceeded_ReturnsFileSizeLimitExceeded(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.FILE_SIZE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());

        // Аватарка и вправду не обновилась
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);
        Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
    }

    [Fact]
    public async Task Post_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // Конфликты параллельности


    [Fact]
    public async Task Get_ConcurrencyConflict_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.OK == result.StatusCode)
            {
                Assert.Equal("application/octet-stream", result.Content.Headers.ContentType?.MediaType);
                Assert.True(contentStream.Length > 0);
                continue;
            }
        }
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас такая же, тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, валидную аватарку (сейчас дефолтная)
    public async Task Post_ConcurrencyConflict_ReturnsNoContentOrConflict(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;
        var client2 = _factory.CreateClient();

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Запрос 1
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Запрос 2
        var request2 = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request2, _tokenManager, userId: user.Id.ToString());

        // Контент 1
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        // Контент 2
        using var stream2 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream2 = new MemoryStream();
        stream2.CopyTo(memStream2);
        memStream2.Seek(0, SeekOrigin.Begin);

        var content2 = new MultipartFormDataContent();
        var fileContent2 = new ByteArrayContent(memStream2.ToArray());
        fileContent2.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content2.Add(fileContent2, "file", "test.png"); // Название обязательно file
        request2.Content = content2;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id);

        // Act
        using var task = client.SendAsync(request);
        using var task2 = client2.SendAsync(request2);

        var results = await Task.WhenAll(task, task2);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);

            // Ошибка сервера
            if (System.Net.HttpStatusCode.InternalServerError == result.StatusCode)
                Assert.Fail("InternalServerError");

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync();

            // Может быть успешный ответ
            if (System.Net.HttpStatusCode.NoContent == result.StatusCode)
            {
                Assert.Null(result.Content.Headers.ContentType);
                continue;
            }

            using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

            // Может быть неуспешный ответ
            if (!result.IsSuccessStatusCode)
            {
                // Конфликт параллельности
                var errorCode = jsonDocument.RootElement.GetProperty("code").GetString();
                string[] allowedErrors =
                [
                    ErrorCodes.CONCURRENCY_CONFLICTS
                ];

                Assert.Contains(errorCode, allowedErrors);
            }
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
        if (userFromDbBeforeUpdate.AvatarURL == $"{TestConstants.TEST_FILES_PATH}/test.png")
        {
            using var stream3 = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test2.png")).Value;
            using MemoryStream memStream3 = new MemoryStream();
            stream3.CopyTo(memStream3);
            memStream3.Seek(0, SeekOrigin.Begin);

            await _s3Manager.CreateObjectAsync(memStream3, $"{TestConstants.TEST_FILES_PATH}/test.png");
        }
    }
}