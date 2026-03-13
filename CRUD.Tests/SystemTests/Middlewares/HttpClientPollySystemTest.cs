using CRUD.Tests.TestImplementions;
using Polly;
using Polly.Timeout;
using System.Net;

namespace CRUD.Tests.SystemTests.Middlewares;

public class HttpClientPollySystemTest
{
    [Fact] // Политика WaitAndRetry - в сумме 4 попытки
    public async Task WaitAndRetryAsync_Mock_GatewayTimeout_ReturnsAttempts_FourTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var fakeHttpDelegatingHandler = new FakeHttpDelegatingHandler((attempt, cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.GatewayTimeout)));
        services.AddHttpClient("test-httpclient", client =>
        {
            client.BaseAddress = new Uri("http://any.localhost");
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(100)))
            .AddHttpMessageHandler(() => fakeHttpDelegatingHandler);
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test-httpclient");
        var request = new HttpRequestMessage(HttpMethod.Get, "/any");

        // Act
        var result = await sut.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.GatewayTimeout, result.StatusCode);
        Assert.Equal(4, fakeHttpDelegatingHandler.Attempts); // 1 обычный вызов и 3 повторные попытки
    }

    [Fact] // Политика Timeout для Get - вернётся исключение TimeoutRejectedException
    public async Task TimeoutAsync_Mock_Get_GatewayTimeout_ReturnsTimeoutRejectedException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Симулируем медленный ответ (больше таймаута)
        var fakeHttpDelegatingHandler = new FakeHttpDelegatingHandler(async (attempt, cancellationToken) =>
        {
            await Task.Delay(200, cancellationToken); // Задержка 200 мс > таймаута 100 мс
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
        });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(100));
        var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(300));
        services.AddHttpClient("test-httpclient", client =>
        {
            client.BaseAddress = new Uri("http://any.localhost");
        })
            .AddPolicyHandler(httpRequestMessage => httpRequestMessage.Method == HttpMethod.Get ? timeoutPolicy : longTimeoutPolicy)
            .AddHttpMessageHandler(() => fakeHttpDelegatingHandler);
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test-httpclient");
        var request = new HttpRequestMessage(HttpMethod.Get, "/any");

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(() => sut.SendAsync(request));
    }

    [Fact] // Политика Timeout для Post - вернётся Ok (т.к задержка меньше таймаута)
    public async Task TimeoutAsync_Mock_Post_GatewayTimeout_ReturnsOk()
    {
        // Arrange
        var services = new ServiceCollection();

        // Симулируем медленный ответ (больше таймаута)
        var fakeHttpDelegatingHandler = new FakeHttpDelegatingHandler(async (attempt, cancellationToken) =>
        {
            await Task.Delay(200, cancellationToken); // Задержка 200 мс < таймаута 300 мс
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(100));
        var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(300));
        services.AddHttpClient("test-httpclient", client =>
        {
            client.BaseAddress = new Uri("http://any.localhost");
        })
            .AddPolicyHandler(httpRequestMessage => httpRequestMessage.Method == HttpMethod.Get ? timeoutPolicy : longTimeoutPolicy)
            .AddHttpMessageHandler(() => fakeHttpDelegatingHandler);
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test-httpclient");
        var request = new HttpRequestMessage(HttpMethod.Post, "/any");

        // Act
        var result = await sut.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(1, fakeHttpDelegatingHandler.Attempts);
    }

    [Fact] // Политика Timeout для Post - вернётся исключение TimeoutRejectedException (т.к задержка больше таймаута)
    public async Task TimeoutAsync_Mock_Post_GatewayTimeout_ReturnsTimeoutRejectedException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Симулируем медленный ответ (больше таймаута)
        var fakeHttpDelegatingHandler = new FakeHttpDelegatingHandler(async (attempt, cancellationToken) =>
        {
            await Task.Delay(500, cancellationToken); // Задержка 500 мс > таймаута 300 мс
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
        });

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(100));
        var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(300));
        services.AddHttpClient("test-httpclient", client =>
        {
            client.BaseAddress = new Uri("http://any.localhost");
        })
            .AddPolicyHandler(httpRequestMessage => httpRequestMessage.Method == HttpMethod.Get ? timeoutPolicy : longTimeoutPolicy)
            .AddHttpMessageHandler(() => fakeHttpDelegatingHandler);
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("test-httpclient");
        var request = new HttpRequestMessage(HttpMethod.Post, "/any");

        // Act
        await Assert.ThrowsAsync<TimeoutRejectedException>(() => sut.SendAsync(request));
    }
}