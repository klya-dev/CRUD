using Grpc.Core;

namespace Microservice.EmailSender.Tests.UnitTests;

public class GrpcEmailSenderServiceUnitTest
{
    private readonly GrpcEmailSenderService _grpcEmailSenderService;
    private readonly Mock<ILogger<GrpcEmailSenderService>> _mockLogger;
    private readonly Mock<IQueueEmail> _mockQueueEmail;

    public GrpcEmailSenderServiceUnitTest()
    {
        _mockLogger = new();
        _mockQueueEmail = new();

        _grpcEmailSenderService = new GrpcEmailSenderService(_mockLogger.Object, _mockQueueEmail.Object);
    }

    [Fact]
    public async Task Enqueue_ReturnsEnqueueLetterReply()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EmailSender.EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        // IQueueEmail отработает без ошибок
        _mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _grpcEmailSenderService.Enqueue(request, TestServerCallContext.Create());

        // Assert
        Assert.NotNull(result);

        _mockQueueEmail.Verify(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Enqueue_WithoutLetterId_ThrowsRpcExceptionStatusInvalidArgument()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EmailSender.EnqueueLetterRequest
        {
            // Без Id
            Email = email,
            Subject = subject,
            Body = body
        };

        // Act
        Func<Task> a = async () =>
        {
            await _grpcEmailSenderService.Enqueue(request, TestServerCallContext.Create());
        };

        // Assert
        var ex = await Assert.ThrowsAsync<RpcException>(a);
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);

        _mockQueueEmail.Verify(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Enqueue_WhenThrowsException_ThrowsException()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EmailSender.EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        // IQueueEmail выбросит исключение
        _mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        // Act
        Func<Task> a = async () =>
        {
            await _grpcEmailSenderService.Enqueue(request, TestServerCallContext.Create());
        };

        // Assert
        await Assert.ThrowsAsync<Exception>(a);
    }

    [Fact]
    public async Task Enqueue_WhenThrowsTaskCanceledException_ThrowsTaskCanceledException()
    {
        // Arrange
        var email = "fan.ass95@mail.ru";
        var subject = "test";
        var body = "test";

        var request = new EmailSender.EnqueueLetterRequest
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Subject = subject,
            Body = body
        };

        // IQueueEmail выбросит исключение
        _mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TaskCanceledException());

        // Act
        Func<Task> a = async () =>
        {
            await _grpcEmailSenderService.Enqueue(request, TestServerCallContext.Create());
        };

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(a);
    }

    [Fact] // Исключение на стороне клиента, т.к сообщение gRPC в данном случае не допускает null
    public async Task Enqueue_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string email = null;
        string subject = "test";
        string body = "test";

        EmailSender.EnqueueLetterRequest request = null;

        // Act
        // Не можем даже создать запрос, если null данные
        Func<Task> a = async () =>
        {
            request = new EmailSender.EnqueueLetterRequest
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Subject = subject,
                Body = body
            };
        };

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(a);

        _mockQueueEmail.Verify(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}