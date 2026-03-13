#nullable disable
using MailKit.Net.Smtp;
using System.Reflection;
using System.Threading.Channels;

namespace Microservice.EmailSender.Tests.UnitTests;

public class EmailSenderBackgroundCoreUnitTest
{
    // #nullable disable

    private readonly EmailSenderBackgroundCore _emailSenderBackgroundCore;
    private readonly Mock<IQueueEmail> _queueEmailMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IOptions<EmailSenderBackgroundServiceOptions>> _optionsMock;
    private readonly Mock<ILogger<EmailSenderBackgroundCore>> _loggerMock;

    public EmailSenderBackgroundCoreUnitTest()
    {
        _queueEmailMock = new();
        _emailSenderMock = new();
        _optionsMock = new();
        _loggerMock = new();

        _optionsMock.Setup(x => x.Value).Returns(new EmailSenderBackgroundServiceOptions() { SmtpClientsCount = 3, RetriesCount = 2, DefaultTimeout = TimeSpan.FromSeconds(1), TimeoutCoefficient = 1.25f, LimitLetters = 3, LimitLettersTime = TimeSpan.FromHours(1) });

        _emailSenderBackgroundCore = new EmailSenderBackgroundCore(_queueEmailMock.Object, _emailSenderMock.Object, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact] // Пустая очередь - сервис ждёт и ничего не делает
    public async Task DoWorkAsync_QueueEmpty_DelaysAndDoesNothing()
    {
        // Arrange
        // Достаём письма
        LetterBackground[] letters = [];
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>())).Returns(letters.ToAsyncEnumerable());

