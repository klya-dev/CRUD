namespace CRUD.Tests.TestImplementions;

/// <summary>
/// Тестовая реализация <see cref="IHttpClientFactory"/>.
/// </summary>
public sealed class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}