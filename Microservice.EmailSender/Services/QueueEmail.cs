using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Microservice.EmailSender.Services;

/// <inheritdoc cref="IQueueEmail"/>
public class QueueEmail : IQueueEmail
{
    private readonly Channel<LetterBackground> _channel;

    public QueueEmail()
    {
        // Создаём неограниченный канал
        var options = new UnboundedChannelOptions()
        {
            SingleReader = false, // Несколько читателей
            SingleWriter = false  // Несколько писателей
        };
        _channel = Channel.CreateUnbounded<LetterBackground>(options);
    }

    public async Task EnqueueAsync(Letter letter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(letter);

        var letterBackground = new LetterBackground(letter);
        await _channel.Writer.WriteAsync(letterBackground, ct);
    }

    public async Task EnqueueAsync(LetterBackground letterBackground, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(letterBackground);

        await _channel.Writer.WriteAsync(letterBackground, ct);
    }

    public IAsyncEnumerable<LetterBackground> DequeueAllAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out LetterBackground letter)
    {
        return _channel.Reader.TryRead(out letter);
    }
}