using Microservice.EmailSender.Services.RabbitMqConsumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Microservice.EmailSender.Tests.IntegrationTests;

public class RabbitMqConsumerBackgroundCoreUnitTest
{
    private readonly Mock<IQueueEmail> _mockQueueEmail;
    private readonly Mock<ILogger<RabbitMqConsumerBackgroundCore>> _mockLogger;
    private readonly Mock<IChannel> _mockChannel;
    private readonly RabbitMqConsumerBackgroundCore _rabbitMqConsumerBackgroundCore;

    public RabbitMqConsumerBackgroundCoreUnitTest()
    {
        _mockQueueEmail = new();
        _mockLogger = new();
        _mockChannel = new();

        _rabbitMqConsumerBackgroundCore = new RabbitMqConsumerBackgroundCore(_mockQueueEmail.Object, _mockLogger.Object);
    }

    [Fact] // При вызове происходит настройка очередей, обменников и тд
    public async Task DoWorkAsync_ShouldDeclareExchangeQueueAndBind_WhenCalled()
    {
        // Assert

        // Act
        // Выполняем настройки очередей (обменник, привязка, обработчик)
        await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_mockChannel.Object);

        // Assert
        // Обменник объявлен
        _mockChannel.Verify(x => x.ExchangeDeclareAsync("informs", ExchangeType.Fanout, true, false, It.IsAny<IDictionary<string, object?>>(), false, false, It.IsAny<CancellationToken>()), Times.Once);

        // Очередь объявлена
        _mockChannel.Verify(x => x.QueueDeclareAsync("informs-consumer-1", true, false, false, It.IsAny<IDictionary<string, object?>>(), false, false, It.IsAny<CancellationToken>()),Times.Once);

        // Очередь привязанна
        _mockChannel.Verify(x => x.QueueBindAsync("informs-consumer-1", "informs", string.Empty, It.IsAny<IDictionary<string, object?>>(), false, It.IsAny<CancellationToken>()), Times.Once);

