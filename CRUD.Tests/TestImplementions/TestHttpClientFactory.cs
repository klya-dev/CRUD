namespace CRUD.Tests.TestImplementions;

/// <summary>
/// Тестовая реализация <see cref="IHttpClientFactory"/>.
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}