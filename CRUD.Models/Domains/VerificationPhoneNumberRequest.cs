namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель запроса на подтверждение телефонного номера.
/// </summary>
public class VerificationPhoneNumberRequest : Request
{
    /// <summary>
    /// Код.
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Id пользователя.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Сущность пользователя.
    /// </summary>
    /// <remarks>
    /// Необходимо прогружать по <see cref="UserId"/>.
    /// </remarks>
    public User? User { get; set; }

    /// <summary>
    /// Проверяет срок действия кода.
    /// </summary>
    /// <returns><see langword="true"/>, если срок кода истёк.</returns>
    public bool IsExpired()
    {
        if (this.Expires < DateTime.UtcNow)
            return true;

        return false;
    }

    /// <summary>
    /// Проверяет можно ли отправить сообщение или необходим таймаут.
    /// </summary>
    /// <remarks>
    /// Выходной параметр <paramref name="timeout"/> содержит таймаут, чтобы при передачи аргумента в ошибку не нужно было искать константу.
    /// </remarks>
    /// <param name="options">Опции <see cref="VerificationPhoneNumberRequestOptions"/> (от туда берётся таймаут).</param>
    /// <param name="timeout">Таймаут, задаётся в любом случае.</param>
    /// <returns><see langword="true"/>, если необходим таймаут.</returns>
    public bool IsTimeout(VerificationPhoneNumberRequestOptions options, out TimeSpan timeout)
    {
        timeout = options.Timeout;

        if (this.CreatedAt.Add(timeout) > DateTime.UtcNow)
            return true;

        return false;
    }
}