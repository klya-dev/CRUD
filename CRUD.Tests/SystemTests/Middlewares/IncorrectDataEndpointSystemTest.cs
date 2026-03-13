using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CRUD.Tests.SystemTests.Middlewares;

public class IncorrectDataEndpointSystemTest : IClassFixture<TestWebApplicationFactory>
{
    // В этом тесте я тестирую разные эндпоинты на возможные возникновения null и других некорректных данных. Я молодец и этого не допускаю
    // FromBody LoginDataDto
    // FromRoute string
    // FromQuery bool
    // FromForm IFormFile
    // Тобишь каждый атрибут, который я использую я протестил

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITokenManager _tokenManager;

    public IncorrectDataEndpointSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");

            // Если нужно переопределить настройку
            //var dict = new Dictionary<string, string>
            //{
            //    [$"{ProgramOptions.SectionName}:{nameof(ProgramOptions.SkipLogging)}"] = false.ToString(),
            //};
            //var configuration = new ConfigurationBuilder()
            //    .AddInMemoryCollection(dict)
            //    .Build();
            //builder.UseConfiguration(configuration);
            //builder.ConfigureAppConfiguration((ctx, config) =>
            //{
            //    config.AddInMemoryCollection(dict);
            //});
        }).CreateClient(); // Т.к Production может чуть иначе обрабатывать исключительные ситуации

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
    }


    // FromBody LoginDataDto

    [Fact]
    public async Task Post_Login_SerializeNullLoginData_ReturnsIncorrectRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Тело запроса
        LoginDataDto loginData = null;
        var json = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, Application.Json); // Вернёт "null"
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Post_Login_ContentIsNull_ReturnsIncorrectRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Тело запроса
        request.Content = null;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    public async Task Post_Login_EmptyStringContent_ReturnsUnsupportedMediaType(string content)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Тело запроса
        var json = new StringContent(content);
        request.Content = json;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, result.StatusCode);
        Assert.Equal(0, result.Content.Headers.ContentLength); // Нет контента
    }

    [Fact] // Правильные данные, но указан неверный ContentType - text/html
    public async Task Post_Login_WrongContentType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.AUTH_LOGIN_URL);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddBearerToken(request, _tokenManager);

        // Тело запроса
        var loginData = new LoginDataDto() { Username = "user", Password = "pass" };
        var json = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, Application.Json);
        request.Content = json;
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, result.StatusCode);
        Assert.Equal(0, result.Content.Headers.ContentLength); // Нет контента
    }


    // FromRoute string

    [Fact]
    public async Task Get_ConfirmationsEmail_EmptyString_ReturnsNotFound()
    {
        // Arrange
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, string.Empty); // /v1/confirmations/email/
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\t")]
    [InlineData(" ")]
    public async Task Get_ConfirmationsEmail_SpaceString_ReturnsNotFound(string token)
    {
        // Arrange
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, token);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact] // Спокойно принимается такая строка, но Bad Request т.к токен недействителен
    public async Task Get_ConfirmationsEmail_SpaceStringEncoded_ReturnsBadRequest()
    {
        // Arrange
        var url = string.Format(TestConstants.CONFIRMATIONS_EMAIL_TOKEN_URL, "%20");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");
        TestConstants.AddIdempotencyKey(request);

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Contains("Предоставленный токен недействителен", jsonDocument.RootElement.GetProperty("detail").GetString());
    }


    // Длина строки несоответствует

    [Theory] // Заданная длина - 6 (VerificationPhoneNumberRequestOptions.LengthCode)
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("1234567")]
    [InlineData("some")]
    public async Task Get_ConfirmationsPhone_WrongStringLenght_ReturnsNotFound(string code)
    {
        // Arrange
        var url = string.Format(TestConstants.CONFIRMATIONS_PHONE_CODE_URL, code);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }


    // Неверный GUID

    [Theory]
    [InlineData("")]
    public async Task Get_Publications_PublicationId_WrongGuid_ReturnsIncorrectRequest(string publicationId)
    {
        // Arrange
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationId);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.INCORRECT_REQUEST, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    // Если бы не было {publicationId:guid}, то вернуло бы IncorrectRequest 
    // И да, так лучше https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/routing?view=aspnetcore-9.0#how-to-address-this-issue
    [Theory]
    [InlineData("123-45")]
    [InlineData("some")]
    public async Task Get_Publications_PublicationId_WrongGuid_ReturnsNotFound(string publicationId)
    {
        // Arrange
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, publicationId);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept-Language", "ru");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    [Fact] // Пустой GUID
    public async Task Get_Publications_PublicationId_EmptyGuid_ReturnsEmptyGuid()
    {
        // Arrange
        var url = string.Format(TestConstants.PUBLICATIONS_PUBLICATION_ID_URL, Guid.Empty);
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal(ErrorCodes.EMPTY_UNIQUE_IDENTIFIER, jsonDocument.RootElement.GetProperty("code").GetString());
    }


    // FromQuery bool

    [Fact]
    public async Task Post_UserConfirmationPhone_None_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_CONFIRMATION_PHONE_URL);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Post_UserConfirmationPhone_BoolWithoutValue_ReturnsBadRequest()
    {
        // Arrange
        var url = TestConstants.USER_CONFIRMATION_PHONE_URL + "?isTelegram";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Accept-Language", "ru");

        // Авторизация
        TestConstants.AddBearerToken(request, _tokenManager);

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }


    // FromForm IFormFile

    [Fact]
    public async Task Post_UserAvatar_ContentIsEmptyData_ReturnsIncorrectRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        var content = new MultipartFormDataContent();
        request.Content = content;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Post_UserAvatar_ContentIsEmptyData_WithContentTypeHeader_ReturnsFileIsEmpty()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(""));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        request.Content = content;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Вы отправили пустой файл, пожалуйста выберите не пустой файл и повторите попытку.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.FILE_IS_EMPTY, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_UserAvatar_ContentIsEmptyData_WithoutContentTypeHeader_ReturnsFileIsEmpty()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(""));
        content.Add(fileContent, "file", "test.txt");

        request.Content = content;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Вы отправили пустой файл, пожалуйста выберите не пустой файл и повторите попытку.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.FILE_IS_EMPTY, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_UserAvatar_ContentIsEmptyData_ReturnsFileIsEmpty()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(""));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        request.Content = content;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Вы отправили пустой файл, пожалуйста выберите не пустой файл и повторите попытку.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.FILE_IS_EMPTY, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_UserAvatar_ReturnsDoesNotMatchSignature()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(File.ReadAllBytes(Path.Combine(TestHelper.GetProjectDirectoryPath(), "test_files", "NVtest2.bmp")));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(fileContent, "file", "test.txt");

        request.Content = content;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Файл имеет неверный формат. Пожалуйста, проверьте, что загружаемый файл соответствует необходимым требованиям и форматам.", jsonDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(ErrorCodes.DOES_NOT_MATCH_SIGNATURE, jsonDocument.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_UserAvatar_NotMultipartContentType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        request.Content = new StringContent("content");

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);
    }

    // ТЕСТА НЕТ
    // По идеи большие файлы (более 30 МБ) возвращают 413 статус (слишком большой файл), даже не заходя в конечную точку, т.к у Kestrel по умолчанию ограничение в 30 МБ
    // Чтобы проверить вручную можно врубить уровень логирования Debug в appsettings.json, и там всё описанно будет +Environment.SetEnvironmentVariable(ProgramConfigures.SKIP_LOGGING, "0");
    // Я не смог воспроизвести эту ситуацию в тесте, по какой-то причине в тест возвращается BadRequest (а в исключении указано, чтобы и вправду ограничение в 30 МБ)
    // Но в Swagger или Bruno приходит пустой 413 статус, как и задумано. +в тесте почему-то лимит в 30 МБ странно работает, по факту почему-то применяется другой лимит в 128 МБ, который относится к другой настройке
    // ГПТ говорит: Тесты с TestServer обходят часть HTTP стека

    [Fact]
    public async Task Post_UserAvatar_ContentIsNull_ReturnsIncorrectRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, TestConstants.USER_AVATAR_URL);
        request.Headers.Add("Accept-Language", "ru");

        TestConstants.AddBearerToken(request, _tokenManager);

        // Контент
        request.Content = null;

        // Act
        using var result = await _client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);

        // Читаем содержимое ответа
        await using var contentStream = await result.Content.ReadAsStreamAsync();
        using var jsonDocument = await JsonDocument.ParseAsync(contentStream);

        Assert.Equal("Отправленный запрос некорректен, проверьте сигнатуру эндпоинта.", jsonDocument.RootElement.GetProperty("detail").GetString());
    }
}