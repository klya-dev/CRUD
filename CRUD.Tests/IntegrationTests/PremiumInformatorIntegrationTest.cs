using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text.Json;

namespace CRUD.Tests.IntegrationTests;

public sealed class PremiumInformatorIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IPremiumInformator _premiumInformator;

    private readonly string Hostname;
    private readonly int Port;

    public PremiumInformatorIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(configuration => configuration.WithTestHttpContextAccessor());

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _premiumInformator = scopedServices.GetRequiredService<IPremiumInformator>();
        var configuration = scopedServices.GetRequiredService<IConfiguration>();

        // Получаем строку подключения и разбиваем на части Hostname и Port
        var connectionString = configuration.GetConnectionString("RabbitMqConnection") ?? string.Empty;
        var parts = connectionString.Split(':');
        Hostname = parts[0];
        Port = parts.Length > 1 ? int.Parse(parts[1]) : 5672; // Если часть одна, то используем дефолтный порт
    }

    [Fact]
    public async Task InformateAsync_ReturnsServiceResult()
    {
        // Arrange
        string email = "test@test.test";
        string languageCode = "ru";

        // Act
        await _premiumInformator.InformateAsync(email, languageCode, TestContext.Current.CancellationToken);

        // Assert
        // Подключаемся к RabbitMQ
        var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };
        using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Объявляем очередь для этого потребителя
        await channel.QueueDeclareAsync(
            queue: "informs-consumer-test",
            durable: true, // Очередь не удалится после перезапуска Rabbit'а (сообщения внутри удалятся, если только не указать Persistent в BasicPublishAsync)
            exclusive: false, // Очередь может использоваться другими соединениями, а не только текущим (получить, удалить и тд)
            autoDelete: false, // Не удалять очередь даже, когда все потребители отключатся
            arguments: null,
            cancellationToken: TestContext.Current.CancellationToken);

        // Привязываем очередь к обменнику
        await channel.QueueBindAsync("informs-consumer-test", "informs", routingKey: string.Empty, cancellationToken: TestContext.Current.CancellationToken);

        // Получаем сообщение
        var result = await channel.BasicGetAsync("informs-consumer-test", autoAck: false, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        // Сравниваем содержимое
        var receivedLetter = JsonSerializer.Deserialize<EnqueueLetterRequest>(result.Body.Span);
        Assert.NotNull(receivedLetter);
        Assert.Equal(email, receivedLetter.Email);
    }
}