#nullable disable

namespace Microservice.EmailSender.Tests.IntegrationTests;

public class QueueEmailIntegrationTest
{
    // #nullable disable

    private readonly QueueEmail _queueEmail;

    public QueueEmailIntegrationTest()
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
        await _queueEmail.EnqueueAsync(letter);

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
            await _queueEmail.EnqueueAsync(letter);
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
        await _queueEmail.EnqueueAsync(letterBackground);

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
            await _queueEmail.EnqueueAsync(letterBackground);
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
        await _queueEmail.EnqueueAsync(letter);

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
        await _queueEmail.EnqueueAsync(letterBackground);

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


    // Конфликты параллельности


    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task Enqueue_ConcurrencyConflict_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);

        // Act
        var task = _queueEmail.EnqueueAsync(letter);
        var task2 = _queueEmail.EnqueueAsync(letter);

        await Task.WhenAll(task, task2);

        // Assert
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue));
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue2));
    }


    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task EnqueueByLetterBackground_ConcurrencyConflict_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var letterBackground = new LetterBackground(letter);

        // Act
        var task = _queueEmail.EnqueueAsync(letter);
        var task2 = _queueEmail.EnqueueAsync(letter);

        await Task.WhenAll(task, task2);

        // Assert
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue));
        Assert.True(_queueEmail.TryDequeue(out var letterDequeue2));
    }


    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task TryDequeue_ConcurrencyConflict_Letter_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        await _queueEmail.EnqueueAsync(letter);

        // Act
        var task = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));
        var task2 = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        if (!(result && !result2 || !result && result2))
            Assert.Fail("Не true/false | Не false/true");
    }

    [Theory]
    [InlineData("fan.ass95@mail.ru", "s", "b")]
    public async Task TryDequeue_ConcurrencyConflict_LetterBackground_CorrectData_ReturnsVoid(string email, string subject, string body)
    {
        // Arrange
        var letter = new Letter(Guid.NewGuid(), email, subject, body);
        var letterBackground = new LetterBackground(letter);

        // Act
        var task = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));
        var task2 = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);
    }

    [Fact]
    public async Task TryDequeue_ConcurrencyConflict_CorrectData_NotEnqueue_ReturnsVoid()
    {
        // Arrange

        // Act
        var task = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));
        var task2 = Task.Run(() => _queueEmail.TryDequeue(out var letterDequeue));

        var results = await Task.WhenAll(task, task2);
        var result = results[0];
        var result2 = results[1];

        // Assert
        Assert.False(result);
        Assert.Equivalent(result, result2);
    }
}