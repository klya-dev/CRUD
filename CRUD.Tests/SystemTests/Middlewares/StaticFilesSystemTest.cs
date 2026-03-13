using System.Net;

namespace CRUD.Tests.SystemTests.Middlewares;

public class StaticFilesSystemTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StaticFilesSystemTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact] // Вернёт text/html
    public async Task Get_Public_ReturnsOk()
    {
        var client = _factory.HttpClient;

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.PUBLIC_URL);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("text/html", result.Content.Headers.ContentType?.MediaType);
    }

    [Fact] // Вернёт текст
    public async Task Get_Public_ReadMe_ReturnsOk()
    {
        var client = _factory.HttpClient;

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.PUBLIC_README_URL);

        // Act
        using var result = await client.SendAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("text/plain", result.Content.Headers.ContentType?.MediaType);
        Assert.Contains("text/plain; charset=utf-8", result.Content.Headers.NonValidated.First(x => x.Key == "Content-Type").Value);
        Assert.Equal(TimeSpan.FromDays(7), result.Headers.CacheControl?.MaxAge); // Неделя
        Assert.True(result.Headers.CacheControl?.Public); // Неделя
    }

    [Fact] // default.png вернёт NotFound т.к его нельзя получить клиенту
    public async Task Get_Default_Png_ReturnsNotFound()
    {
        var client = _factory.HttpClient;

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, TestConstants.PUBLIC_URL + "default.png");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "default.png");

        // Act
        using var result = await client.SendAsync(request);
        using var result2 = await client.SendAsync(request2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Null(result.Content.Headers.ContentType);

        Assert.NotNull(result2);
        Assert.Equal(HttpStatusCode.NotFound, result2.StatusCode);
        Assert.Null(result2.Content.Headers.ContentType);
    }
}