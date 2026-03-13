using Grpc.Core;

namespace Microservice.EmailSender.Services.Grpc;

/// <summary>
/// Реализация gRPC EmailSender сервиса.
/// </summary>
public class GrpcEmailSenderService : GrpcEmailSender.GrpcEmailSenderBase
{
    private readonly ILogger<GrpcEmailSenderService> _logger;
    private readonly IQueueEmail _queueEmail;

    public GrpcEmailSenderService(ILogger<GrpcEmailSenderService> logger, IQueueEmail queueEmail)
    {
        _logger = logger;
        _queueEmail = queueEmail;
    }

    public override async Task<EnqueueLetterReply> Enqueue(EnqueueLetterRequest letterRequest, ServerCallContext context)
    {
        //await Task.Delay(5000);
        //throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
        //throw new NotImplementedException();

        // Не удалось пропарсить Guid
        if (!Guid.TryParse(letterRequest.Id, out Guid letterId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect GUID."));

        var ct = context.CancellationToken;

        // Добавляем письмо в очередь
        var letter = new Letter(letterId, letterRequest.Email, letterRequest.Subject, letterRequest.Body);
        await _queueEmail.EnqueueAsync(letter, ct);
        _logger.LogInformation("Письмо \"{id}\" успешно поставлено в очередь на отправку.", letterRequest.Id);

        // Возвращаем успешный результат
        return new EnqueueLetterReply
        {

        };
    }
}