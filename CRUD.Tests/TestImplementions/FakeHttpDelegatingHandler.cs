namespace CRUD.Tests.TestImplementions;

/// <summary>
/// Тестовая реализация <see cref="DelegatingHandler"/>.
/// </summary>
public class FakeHttpDelegatingHandler : DelegatingHandler
{
    private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _responseFactory;
    public int Attempts { get; private set; }

    public FakeHttpDelegatingHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    {
        _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _responseFactory.Invoke(++Attempts, cancellationToken);
    }
}