using Grpc.Core;
using Grpc.Net.ClientFactory;

namespace CRUD.Services;

/// <inheritdoc cref="IQueueEmail"/>
public class QueueEmail : IQueueEmail
{
    private readonly GrpcEmailSender.GrpcEmailSenderClient _client;
    private readonly ILogger<QueueEmail> _logger;

    public QueueEmail(GrpcClientFactory grpcClientFactory, ILogger<QueueEmail> logger)
    {
        _client = grpcClientFactory.CreateClient<GrpcEmailSender.GrpcEmailSenderClient>(GrpcClientNames.GrpcEmailSender);
        _logger = logger;
    }

    public async Task<StatusCode> EnqueueAsync(Letter letter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(letter);

        try
        {
            var request = new EnqueueLetterRequest
            {
                Id = letter.Id.ToString(),
                Email = letter.Email,
                Subject = letter.Subject,
                Body = letter.Body
            };

            // Отправляем запрос
            var response = await _client.EnqueueAsync(request, deadline: DateTime.UtcNow.AddSeconds(3), cancellationToken: ct); // Не больше 3 секунд на запрос, иначе исключение (RpcException, StatusCode.DeadlineExceeded)

            //using var call = _client.EnqueueAsync(request, deadline: DateTime.UtcNow.AddSeconds(3), cancellationToken: ct); // Запрос
            //var response = await call.ResponseAsync; // Ответ EnqueueLetterReply
            //var statusCode = call.GetStatus().StatusCode; // Статус код
            //var trailers = call.GetTrailers(); // Трейлеры

            _logger.LogInformation("Письмо \"{id}\" успешно поставлено в очередь на отправку.", letter.Id);

            // Если статус код не StatusCode.OK, то всегда генерируется исключение (https://github.com/grpc/grpc-dotnet/issues/1538)
            // Поэтому нет смысла проверять статус на успешность - он всегда будет успешным. А если неуспешный, то исключение

            // Возвращаем статус код
            return StatusCode.OK;
        }
        catch (RpcException ex) // Исключения сервера, и могут быть мои, например DeadlineExceeded (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/error-handling?view=aspnetcore-10.0#error-scenarios)
        {
            var correlationId = ex.Trailers.FirstOrDefault(x => x.Key == "correlationid")?.Value;
            _logger.LogError(ex, "Не удалось поставить письмо в очередь на отправку по причине \"{message}\". CorrelationId: {correlationId}.", ex.Message, correlationId);
            return ex.StatusCode;
        }
    }
}