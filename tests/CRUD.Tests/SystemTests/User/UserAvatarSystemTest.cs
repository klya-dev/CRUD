using System.Net.Http.Headers;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.User;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class UserAvatarSystemTest : IClassFixture<TestWebApplicationFactory>
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
    public async Task Get_File_ReturnsFileStream()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_FILE_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/octet-stream", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);

        Assert.True(contentStream.Length > 0);
    }

    [Fact]
    public async Task Get_File_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_FILE_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_File_ReturnsFileNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something", ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_FILE_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.FILE_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    [Fact]
    public async Task Get_Url_ReturnsUrl()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var contentString = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(contentString);
    }

    [Fact]
    public async Task Get_Url_ReturnsUserNotFound()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL_URL);
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_Url_WhenFileNotExists_ReturnsUrl()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: "something", ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.USER_AVATAR_URL_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        var contentString = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        AssertExtensions.IsNotNullOrNotWhiteSpace(contentString);
    }


    [Fact] // У пользователя сейчас тестовая аватарка, будем устанавливать любую (путь не поменяется)
    public async Task Post_WhenAvatarUserIsNotDefaultAvatar_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Путь до аватарки не изменился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
    }

    [Fact] // У пользователя дефолтная аватарка, будем устанавливать тестовую (путь поменяется)
    public async Task Post_WhenAvatarUserIsDefaultAvatar_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: TestConstants.DefaultAvatarPath, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        // Путь до аватарки изменился
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.NotEqual(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);

        // Удаляем за собой
        await _s3Manager.DeleteObjectAsync(userFromDbAfterUpdate.AvatarURL, ct: TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас тестовая)
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))] // Пользователю, устанавливаем тестовую, невалидную аватарку (сейчас дефолтная)
    public async Task Post_WhenDoesNotMatchSignature_ReturnsDoesNotMatchSignature(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        // Одинаковый результат для трёх случаев (bmp, png, который на самом деле bmp)
        string[] files = ["NVtest2.bmp", "NVtest3.png"];
        foreach (var item in files)
        {
            // Запрос
            var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
            TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

            // Контент
            using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/{item}", ct: TestContext.Current.CancellationToken)).Value.Stream;
            using MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(memStream.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(fileContent, "file", "test.png"); // Название обязательно file
            request.Content = content;

            var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

            // Act
            using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

            // Читаем содержимое ответа
            await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
            using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(ErrorCodes.DOES_NOT_MATCH_SIGNATURE, jsonDocument.RootElement.GetProperty("code").GetString());

            // Аватарка и вправду не обновилась
            var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
            Assert.Equal(userFromDbAfterUpdate.AvatarURL, userFromDbBeforeUpdate.AvatarURL);
        }
    }

    [Fact]
    public async Task Post_WhenEmptyFile_ReturnsFileIsEmpty()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{_avatarManagerOptions.AvatarsInS3Directory}/test.png", ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest4.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.FILE_IS_EMPTY, jsonDocument.RootElement.GetProperty("code").GetString());

        // Аватарка и вправду не обновилась
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
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
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar, ct: TestContext.Current.CancellationToken);

        // Запрос
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        TestConstants.AddBearerToken(request, _tokenManager, userId: user.Id.ToString());

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        var userFromDbBeforeUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.RequestEntityTooLarge, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.FILE_SIZE_LIMIT_EXCEEDED, jsonDocument.RootElement.GetProperty("code").GetString());

        // Аватарка и вправду не обновилась
        var userFromDbAfterUpdate = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.Id, TestContext.Current.CancellationToken);
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
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png", ct: TestContext.Current.CancellationToken)).Value.Stream;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png"); // Название обязательно file
        request.Content = content;

        // Act
        using var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(ErrorCodes.USER_NOT_FOUND, jsonDocument.RootElement.GetProperty("code").GetString());
    }
}