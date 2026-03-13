#nullable disable

using CRUD.Utility.Options;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace CRUD.Tests.IntegrationTests;

public class QueueEmailIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IQueueEmail _queueEmail;

    public QueueEmailIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _queueEmail = scopedServices.GetRequiredService<IQueueEmail>();
    }

    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task Enqueue_ReturnsOk(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(email, subject, body);

        // MeterProvider
        var exportedItems = new List<Metric>();
        using var meterProvider = Sdk.CreateMeterProviderBuilder()
           .AddEventCountersInstrumentation(options =>
           {
               options.AddEventSources("Grpc.Net.Client");
           })
           .AddInMemoryExporter(exportedItems, metricReaderOptions =>
           {
               metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
           })
           .Build();

        // Act
        var result = await _queueEmail.EnqueueAsync(letter);

        // Assert
        Assert.Equal(StatusCode.OK, result);

        // Метрика добавилась
        // Ждем сбора метрик не больше 10 секунд
        for (int i = 0; i < 10 && exportedItems.Count <= 0; i++)
            await Task.Delay(1000);
        meterProvider.ForceFlush();

        // total-calls = 1
        var metricPoints = new List<MetricPoint>();
        foreach (ref readonly var point in exportedItems.First(x => x.Name == "ec.Grpc.Net.Client.total-calls").GetMetricPoints())
            metricPoints.Add(point);
        var totalCallsValue = metricPoints.Last().GetGaugeLastValueDouble();
        Assert.Equal(1, totalCallsValue);

        // calls-failed = 0
        metricPoints.Clear();
        foreach (ref readonly var point in exportedItems.First(x => x.Name == "ec.Grpc.Net.Client.calls-failed").GetMetricPoints())
            metricPoints.Add(point);
        var failedCallsValue = metricPoints.Last().GetGaugeLastValueDouble();
        Assert.Equal(0, failedCallsValue);
    }

    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task Enqueue_WrongUrl_ReturnsDeadlineExceeded(string email, string subject, string body)
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            [$"{EmailSenderOptions.SectionName}:{nameof(EmailSenderOptions.ServiceURL)}"] = "https://localhost" // Неверный URL сервиса
        };

        // Заменяем конфигурацию и достаём сервис
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            // Конфигурация до того, как будет вызван WebApplication.CreateBuilder(args);
            builder.UseConfiguration(configuration);
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                // Переопределяет значения после WebApplication.CreateBuilder(args);
                config.AddInMemoryCollection(dict);
            });
        });
        var scope = factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        var queueEmail = scopedServices.GetRequiredService<IQueueEmail>();

        var letter = new Letter(email, subject, body);

        // Act
        var result = await queueEmail.EnqueueAsync(letter);

        // Assert
        Assert.Equal(StatusCode.DeadlineExceeded, result);
    }

    [Fact]
    public async Task Enqueue_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Letter letter = null;

        // Act
        Func<Task> a = async () =>
        {
            await _queueEmail.EnqueueAsync(letter);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);
        Assert.Contains(nameof(letter), ex.ParamName);
    }
}