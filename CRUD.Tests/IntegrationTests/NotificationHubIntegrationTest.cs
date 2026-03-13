using CRUD.Utility.Metrics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Diagnostics.Metrics;

namespace CRUD.Tests.IntegrationTests;

public class NotificationHubIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ITokenManager _tokenManager;
    private readonly IMeterFactory _meterFactory;

    public NotificationHubIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        TestWebApplicationFactory.RecreateDatabase();

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _tokenManager = scopedServices.GetRequiredService<ITokenManager>();
        _meterFactory = scopedServices.GetRequiredService<IMeterFactory>();
    }

    [Fact]
    public async Task IsUsefulNotification_ShouldAddMeter()
    {
        // Arrange
        var client = _factory.HttpClient;
        var server = _factory.Server;

        // Генерируем токен
        var request = new HttpRequestMessage(HttpMethod.Get, "");
        string token = TestConstants.AddBearerToken(request, _tokenManager);

        var collector = new MetricCollector<int>(_meterFactory, ApiMeters.MeterName, ApiMeters.UsefulNotificationMeterName);
        var notificationId = Guid.NewGuid();

        // Подключаемся к хабу
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://localhost/{TestConstants.NOTIFICATION_HUB_URL}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!; // Аутентификация
                options.HttpMessageHandlerFactory = _ => server.CreateHandler(); // Чтобы подключится
            })
            .AddMessagePackProtocol()
            .Build();
        await hubConnection.StartAsync();

        // Act
        await hubConnection.SendAsync("IsUsefulNotification", notificationId);

        // Assert
        // Метрика добавилась
        await collector.WaitForMeasurementsAsync(minCount: 1).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Collection(collector.GetMeasurementSnapshot(),
            measurement =>
            {
                Assert.Equal(notificationId, measurement.Tags["notificationId"]);
                Assert.Equal(1, measurement.Value);
            });
    }
}