using Grpc.Core;
using Grpc.Net.Client;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microservice.EmailSender.Tests.IntegrationTests;

public class GrpcEmailSenderServiceIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IQueueEmail _queueEmail;

    public GrpcEmailSenderServiceIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _queueEmail = scopedServices.GetRequiredService<IQueueEmail>();
    }

    [Fact]
    public async Task Enqueue_ReturnsEnqueueLetterReply()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        // MeterProvider
        var exportedItems = new List<Metric>();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
           .AddEventCountersInstrumentation(options =>
           {
               options.AddEventSources("Grpc.AspNetCore.Server");
           })
           .AddInMemoryExporter(exportedItems, metricReaderOptions =>
           {
               metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
           })
           .Build();

        var request = new EnqueueLetterRequest
        { 
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {TokenManager.GenerateEmailSenderAuthToken()}");

        // Подключаемся к серверу и получаем клиента
        var serverClient = _factory.CreateClient();
        using var channel = GrpcChannel.ForAddress(serverClient.BaseAddress, new GrpcChannelOptions()
        {
            HttpClient = serverClient
        });
        var client = new GrpcEmailSender.GrpcEmailSenderClient(channel);

        // Act
        var result = await client.EnqueueAsync(request, headers);

        // Assert
        Assert.NotNull(result);

        _queueEmail.TryDequeue(out var letter);
        Assert.NotNull(letter);

        // Метрика добавилась
        // Ждем сбора метрик не больше 10 секунд
        for (int i = 0; i < 10 && exportedItems.Count <= 0; i++)
            await Task.Delay(1000);
        meterProvider.ForceFlush();

        // total-calls = 1
        var metricPoints = new List<MetricPoint>();
        foreach (ref readonly var point in exportedItems.First(x => x.Name == "ec.Grpc.AspNetCore.Server.total-calls").GetMetricPoints())
            metricPoints.Add(point);
        var totalCallsValue = metricPoints.Last().GetGaugeLastValueDouble();
        Assert.Equal(1, totalCallsValue);

        // calls-failed = 0
        metricPoints.Clear();
        foreach (ref readonly var point in exportedItems.First(x => x.Name == "ec.Grpc.AspNetCore.Server.calls-failed").GetMetricPoints())
            metricPoints.Add(point);
        var failedCallsValue = metricPoints.Last().GetGaugeLastValueDouble();
        Assert.Equal(0, failedCallsValue);
    }

    [Fact]
    public async Task Enqueue_WithoutBearerToken_ThrowsRpcExceptionStatusUnauthenticated()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        // Подключаемся к серверу и получаем клиента
        var serverClient = _factory.CreateClient();
        using var channel = GrpcChannel.ForAddress(serverClient.BaseAddress, new GrpcChannelOptions()
        {
            HttpClient = serverClient
        });
        var client = new GrpcEmailSender.GrpcEmailSenderClient(channel);

        // Act
        Func<Task> a = async () =>
        {
            await client.EnqueueAsync(request);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<RpcException>(a);
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);

        _queueEmail.TryDequeue(out var letter);
        Assert.Null(letter);
    }

    [Fact]
    public async Task Enqueue_WithoutLetterId_ThrowsRpcExceptionStatusInvalidArgument()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EnqueueLetterRequest
        {
            // Без Id
            Email = email,
            Subject = subject,
            Body = body
        };

        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {TokenManager.GenerateEmailSenderAuthToken()}");

        // Подключаемся к серверу и получаем клиента
        var serverClient = _factory.CreateClient();
        using var channel = GrpcChannel.ForAddress(serverClient.BaseAddress, new GrpcChannelOptions()
        {
            HttpClient = serverClient
        });
        var client = new GrpcEmailSender.GrpcEmailSenderClient(channel);

        // Act
        Func<Task> a = async () =>
        {
            await client.EnqueueAsync(request, headers);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<RpcException>(a);
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);

        _queueEmail.TryDequeue(out var letter);
        Assert.Null(letter);
    }

    [Fact]
    public async Task Enqueue_Mock_ThrowsException_ThrowsRpcExceptionStatusInternal()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {TokenManager.GenerateEmailSenderAuthToken()}");

        // IQueueEmail выбросит исключение
        var mockQueueEmail = new Mock<IQueueEmail>();
        mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
        var serverClient = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockQueueEmail.Object);
            });
        }).CreateClient();

        // Подключаемся к серверу и получаем клиента
        using var channel = GrpcChannel.ForAddress(serverClient.BaseAddress, new GrpcChannelOptions()
        {
            HttpClient = serverClient
        });
        var client = new GrpcEmailSender.GrpcEmailSenderClient(channel);

        // Act
        Func<Task> a = async () =>
        {
            await client.EnqueueAsync(request, headers);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<RpcException>(a);
        Assert.Equal(StatusCode.Internal, ex.StatusCode);

        _queueEmail.TryDequeue(out var letter);
        Assert.Null(letter);
    }

    [Fact]
    public async Task Enqueue_Mock_ThrowsTaskCanceledException_ThrowsRpcExceptionStatusCancelled()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {TokenManager.GenerateEmailSenderAuthToken()}");

        // IQueueEmail выбросит исключение
        var mockQueueEmail = new Mock<IQueueEmail>();
        mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TaskCanceledException());
        var serverClient = _factory.WithWebHostBuilder(configuration =>
        {
            configuration.ConfigureTestServices(services =>
            {
                services.AddSingleton(_ => mockQueueEmail.Object);
            });
        }).CreateClient();

        // Подключаемся к серверу и получаем клиента
        using var channel = GrpcChannel.ForAddress(serverClient.BaseAddress, new GrpcChannelOptions()
        {
            HttpClient = serverClient
        });
        var client = new GrpcEmailSender.GrpcEmailSenderClient(channel);

        // Act
        Func<Task> a = async () =>
        {
            await client.EnqueueAsync(request, headers);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<RpcException>(a);
        Assert.Equal(StatusCode.Cancelled, ex.StatusCode);

        _queueEmail.TryDequeue(out var letter);
        Assert.Null(letter);
    }

    [Fact] // Исключение на стороне клиента, т.к сообщение grpc в данном случае не допускает null
    public async Task Enqueue_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string email = null;
        string subject = "test";
        string body = "test";

        EmailSender.EnqueueLetterRequest request = null;

        // Act
        // Не можем даже создать запрос, если null данные
        Func<Task> a = async () =>
        {
            request = new EmailSender.EnqueueLetterRequest
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Subject = subject,
                Body = body
            };
        };

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(a);

        _queueEmail.TryDequeue(out var letter);
        Assert.Null(letter);
    }
}