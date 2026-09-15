using MailKit.Net.Smtp;

namespace Microservice.EmailSender.Tests.IntegrationTests;

[Collection(nameof(IntegrationsTestCollection))]
public sealed class EmailSenderIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IEmailSender _emailSender;

    public EmailSenderIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _emailSender = scopedServices.GetRequiredService<IEmailSender>();
    }

    [Fact]
    public async Task ConnectAsync_ReturnsSmtpClient()
    {
        // Arrange

        // Act
        var result = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConnected);

        // Отключаемся
        await _emailSender.DisconnectAsync(result, TestContext.Current.CancellationToken);
    }


    [Fact]
    public async Task DisconnectAsync_ReturnsVoid()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);

        // Act
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);
        
        // Assert
        Assert.False(smtpClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WrongData_ReturnsVoid()
    {
        // Arrange
        var smtpClient = new SmtpClient();

        // Act
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(smtpClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WrongData_ReDisconnect_ReturnsVoid()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);

        // Act
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(smtpClient.IsConnected);
    }


    [Fact]
    public async Task Connect_CorrectData_ReturnsIsConnected()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);

        // Act
        _emailSender.Connect(smtpClient);

        // Assert
        Assert.NotNull(smtpClient);
        Assert.True(smtpClient.IsConnected);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);
    }


    [Theory] // Корректные данные
    [InlineData("fan.ass95@mail.ru", "s", "b<br><b>Big</b>")]
    [InlineData("fan.ass95@mail.ru", "", "")]
    public async Task SendEmailAsyncBySmtpClient_ReturnsTrue(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _emailSender.SendEmailAsync(letter, smtpClient, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("fan.ass995@mail.ru", "s", "b")] // Такого Email не существует
    public async Task SendEmailAsyncBySmtpClient_ReturnsFalse(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await _emailSender.ConnectAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _emailSender.SendEmailAsync(letter, smtpClient, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient, TestContext.Current.CancellationToken);
    }

    [Fact] // Корректные данные
    public async Task SendEmailAsyncBySmtpClient_NotConnected_ReturnsFalse()
    {
        // Arrange
        string email = "some";
        string subject = "sub";
        string body = "body";
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = new SmtpClient();

        // Act
        var result = await _emailSender.SendEmailAsync(letter, smtpClient, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }


    [Theory] // Корректные данные
    [InlineData("fan.ass95@mail.ru", "s", "b<br><b>Big</b>")]
    [InlineData("fan.ass95@mail.ru", "", "")]
    public async Task SendEmailAsync_ReturnsTrue(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);

        // Act
        var result = await _emailSender.SendEmailAsync(letter, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("fan.ass995@mail.ru", "s", "b")] // Такого Email не существует
    public async Task SendEmailAsync_ReturnsFalse(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);

        // Act
        var result = await _emailSender.SendEmailAsync(letter, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }
}