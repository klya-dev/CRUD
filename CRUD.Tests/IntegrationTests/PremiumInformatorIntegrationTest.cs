using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text.Json;

namespace CRUD.Tests.IntegrationTests;

public class PremiumInformatorIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

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
        await _premiumInformator.InformateAsync(email, languageCode);

        // Assert
        // Подключаемся к RabbitMQ
        var factory = new ConnectionFactory() { HostName = Hostname, Port = Port };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // Получаем сообщение
        var result = await channel.BasicGetAsync("informs-consumer-1", autoAck: false);
        Assert.NotNull(result);

        // Сравниваем содержимое
        var receivedLetter = JsonSerializer.Deserialize<EnqueueLetterRequest>(result.Body.Span);
        Assert.NotNull(receivedLetter);
        Assert.Equal(email, receivedLetter.Email);
    }
}