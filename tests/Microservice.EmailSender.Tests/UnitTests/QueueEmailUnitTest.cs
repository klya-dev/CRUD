namespace Microservice.EmailSender.Tests.UnitTests;

public sealed class QueueEmailUnitTest
{
    private readonly QueueEmail _queueEmail;

    public QueueEmailUnitTest()
    {
        _queueEmail = new QueueEmail();
    }

    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task Enqueue_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);

        // Act
        await _queueEmail.EnqueueAsync(letter, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue));
    }

    [Fact]
    public async Task Enqueue_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        Letter letter = null;

        // Act
        Func<Task> a = async () =>
        {
            await _queueEmail.EnqueueAsync(letter, TestContext.Current.CancellationToken);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);
        Assert.Contains(nameof(letter), ex.ParamName);

        Assert.False(_queueEmail.TryDequeue(out var letterDequeue));
    }


    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task EnqueueByLetterBackground_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var letterBackground = new LetterBackground(letter);

        // Act
        await _queueEmail.EnqueueAsync(letterBackground, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue));
    }

    [Fact]
    public async Task EnqueueByLetterBackground_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        LetterBackground letterBackground = null;

        // Act
        Func<Task> a = async () =>
        {
            await _queueEmail.EnqueueAsync(letterBackground, TestContext.Current.CancellationToken);
        };

        // Assert
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(a);
        Assert.Equivalent("letterBackground", ex.ParamName);

        Assert.False(_queueEmail.TryDequeue(out var letterDequeue));
    }


    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task TryDequeue_Letter_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        await _queueEmail.EnqueueAsync(letter, TestContext.Current.CancellationToken);

        // Act
        var result = _queueEmail.TryDequeue(out var letterDequeue);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task TryDequeue_LetterBackground_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var letterBackground = new LetterBackground(letter);
        await _queueEmail.EnqueueAsync(letterBackground, TestContext.Current.CancellationToken);

        // Act
        var result = _queueEmail.TryDequeue(out var letterDequeue);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryDequeue_CorrectData_NotEnqueue_ReturnsVoid()
    {
        // Arrange

        // Act
        var result = _queueEmail.TryDequeue(out var letterDequeue);

        // Assert
        Assert.False(result);
    }
}