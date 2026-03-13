namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции фоновой отправки писем.
/// </summary>
public class EmailSenderBackgroundServiceOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "EmailSenderBackgroundService";

    /// <summary>
    /// Сколько выделено SmtpClient'ов для обработки писем.
    /// </summary>
    /// <remarks>
    /// <para>Например, если три, то тогда сервис сможет обрабатывать три письма за раз.</para>
    /// <para>Ставить много не надо, иначе можно словить бан от mail.ru. В идеале иметь свой Smtp relay сервер.</para>
    /// </remarks>
    public required int SmtpClientsCount { get; set; }

    /// <summary>
    /// Сколько повторных попыток отправки может быть у письма.
    /// </summary>
    /// <remarks>
    /// Например, если вписано 0, то у письма не будет второго шанса.
    /// </remarks>
    public required int RetriesCount { get; set; }

    /// <summary>
    /// Таймаут по умолчанию у неотправленных писем.
    /// </summary>
    public required TimeSpan DefaultTimeout { get; set; }

    /// <summary>
    /// Коэффициент к таймауту при последующих неудачах.
    /// </summary>
    /// <remarks>
    /// Считается так: <c><see cref="DefaultTimeout"/> * КОЛИЧЕСТВО_НЕУДАЧ * <see cref="TimeoutCoefficient"/>.</c>
    /// </remarks>
    public required float TimeoutCoefficient { get; set; }

    /// <summary>
    /// Какое количество писем (включительно) можно отправить на одну и ту же электронную почту в течении <see cref="LIMIT_LETTERS_TIME"/>.
    /// </summary>
    /// <remarks>
    /// Например, 100, а <see cref="LimitLettersTime"/> = 3600 секунд. Значит не более 100 писем на одну электронную почту в час.
    /// </remarks>
    public required int LimitLetters { get; set; }

    /// <summary>
    /// В течении какого времени можно отправить <see cref="LimitLetters"/> писем, до достижения лимита.
    /// </summary>
    /// <remarks>
    /// Например, <see cref="LimitLetters"/> = 100, а <see cref="LimitLettersTime"/> = 3600 секунд. Значит не более 100 писем на одну электронную почту в час.
    /// </remarks>
    public required TimeSpan LimitLettersTime { get; set; }
}