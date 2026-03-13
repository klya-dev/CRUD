#nullable disable
using MailKit.Net.Smtp;

namespace Microservice.EmailSender.Tests.IntegrationTests;

public class EmailSenderIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    // #nullable disable

    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly IEmailSender _emailSender;

    public EmailSenderIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;

        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _emailSender = scopedServices.GetRequiredService<IEmailSender>();
    }

    private IEmailSender GenerateNewEmailSender()
    {
        var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        return scopedServices.GetRequiredService<IEmailSender>();
    }

    [Fact]
    public async Task ConnectAsync_ReturnsSmtpClient()
    {
        // Arrange

        // Act
        var result = await _emailSender.ConnectAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConnected);

        // Отключаемся
        await _emailSender.DisconnectAsync(result);
    }


    [Fact]
    public async Task DisconnectAsync_ReturnsVoid()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync();

        // Act
        await _emailSender.DisconnectAsync(smtpClient);
        
        // Assert
        Assert.False(smtpClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WrongData_ReturnsVoid()
    {
        // Arrange
        var smtpClient = new SmtpClient();

        // Act
        await _emailSender.DisconnectAsync(smtpClient);

        // Assert
        Assert.False(smtpClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WrongData_ReDisconnect_ReturnsVoid()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync();

        // Act
        await _emailSender.DisconnectAsync(smtpClient);
        await _emailSender.DisconnectAsync(smtpClient);

        // Assert
        Assert.False(smtpClient.IsConnected);
    }


    [Fact]
    public async Task Connect_CorrectData_ReturnsIsConnected()
    {
        // Arrange
        var smtpClient = await _emailSender.ConnectAsync();
        await _emailSender.DisconnectAsync(smtpClient);

        // Act
        _emailSender.Connect(smtpClient);

        // Assert
        Assert.NotNull(smtpClient);
        Assert.True(smtpClient.IsConnected);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
    }


    [Theory] // Корректные данные
    [InlineData("fan.ass95@mail.ru", "s", "b<br><b>Big</b>")]
    [InlineData("fan.ass95@mail.ru", "", "")]
    public async Task SendEmailAsyncBySmtpClient_ReturnsTrue(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await _emailSender.ConnectAsync();

        // Act
        var result = await _emailSender.SendEmailAsync(letter, smtpClient);

        // Assert
        Assert.True(result);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
    }

    [Theory]
    [InlineData("fan.ass995@mail.ru", "s", "b")] // Такого Email не существует
    public async Task SendEmailAsyncBySmtpClient_ReturnsFalse(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await _emailSender.ConnectAsync();

        // Act
        var result = await _emailSender.SendEmailAsync(letter, smtpClient);

        // Assert
        Assert.False(result);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
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
        var result = await _emailSender.SendEmailAsync(letter, smtpClient);

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
        var result = await _emailSender.SendEmailAsync(letter);

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
        var result = await _emailSender.SendEmailAsync(letter);

        // Assert
        Assert.False(result);
    }


    // Конфликты параллельности


    [Fact]
    public async Task ConnectAsync_ConcurrencyConflict_ReturnsSmtpClient()
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();

        // Act
        var task = emailSender.ConnectAsync();
        var task2 = emailSender2.ConnectAsync();

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConnected);

        Assert.Equivalent(result.IsConnected, result2.IsConnected);

        // Отключаемся
        await _emailSender.DisconnectAsync(result);
        await _emailSender.DisconnectAsync(result2);
    }


    [Fact]
    public async Task DisconnectAsync_ConcurrencyConflict_ThrowsNotSupportedException()
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var smtpClient = await emailSender.ConnectAsync();

        // Act
        Func<Task> a = async () =>
        {
            var task = emailSender.DisconnectAsync(smtpClient);
            var task2 = emailSender2.DisconnectAsync(smtpClient);

            await Task.WhenAll(task, task2);
        };

        // Assert
        await Assert.ThrowsAsync<NotSupportedException>(a);

        // Отключаемся
        await emailSender.DisconnectAsync(smtpClient);
    }

    [Fact]
    public async Task DisconnectAsync_ConcurrencyConflict_ReturnsVoid()
    {
        // Arrange
        var smtpClient = new SmtpClient();
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();

        // Act
        var task = emailSender.DisconnectAsync(smtpClient);
        var task2 = emailSender2.DisconnectAsync(smtpClient);

        await Task.WhenAll(task, task2);

        // Assert
        Assert.False(smtpClient.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_ConcurrencyConflict_ReDisconnect_ReturnsVoid()
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var smtpClient = await emailSender.ConnectAsync();

        // Act
        Func<Task> a = async () =>
        {
            var task = emailSender.DisconnectAsync(smtpClient);
            var task2 = emailSender2.DisconnectAsync(smtpClient);

            await Task.WhenAll(task, task2);
        };

        // Assert
        await Assert.ThrowsAsync<NotSupportedException>(a);

        // Отключаемся
        await emailSender.DisconnectAsync(smtpClient);
    }


    [Fact]
    public async Task Connect_ConcurrencyConflict_ReturnsIsConnected()
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var smtpClient = await emailSender.ConnectAsync();
        await emailSender.DisconnectAsync(smtpClient);

        // Act
        Func<Task> a = async () =>
        {
            var task = Task.Run(() => emailSender.Connect(smtpClient));
            var task2 = Task.Run(() => emailSender2.Connect(smtpClient));

            await Task.WhenAll(task, task2);
        };

        // Assert
        await Assert.ThrowsAsync<SmtpProtocolException>(a);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
    }


    [Theory] // Корректные данные
    [InlineData("fan.ass95@mail.ru", "s", "b<br><b>Big</b>")]
    public async Task SendEmailAsyncBySmtpClient_ConcurrencyConflict_ReturnsTrue(string email, string subject, string body)
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await emailSender.ConnectAsync();
        var smtpClient2 = await emailSender.ConnectAsync();

        // Act
        var task = emailSender.SendEmailAsync(letter, smtpClient);
        var task2 = emailSender2.SendEmailAsync(letter, smtpClient2);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.True(result);
        Assert.Equivalent(result, result2);

        // Отключаемся
        await emailSender.DisconnectAsync(smtpClient);
        await emailSender.DisconnectAsync(smtpClient2);
    }

    [Theory]
    [InlineData("fan.ass995@mail.ru", "s", "b")] // Такого Email не существует
    public async Task SendEmailAsyncBySmtpClient_ConcurrencyConflict_ReturnsFalse(string email, string subject, string body)
    {
        // Arrange
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = await emailSender.ConnectAsync();
        var smtpClient2 = await emailSender2.ConnectAsync();

        // Act
        var task = emailSender.SendEmailAsync(letter, smtpClient);
        var task2 = emailSender2.SendEmailAsync(letter, smtpClient2);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
        await _emailSender.DisconnectAsync(smtpClient2);
    }

    [Fact] // Корректные данные
    public async Task SendEmailAsyncBySmtpClient_ConcurrencyConflict_NotConnected_ReturnsFalse()
    {
        // Arrange
        string email = "some";
        string subject = "sub";
        string body = "body";

        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var smtpClient = new SmtpClient();

        // Act
        var task = emailSender.SendEmailAsync(letter, smtpClient);
        var task2 = emailSender2.SendEmailAsync(letter, smtpClient);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);
    }


    [Theory] // Корректные данные
    [InlineData("fan.ass95@mail.ru", "s", "b<br><b>Big</b>")]
    public async Task SendEmailAsync_ConcurrencyConflict_ReturnsTrue(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();

        // Act
        var task = emailSender.SendEmailAsync(letter);
        var task2 = emailSender2.SendEmailAsync(letter);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.True(result);
        Assert.Equivalent(result, result2);
    }

    [Theory]
    [InlineData("fan.ass995@mail.ru", "s", "b")] // Такого Email не существует
    public async Task SendEmailAsync_ConcurrencyConflict_ReturnsFalse(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var emailSender = GenerateNewEmailSender();
        var emailSender2 = GenerateNewEmailSender();

        // Act
        var task = emailSender.SendEmailAsync(letter);
        var task2 = emailSender2.SendEmailAsync(letter);

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);
    }
}