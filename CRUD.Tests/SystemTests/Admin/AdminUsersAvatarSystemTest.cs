using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CRUD.Tests.SystemTests.Admin;

public class AdminUsersAvatarSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ApplicationDbContext _db;
    private readonly ITokenManager _tokenManager;
    private readonly IS3Manager _s3Manager;

    public AdminUsersAvatarSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _db = scopedServices.GetRequiredService<ApplicationDbContext>();
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _s3Manager = scopedServices.GetRequiredService<IS3Manager>();
    }

    [Theory]
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))]
    public async Task Post_ReturnsNoContent(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_AVATAR_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png");
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
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))]
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
            var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_AVATAR_URL, user.Id);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

            // Контент
            using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/{item}")).Value;
            using MemoryStream memStream = new MemoryStream();
            stream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(memStream.ToArray());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            content.Add(fileContent, "file", "test.png");
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
    public async Task Post_WhenFileEmpty_ReturnsFileIsEmpty()
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: $"{TestConstants.TEST_FILES_PATH}/test.png");

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_AVATAR_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest4.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png");
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
    [InlineData($"{TestConstants.TEST_FILES_PATH}/test.png")]
    [MemberData(nameof(TestConstants.DefaultAvatarPathObject), MemberType = typeof(TestConstants))]
    public async Task Post_WhenFileSizeLimitExceeded_ReturnsFileSizeLimitExceeded(string currentAvatar)
    {
        // Arrange
        var client = _factory.HttpClient;

        // Добавляем пользователя в базу
        var user = await DI.CreateUserAsync(_db, avatarUrl: currentAvatar);

        // Запрос
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_AVATAR_URL, user.Id);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/NVtest.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png");
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
        var url = string.Format(TestConstants.ADMIN_USERS_USER_ID_AVATAR_URL, Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        TestConstants.AddBearerToken(request, _tokenManager, role: UserRoles.Admin);

        // Контент
        using var stream = (await _s3Manager.GetObjectAsync($"{TestConstants.TEST_FILES_PATH}/test.png")).Value;
        using MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(memStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", "test.png");
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
}