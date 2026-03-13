using MailKit.Net.Smtp;

namespace Microservice.EmailSender.Services.EmailSenderBackground;

/// <inheritdoc cref="IEmailSenderBackgroundCore"/>
public class EmailSenderBackgroundCore : IEmailSenderBackgroundCore
{
    private readonly IEmailSender _emailSender;
    private readonly EmailSenderBackgroundServiceOptions _options;
    private readonly ILogger<EmailSenderBackgroundCore> _logger;
    private readonly Dictionary<string, List<DateTime>> _lettersAnalytics;

    /// <summary>
    /// Очередь писем.
    /// </summary>
    private IQueueEmail Queue { get; }

    public EmailSenderBackgroundCore(IQueueEmail queueEmail, IEmailSender emailSender, IOptions<EmailSenderBackgroundServiceOptions> options, ILogger<EmailSenderBackgroundCore> logger)
    {
        Queue = queueEmail;

        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;

        // Список электронных почт с датами отправки
        _lettersAnalytics = [];
    }

    public async Task<List<SmtpClient>> CreateSmtpClientsAsync(CancellationToken ct = default)
    {
        // Создаём несколько SmtpClient'ов
        var smtpClients = new List<SmtpClient>();
        for (int i = 0; i < _options.SmtpClientsCount; i++)
        {
            var smtpClient = await _emailSender.ConnectAsync(ct);

            // Если отключился, то подключаемся снова
            smtpClient.Disconnected += (o, e) =>
            {
                Disconnected(o, e);
            };

            smtpClients.Add(smtpClient);
        }

        return smtpClients;
    }

    private void Disconnected(object? o, MailKit.DisconnectedEventArgs e)
    {
        o ??= new SmtpClient();

        _logger.LogWarning("Выполняю переподключение SmtpClient.");
        _emailSender.Connect((SmtpClient)o);

        if (((SmtpClient)o).IsConnected == false)
            _logger.LogError("Не удалось выполнить переподключение SmtpClient.");
    }

    public async Task DoWorkAsync(List<SmtpClient> smtpClients, CancellationToken ct)
    {
        var processingTasks = new List<Task>();

        // Запускаем обработчики для каждого SMTP клиента
        foreach (var smtpClient in smtpClients)
            processingTasks.Add(ProcessAsync(smtpClient, ct));

        // Ждём завершения всех обработчиков
        await Task.WhenAll(processingTasks);
    }

    private async Task ProcessAsync(SmtpClient smtpClient, CancellationToken ct = default)
    {
        await foreach (var letter in Queue.DequeueAllAsync(ct))
        {
            _logger.LogDebug("Письмо \"{id}\" достано из очереди.", letter.Id);

            if (ct.IsCancellationRequested)
                break;

            // Если письмо не отправлось ранее определённое количество раз, убираем возможность повторно отправить
            if (letter.ErrorCount - 1 >= _options.RetriesCount) // Не учитываем первую отправка (не повторную)
            {
                _logger.LogDebug("Письмо \"{id}\" достигло максимальное число повторных попыток. Количество неудачных попыток: {count}.", letter.Id, letter.ErrorCount);
                continue;
            }

            // Если у письма есть неудачи и письмо уже проожидало предыдущий таймаут, то в другом потоке ждём таймаут, а после добавляем в очередь
            if (letter.ErrorCount > 0 && letter.IsShouldWaitTimeout)
            {
                _logger.LogDebug("Письмо \"{id}\" имеет неудачные попытки ({count}) и оно должно ожидать таймаут.", letter.Id, letter.ErrorCount);

                _ = Task.Run(async () =>
                {
                    await letter.WaitErrorTimeout(_options.DefaultTimeout.Microseconds, _options.TimeoutCoefficient, ct);
                    await Queue.EnqueueAsync(letter, ct);
                    _logger.LogDebug("Письмо \"{id}\" подождало таймаут и было добавлено в очередь.", letter.Id, letter.ErrorCount);
                }, ct);

                continue;
            }

            // Очищаем прошедший час и проверяем лимит
            if (_lettersAnalytics.TryGetValue(letter.Email, out var letterAnalyticsDates))
            {
                var firstDate = letterAnalyticsDates[0]; // Первая дата отправки письма
                if (firstDate.Add(_options.LimitLettersTime) < DateTime.UtcNow) // Если прошло более часа
                    _lettersAnalytics.Remove(letter.Email); // Удаляем почту из словаря и считаем заново

                // Если электронная почта достигла лимита писем в час, то просто не пускаем дальше
                if (IsRateLimit(letterAnalyticsDates))
                {
                    _logger.LogDebug("Письмо \"{id}\" не будет отправлено по причине лимита писем в час на этот адрес электронной почты.", letter.Id);
                    continue;
                }
            }

            try
            {
                bool isSend = await _emailSender.SendEmailAsync(letter, smtpClient, ct);
                _logger.LogDebug("Письмо \"{id}\". Удалось отправить: {result}.", letter.Id, isSend);

                // Не удалось отправить письмо
                if (!isSend)
                {
                    // Если письмо не отправлось ранее определённое количество раз, и сейчас не отправилось, убираем возможность повторно отправить
                    if (letter.ErrorCount - 1 >= _options.RetriesCount) // -1, т.к количество неудачных отправок и количество повторных попыток не одно и тоже. Поэтому один - это первая отправка, не повторная
                    {
                        _logger.LogDebug("Письмо \"{id}\" достигло максимальное число повторных попыток. Количество неудачных попыток: {count}.", letter.Id, letter.ErrorCount);
                        continue;
                    }

                    letter.IncrementError();
                    await Queue.EnqueueAsync(letter, ct); // Добавляем в конец очереди с пометкой, что письмо не отправилось с первого раза
                    letter.ShouldWaitTimeout(); // И письмо должно подождать таймаут
                    _logger.LogDebug("Письмо \"{id}\" снова добавлено в очередь. Количество неудачных попыток стало: {count}. Письмо должно ждать таймаут: {shouldWait}.", letter.Id, letter.ErrorCount, letter.IsShouldWaitTimeout);
                }

                // Добавляем в аналитику отправленное/неотправленное письмо
                if (_lettersAnalytics.TryGetValue(letter.Email, out var dateTimes)) // Почта в словаре есть, добавляем новую дату
                    dateTimes.Add(DateTime.UtcNow);
                else // Почты в словаре нет, добавляем почту и задаём первую дату отправки
                    _lettersAnalytics.Add(letter.Email, [DateTime.UtcNow]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Во время отправки письма \"{id}\" произошла ошибка: {ex}.", letter.Id, ex.Message);
            }
        }
    }

    /// <summary>
    /// Проверяет достигла ли почта лимита по её датам отправки.
    /// </summary>
    /// <param name="dateTimes">Даты отправки писем на эту электронную почту.</param>
    /// <returns><see langword="true"/>, если достигла.</returns>
    private bool IsRateLimit(List<DateTime> dateTimes)
    {
        // Дата первой отправки
        var firstDate = dateTimes[0];

        // Если с момента первой отправки прошло менее часа, а количество отправок больше лимита (включительно)
        if (firstDate.Add(_options.LimitLettersTime) > DateTime.UtcNow && dateTimes.Count >= _options.LimitLetters)
            return true; // Электронная почта достигла лимита

        return false;
    }
}