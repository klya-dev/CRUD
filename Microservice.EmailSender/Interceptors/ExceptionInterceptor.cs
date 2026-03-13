using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Microservice.EmailSender.Interceptors;

/// <summary>
/// Перехватчик (интерцептор) исключений.
/// </summary>
/// <remarks>
/// <para>Логирует и сопоставляет различные исключения к <see cref="RpcException"/>, добавляет <c>CorrelationId</c> в <see cref="RpcException.Trailers"/>.</para>
/// <para>Например, если в сервисе выбрасывается исключение <see cref="TaskCanceledException"/>, то клиенту выбросится <see cref="RpcException"/> со статусом <see cref="StatusCode.Cancelled"/>.</para>
/// <para><c>CorrelationId</c> добавляется в <see cref="RpcException.Trailers"/> только, если поймалось исключение.</para>
/// <see href="https://learn.microsoft.com/ru-ru/aspnet/core/grpc/interceptors?view=aspnetcore-10.0"/>.
/// </remarks>
public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;
    private readonly Guid _correlationId;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger)
    {
        _logger = logger;
        _correlationId = Guid.NewGuid();
    }

    // Унарный вызов
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    // Клиентский поток
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    // Серверный поток
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }

    // Двунаправленный поток
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (Exception e)
        {
            throw e.Handle(context, _logger, _correlationId);
        }
    }
}