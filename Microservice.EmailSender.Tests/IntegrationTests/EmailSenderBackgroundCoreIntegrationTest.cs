#nullable disable

namespace Microservice.EmailSender.Tests.IntegrationTests;

public class EmailSenderBackgroundCoreIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IQueueEmail _queueEmail;
    private readonly IEmailSenderBackgroundCore _emailSenderBackgroundCore;
    private readonly IEmailSender _emailSender;

    public EmailSenderBackgroundCoreIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _queueEmail = scopedServices.GetRequiredService<IQueueEmail>();
        _emailSenderBackgroundCore = scopedServices.GetRequiredService<IEmailSenderBackgroundCore>();
        _emailSender = scopedServices.GetRequiredService<IEmailSender>();
    }

    [Fact]
    public async Task CreateSmtpClientsAsync_CorrectData_ReturnsList()
    {
        // Arrange

        // Act
        var result = await _emailSenderBackgroundCore.CreateSmtpClientsAsync();

        // Assert
        foreach (var smtpClient in result)
            Assert.True(smtpClient.IsConnected);
    }

    [Fact] // Проверяем переподключение
    public async Task CreateSmtpClientsAsync_CorrectData_CheckReconnect_ReturnsList()
    {
        // Arrange

        // Act
        var result = await _emailSenderBackgroundCore.CreateSmtpClientsAsync();

        // Assert
        foreach (var smtpClient in result)
            Assert.True(smtpClient.IsConnected);

        // Отключаемся, и проверяем переподключение
        foreach (var smtpClient in result)
            await _emailSender.DisconnectAsync(smtpClient);

        int i = 0;
        while (i < 25)
        {
            // Если все переподключились, иначе продолжаем ждать
            if (result.Where(x => x.IsConnected).Count() == result.Count)
                return;

            i++;
            await Task.Delay(500);
        }

        Assert.Fail("Переподключение не удалось");
    }
}