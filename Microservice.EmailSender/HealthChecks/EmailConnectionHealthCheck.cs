using MailKit.Net.Smtp;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microservice.EmailSender.HealthChecks;

/// <summary>
/// Проверяет подключение к почтовому серверу.
/// </summary>
public class EmailConnectionHealthCheck : IHealthCheck
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailConnectionHealthCheck> _logger;

    public EmailConnectionHealthCheck(IEmailSender emailSender, ILogger<EmailConnectionHealthCheck> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        SmtpClient? smtpClient = null;
        try
        {
            smtpClient = await _emailSender.ConnectAsync(cancellationToken);

            bool isHealthy = smtpClient.IsConnected;
            return isHealthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Failed to connect to the email server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось подключится к Email серверу по причине: {message}.", ex.Message);
            return HealthCheckResult.Unhealthy("Failed to connect to the email server.");
        }
        finally
        {
            if (smtpClient != null)
                await smtpClient.DisconnectAsync(true, CancellationToken.None);
        }
    }
}