        var smtpClients = new List<SmtpClient> { new SmtpClient(), new SmtpClient(), new SmtpClient() };
        using var cts = new CancellationTokenSource(3000); // ограничим время

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, cts.Token);

        // Assert
        // Никаких вызовов к отправке быть не должно
        _emailSenderMock.Verify(es => es.SendEmailAsync(It.IsAny<LetterBackground>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // Успешная отправка письма - сервис добавляет в аналитику
    public async Task DoWorkAsync_SuccessfulSend_AddsAnalytics()
    {
        // Arrange
        var letter = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));

        // Создаём реальный Channel для теста
        var channel = Channel.CreateUnbounded<LetterBackground>();
        await channel.Writer.WriteAsync(letter);
        channel.Writer.Complete();
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>()))
                       .Returns(channel.Reader.ReadAllAsync());

        // Успешная отправка письма
        _emailSenderMock.Setup(es => es.SendEmailAsync(letter, It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var smtpClients = new List<SmtpClient> { new SmtpClient(), new SmtpClient(), new SmtpClient() };

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, CancellationToken.None);

        // Assert
        _emailSenderMock.Verify(es => es.SendEmailAsync(letter, It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>()), Times.Once);

        // Проверим, что внутренняя аналитика содержит запись
        // Доступ к приватному _lettersAnalytics получим через reflection
        var analyticsField = typeof(EmailSenderBackgroundCore).GetField("_lettersAnalytics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(analyticsField);

        var analytics = analyticsField!.GetValue(_emailSenderBackgroundCore) as Dictionary<string, List<DateTime>>;
        Assert.NotNull(analytics);
        Assert.True(analytics!.ContainsKey(letter.Email));
        Assert.Single(analytics[letter.Email]);
    }

    [Fact] // Неудачная отправка письма - сервис добавляет письмо обратно в очередь с неудачной попыткой
    public async Task DoWorkAsync_FailedSend_RequeuesAndIncrementsError()
    {
        // Arrange
        var letter = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));

        // Создаём реальный Channel для теста
        var channel = Channel.CreateUnbounded<LetterBackground>();
        await channel.Writer.WriteAsync(letter);
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>()))
                       .Returns(channel.Reader.ReadAllAsync());

        int writeCount = 0;
        _queueEmailMock.Setup(x => x.EnqueueAsync(It.IsAny<LetterBackground>(), It.IsAny<CancellationToken>()))
            .Callback<LetterBackground, CancellationToken>((letter, ct) =>
            {
                writeCount++;
                if (writeCount == 5) // Количество вызовов EnqueueAsync
                    channel.Writer.Complete(); // Чтобы Queue.DequeueAllAsync не ждал новые письма

                channel.Writer.TryWrite(letter); // Вписываем письмо в канал
            });

        // Неудачная отправка письма на каждый вызов
        _emailSenderMock.Setup(es => es.SendEmailAsync(letter, It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var smtpClients = new List<SmtpClient> { new SmtpClient(), new SmtpClient(), new SmtpClient() };

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, CancellationToken.None);

        // Assert: письмо поставлено обратно в очередь 5 раз (из которых 2 для таймаута) и ErrorCount увеличен
        _queueEmailMock.Verify(q => q.EnqueueAsync(It.Is<LetterBackground>(x => x == letter), It.IsAny<CancellationToken>()), Times.Exactly(5));
        Assert.Equal(3, letter.ErrorCount); // Первая отправка неудачная, и две повторные тоже

        // SendEmailAsync вызывается 3 раза, первая и повторные попытки
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<LetterBackground>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact] // Количество неудачных попыток достигло лимит - сервис пропускает письмо
    public async Task DoWorkAsync_ReachedRetries_DoesNotRequeue()
    {
        // Arrange
        var letter = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));
        letter.IncrementError(); // Первая попытка                (ErrorCount = 1)
        letter.IncrementError(); // Первая повторная попытка (ErrorCount = 2)
        letter.IncrementError(); // Вторая повторная попытка (ErrorCount = 3)

        // Создаём реальный Channel для теста
        var channel = Channel.CreateUnbounded<LetterBackground>();
        await channel.Writer.WriteAsync(letter);
        channel.Writer.Complete();
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>()))
                       .Returns(channel.Reader.ReadAllAsync());

        // Неудачная отправка письма
        _emailSenderMock.Setup(es => es.SendEmailAsync(letter, It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var smtpClients = new List<SmtpClient> { new SmtpClient(), new SmtpClient(), new SmtpClient() };

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, CancellationToken.None);

        // Assert
        // Письмо не добавляется в очередь, т.к количество повторных попыток 2 (ErrorCount - 1)
        _queueEmailMock.Verify(q => q.EnqueueAsync(It.IsAny<LetterBackground>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // Достигнут лимит на одну электронную почту в час - сервис пропускает письмо
    public async Task DoWorkAsync_ReachedRateLimit_DoesNotRequeue()
    {
        // Arrange
        var letter = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));
        var letter2 = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));
        var letter3 = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));
        var letter4 = new LetterBackground(new Letter(Guid.NewGuid(), "a@test", "s", "b"));

        // Создаём реальный Channel для теста
        var channel = Channel.CreateUnbounded<LetterBackground>();
        await channel.Writer.WriteAsync(letter);
        await channel.Writer.WriteAsync(letter2);
        await channel.Writer.WriteAsync(letter3);
        await channel.Writer.WriteAsync(letter4);
        channel.Writer.Complete();
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>()))
                       .Returns(channel.Reader.ReadAllAsync());

        // Успешная отправка письмем
        _emailSenderMock.Setup(es => es.SendEmailAsync(It.IsAny<LetterBackground>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var smtpClients = new List<SmtpClient> { new SmtpClient(), new SmtpClient(), new SmtpClient() };

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, CancellationToken.None);

        // Assert
        // Письмо не добавляется в очередь, т.к все попытки успешные
        _queueEmailMock.Verify(q => q.EnqueueAsync(It.IsAny<LetterBackground>(), It.IsAny<CancellationToken>()), Times.Never);

        // SendEmailAsync вызывается 3 раза, четвёртый не вызывается, т.к лимит
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<LetterBackground>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact] // Успешная отправка писем
    public async Task DoWorkAsync_CorrectData_ReturnsTask()
    {
        // Arrange
        _emailSenderMock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SmtpClient()); // Создаются пустые SmtpClient'ы

        var smtpClients = await _emailSenderBackgroundCore.CreateSmtpClientsAsync();
        var ctx = new CancellationToken();

        var letter = new LetterBackground(new Letter(Guid.NewGuid(), "email", "sub", "body"));
        var letter2 = new LetterBackground(new Letter(Guid.NewGuid(), "email", "sub", "body"));
        var letter3 = new LetterBackground(new Letter(Guid.NewGuid(), "email", "sub", "body"));

        // Создаём реальный Channel для теста
        var channel = Channel.CreateUnbounded<LetterBackground>();
        await channel.Writer.WriteAsync(letter);
        await channel.Writer.WriteAsync(letter2);
        await channel.Writer.WriteAsync(letter3);
        channel.Writer.Complete();
        _queueEmailMock.Setup(x => x.DequeueAllAsync(It.IsAny<CancellationToken>()))
                       .Returns(channel.Reader.ReadAllAsync());

        // Письма всегда успешно отправляются
        _emailSenderMock.Setup(x => x.SendEmailAsync(It.IsAny<Letter>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, ctx);

        // Assert
        // SendEmailAsync вызывается 3 раза
        _emailSenderMock.Verify(x => x.SendEmailAsync(It.IsAny<LetterBackground>(), It.IsAny<SmtpClient>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        // Письма ни разу не добавлялись в очередь повторно
        _queueEmailMock.Verify(q => q.EnqueueAsync(It.IsAny<LetterBackground>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}