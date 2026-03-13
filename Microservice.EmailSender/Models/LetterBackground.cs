namespace Microservice.EmailSender.Models;

/// <summary>
/// Письмо для электронной почты с элементами необходимыми для фоновой отправки.
/// </summary>
public class LetterBackground : Letter
{
    /// <summary>
    /// Конструктор для создания <see cref="LetterBackground"/> по <see cref="Letter"/>.
    /// </summary>
    /// <param name="letter">Электронное письмо.</param>
    public LetterBackground(Letter letter) : base(letter.Id, letter.Email, letter.Subject, letter.Body) { }

    /// <summary>
    /// Количество неуспешных отправок письма.
    /// </summary>
    /// <remarks>
    /// По умолчанию 0.
    /// </remarks>
    public int ErrorCount { get; private set; } = 0;

    /// <summary>
    /// Должно ли письмо ждать таймаут.
    /// </summary>
    /// <remarks>
    /// По умолчанию <see langword="false"/>
    /// </remarks>
    public bool IsShouldWaitTimeout { get; private set; } = false;

    /// <summary>
    /// Инкрементировать количество неуспешных отправок письма.
    /// </summary>
    /// <remarks>
    /// К <see cref="ErrorCount"/> прибавляется 1.
    /// </remarks>
    public void IncrementError() => ErrorCount++;

    /// <summary>
    /// Письмо должно подождать таймаут.
    /// </summary>
    /// <remarks>
    /// <see cref="IsShouldWaitTimeout"/> присваивается <see langword="true"/>.
    /// </remarks>
    public void ShouldWaitTimeout() => IsShouldWaitTimeout = true;

    /// <summary>
    /// Ожидает таймаут письма, где <paramref name="timeout"/> - это таймаут по умолчанию, а <paramref name="coefficient"/> - коэффициент.
    /// </summary>
    /// <remarks>
    /// <para>Считается так: <c><paramref name="timeout"/> * <see cref="ErrorCount"/> * <paramref name="coefficient"/></c>.</para>
    /// <para><see cref="IsShouldWaitTimeout"/> присваивается <see langword="false"/>, т.к таймаут отработал. Для следующего таймаута нужно снова поставить <see langword="true"/>.</para>
    /// </remarks>
    /// <param name="timeout">Таймаут по умолчанию.</param>
    /// <param name="coefficient">Коэффициент.</param>
    /// <returns></returns>
    public async Task WaitErrorTimeout(int timeout, float coefficient, CancellationToken ct = default)
    {
        // Если письмо не должено ждать таймаут
        if (!IsShouldWaitTimeout)
            return;

        // Первая неудача без коэффициента
        if (ErrorCount == 1)
            await Task.Delay(timeout, ct); // А далее с коэффициентом
        else if (ErrorCount > 0)
        {
            int delay = (int)Math.Round(timeout * ErrorCount * coefficient);
            await Task.Delay(delay, ct);
        }

        IsShouldWaitTimeout = false; // Таймаут отработал. Для следующего таймаута нужно снова поставить true
    }
}