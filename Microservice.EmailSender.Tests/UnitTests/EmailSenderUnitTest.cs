using MailKit.Net.Smtp;
using Microservice.EmailSender.Options;
using Microservice.EmailSender.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Microservice.EmailSender.Tests.UnitTests;

public class EmailSenderUnitTest
{
    private readonly Services.EmailSender _emailSender;
    private readonly Mock<IOptions<SmtpServerOptions>> _options;
    private readonly Mock<ILogger<Services.EmailSender>> _logger;

    public EmailSenderUnitTest()
    {
        _options = new();
        _logger = new();

        var options = new SmtpServerOptions() { Host = "", Port = 0, AuthPassword = "", Name = "", Email = "" };
        _options.Setup(x => x.Value).Returns(options);

        _emailSender = new Services.EmailSender(_options.Object, _logger.Object);
    }

    [Fact]
    public async Task DisconnectAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        SmtpClient smtpClient = null;

        // Act
        Func<Task> a = async () =>
        {
            await _emailSender.DisconnectAsync(smtpClient);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);
        Assert.Equivalent(nameof(smtpClient), ex.ParamName);
    }


    [Fact]
    public void Connect_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        SmtpClient smtpClient = null;

        // Act
        Action a = () =>
        {
            _emailSender.Connect(smtpClient);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Equivalent(nameof(smtpClient), ex.ParamName);
    }


    [Fact]
    public async Task SendEmailAsyncBySmtpClient_NullObject_Letter_ThrowsArgumentNullException()
    {
        // Arrange
        Letter letter = null;
        var smtpClient = new SmtpClient();

        // Act
        Func<Task> a = async () =>
        {
            await _emailSender.SendEmailAsync(letter, smtpClient);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(letter), ex.ParamName);

        // Отключаемся
        await _emailSender.DisconnectAsync(smtpClient);
    }

    [Fact]
    public async Task SendEmailAsyncBySmtpClient_NullObject_SmtpClient_ThrowsArgumentNullException()
    {
        // Arrange
        Letter letter = new Letter(Guid.NewGuid(), "e", "s", "b");
        SmtpClient smtpClient = null;

        // Act
        Func<Task> a = async () =>
        {
            await _emailSender.SendEmailAsync(letter, smtpClient);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(smtpClient), ex.ParamName);
    }


    [Fact]
    public async Task SendEmailAsync_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Letter letter = null;

        // Act
        Func<Task> a = async () =>
        {
            await _emailSender.SendEmailAsync(letter);
        };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);

        // Assert
        Assert.Contains(nameof(letter), ex.ParamName);
    }
}