        // Подписка на обработчик
        _mockChannel.Verify(x => x.BasicConsumeAsync("informs-consumer-1", false, string.Empty, false, false, It.IsAny<IDictionary<string, object?>>(), It.IsAny<AsyncEventingBasicConsumer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Пришло сообщение - письмо поставилось в очередь и подтверждение получения
    public async Task DoWorkAsync_ShouldEnqueueAndAck_WhenMessageIncomming()
    {
        // Assert
        var letterId = Guid.NewGuid();
        var request = new EnqueueLetterRequest { Id = letterId.ToString(), Email = "test@test.test", Subject = "sub", Body = "body" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

        // Перехватываем AsyncEventingBasicConsumer
        AsyncEventingBasicConsumer capturedConsumer = null!;
        _mockChannel.Setup(x => x.BasicConsumeAsync(
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<IDictionary<string, object?>>(),
                   It.IsAny<IAsyncBasicConsumer>(),
                   It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => capturedConsumer = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("consumer-tag");

        // Выполняем настройки очередей (обменник, привязка, обработчик)
        await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_mockChannel.Object);

        // Act
        // Приход сообщения
        await capturedConsumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: false,
            exchange: "informs",
            routingKey: string.Empty,
            properties: new ReadOnlyBasicProperties([]),
            body: new ReadOnlyMemory<byte>(body)
        );

        // Assert
        // Письмо поставленно в очередь
        _mockQueueEmail.Verify(x => x.EnqueueAsync(It.Is<Letter>(l => l.Id == letterId), It.IsAny<CancellationToken>()), Times.Once);

        // Подтверждение получения письма
        _mockChannel.Verify(x => x.BasicAckAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Пришло невалидное сообщение - отмена сообщения
    public async Task DoWorkAsync_ShouldReject_WhenNotValidMessageIncomming()
    {
        // Assert
        var letterId = "NOT VALID GUID";
        var request = new EnqueueLetterRequest { Id = letterId.ToString(), Email = "test@test.test", Subject = "sub", Body = "body" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

        // Перехватываем AsyncEventingBasicConsumer
        AsyncEventingBasicConsumer capturedConsumer = null!;
        _mockChannel.Setup(x => x.BasicConsumeAsync(
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<IDictionary<string, object?>>(),
                   It.IsAny<IAsyncBasicConsumer>(),
                   It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => capturedConsumer = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("consumer-tag");

        // Выполняем настройки очередей (обменник, привязка, обработчик)
        await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_mockChannel.Object);

        // Act
        // Приход сообщения
        await capturedConsumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: false,
            exchange: "informs",
            routingKey: string.Empty,
            properties: new ReadOnlyBasicProperties([]),
            body: new ReadOnlyMemory<byte>(body)
        );

        // Assert
        // Отмена сообщения
        _mockChannel.Verify(x => x.BasicRejectAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Исключение во время обработки - письмо повторно добавляется в очередь
    public async Task DoWorkAsync_ShouldRejectWithRequeue_WhenThrowsException()
    {
        // Assert
        var letterId = Guid.NewGuid();
        var request = new EnqueueLetterRequest { Id = letterId.ToString(), Email = "test@test.test", Subject = "sub", Body = "body" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

        // Перехватываем AsyncEventingBasicConsumer
        AsyncEventingBasicConsumer capturedConsumer = null!;
        _mockChannel.Setup(x => x.BasicConsumeAsync(
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<IDictionary<string, object?>>(),
                   It.IsAny<IAsyncBasicConsumer>(),
                   It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => capturedConsumer = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("consumer-tag");

        // Выполняем настройки очередей (обменник, привязка, обработчик)
        await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_mockChannel.Object);

        // QueueEmail выбросило исключение
        _mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("some"));

        // Act
        // Приход сообщения
        await capturedConsumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: false,
            exchange: "informs",
            routingKey: string.Empty,
            properties: new ReadOnlyBasicProperties([]),
            body: new ReadOnlyMemory<byte>(body)
        );

        // Assert
        // Отмена письма с повторным добавление в очередь
        _mockChannel.Verify(x => x.BasicRejectAsync(123, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Исключение во время обработки + письмо уже было повторно добавлено в очередь - письмо отменяется без добавления в очередь
    public async Task DoWorkAsync_ShouldRejectWithoutRequeue_WhenThrowsException()
    {
        // Assert
        var letterId = Guid.NewGuid();
        var request = new EnqueueLetterRequest { Id = letterId.ToString(), Email = "test@test.test", Subject = "sub", Body = "body" };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

        // Перехватываем AsyncEventingBasicConsumer
        AsyncEventingBasicConsumer capturedConsumer = null!;
        _mockChannel.Setup(x => x.BasicConsumeAsync(
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<string>(),
                   It.IsAny<bool>(),
                   It.IsAny<bool>(),
                   It.IsAny<IDictionary<string, object?>>(),
                   It.IsAny<IAsyncBasicConsumer>(),
                   It.IsAny<CancellationToken>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object?>, IAsyncBasicConsumer, CancellationToken>(
                (_, _, _, _, _, _, consumer, _) => capturedConsumer = (AsyncEventingBasicConsumer)consumer)
            .ReturnsAsync("consumer-tag");

        // Выполняем настройки очередей (обменник, привязка, обработчик)
        await _rabbitMqConsumerBackgroundCore.DoWorkAsync(_mockChannel.Object);

        // QueueEmail выбросило исключение
        _mockQueueEmail.Setup(x => x.EnqueueAsync(It.IsAny<Letter>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("some"));

        // Act
        // Приход сообщения
        await capturedConsumer.HandleBasicDeliverAsync(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: true, // Уже было повторое добавление в очередь
            exchange: "informs",
            routingKey: string.Empty,
            properties: new ReadOnlyBasicProperties([]),
            body: new ReadOnlyMemory<byte>(body)
        );

        // Assert
        // Отмена письма без повторного добавления в очередь
        _mockChannel.Verify(x => x.BasicRejectAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}