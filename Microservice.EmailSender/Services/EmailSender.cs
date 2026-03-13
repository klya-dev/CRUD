using MailKit.Net.Smtp;
using MimeKit;

namespace Microservice.EmailSender.Services;

/// <inheritdoc cref="IEmailSender"/>
public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpServerOptions _options;

    public EmailSender(IOptions<SmtpServerOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SmtpClient> ConnectAsync(CancellationToken ct = default)
    {
        var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await smtpClient.AuthenticateAsync(_options.Email, _options.AuthPassword, ct);

        return smtpClient;
    }

    public void Connect(SmtpClient smtpClient)
    {
        ArgumentNullException.ThrowIfNull(smtpClient);

        smtpClient.Connect(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls);
        smtpClient.Authenticate(_options.Email, _options.AuthPassword);
    }

    public async Task DisconnectAsync(SmtpClient smtpClient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(smtpClient);

        await smtpClient.DisconnectAsync(true, ct);
    }

    public async Task<bool> SendEmailAsync(Letter letter, SmtpClient smtpClient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(letter);
        ArgumentNullException.ThrowIfNull(smtpClient);

        // Формируем письмо
        var emailToSend = new MimeMessage();
        emailToSend.From.Add(new MailboxAddress(_options.Name, _options.Email));
        emailToSend.To.Add(MailboxAddress.Parse(letter.Email));
        emailToSend.Subject = letter.Subject;
        emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = letter.Body
        };

        // Отправляем
        try
        {
            await smtpClient.SendAsync(emailToSend, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Во время отправки письма \"{id}\" возникла ошибка: {ex}.", letter.Id, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(Letter letter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(letter);

        // Формируем письмо
        var emailToSend = new MimeMessage();
        emailToSend.From.Add(new MailboxAddress(_options.Name, _options.Email));
        emailToSend.To.Add(MailboxAddress.Parse(letter.Email));
        emailToSend.Subject = letter.Subject;
        emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = letter.Body
        };

        // Отправляем
        using (var smtpClient = new SmtpClient())
        {
            await smtpClient.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls, ct);
            await smtpClient.AuthenticateAsync(_options.Email, _options.AuthPassword, ct);
            try
            {
                await smtpClient.SendAsync(emailToSend, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Во время отправки письма \"{id}\" возникла ошибка: {ex}.", letter.Id, ex.Message);
                return false;
            }
            finally
            {
                await smtpClient.DisconnectAsync(true, ct);
            }
        }
    }
}