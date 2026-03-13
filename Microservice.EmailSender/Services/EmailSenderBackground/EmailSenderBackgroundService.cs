namespace Microservice.EmailSender.Services.EmailSenderBackground;

/// <summary>
/// Сервис для отправки писем в фоне.
/// </summary>
public class EmailSenderBackgroundService : BackgroundService
{
    private readonly IEmailSenderBackgroundCore _emailSenderBackgroundCore;
    private readonly ILogger<EmailSenderBackgroundService> _logger;

    public EmailSenderBackgroundService(IEmailSenderBackgroundCore emailSenderBackgroundCore, ILogger<EmailSenderBackgroundService> logger)
    {
        _emailSenderBackgroundCore = emailSenderBackgroundCore;
        _logger = logger;

        _logger.StartedBackgroundServiceLog(nameof(EmailSenderBackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Создаём несколько SmtpClient'ов
        var smtpClients = await _emailSenderBackgroundCore.CreateSmtpClientsAsync(ct);

        try
        {
            await _emailSenderBackgroundCore.DoWorkAsync(smtpClients, ct);
        }
        catch (OperationCanceledException)
        {
            _logger.StopedBackgroundServiceLog(nameof(EmailSenderBackgroundService));
        }
    }
}