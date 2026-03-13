using System.Net.Sockets;

namespace CRUD.WebApi.Helpers;

/// <summary>
/// Фабрика подключений Unix domain sockets (UDS).
/// </summary>
public class UnixDomainSocketsConnectionFactory
{
    private readonly EndPoint endPoint;

    public UnixDomainSocketsConnectionFactory(EndPoint endPoint)
    {
        this.endPoint = endPoint;
    }

    /// <summary>
    /// Подключает соединение к Unix адресу.
    /// </summary>
    /// <param name="_"><see cref="SocketsHttpConnectionContext"/>.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="Stream"/>.</returns>
    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _, CancellationToken cancellationToken = default)
    {
        // Через using не надо, socket будет дизпозиться и вызывать исключение
        //using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        //await socket.ConnectAsync(this.endPoint, cancellationToken).ConfigureAwait(false);
        //return new NetworkStream(socket, true);

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            await socket.ConnectAsync(this.endPoint, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}