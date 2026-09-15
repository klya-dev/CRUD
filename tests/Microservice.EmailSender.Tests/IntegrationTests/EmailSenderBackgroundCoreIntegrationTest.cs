namespace Microservice.EmailSender.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class EmailSenderBackgroundCoreIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
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
        var result = await _emailSenderBackgroundCore.CreateSmtpClientsAsync(TestContext.Current.CancellationToken);

        // Assert
        foreach (var smtpClient in result)
            Assert.True(smtpClient.IsConnected);
    }

    [Fact] // Проверяем переподключение
    public async Task CreateSmtpClientsAsync_CorrectData_CheckReconnect_ReturnsList()
    {
        // Arrange

        // Act
        var result = await _emailSenderBackgroundCore.CreateSmtpClientsAsync(TestContext.Current.CancellationToken);

        // Assert
        foreach (var smtpClient in result)
            Assert.True(smtpClient.IsConnected);

        // Отключаемся, и проверяем переподключение
        foreach (var smtpClient in result)
            await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);

        int i = 0;
        while (i < 25)
        {
            // Если все переподключились, иначе продолжаем ждать
            if (result.Where(x => x.IsConnected).Count() == result.Count)
                return;

            i++;
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        Assert.Fail("Переподключение не удалось");
    }
}