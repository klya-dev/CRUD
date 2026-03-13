namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель запроса на смену пароля.
/// </summary>
public class ChangePasswordRequest : Request
{
    /// <summary>
    /// Хэш нового пароля.
    /// </summary>
    public required string HashedNewPassword { get; set; }

    /// <summary>
    /// Токен.
    /// </summary>
    public required string Token { get; set; }

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
    /// Проверяет срок действия токена.
    /// </summary>
    /// <returns><see langword="true"/>, если токен истёк.</returns>
    public bool IsExpired()
    {
        if (this.Expires < DateTime.UtcNow)
            return true;

        return false;
    }

    /// <summary>
    /// Проверяет можно ли отправить запрос или необходим таймаут.
    /// </summary>
    /// <remarks>
    /// Выходной параметр <paramref name="timeout"/> содержит таймаут, чтобы при передачи аргумента в ошибку не нужно было искать значение таймаута.
    /// </remarks>
    /// <param name="options">Опции <see cref="ChangePasswordRequestOptions"/> (от туда берётся таймаут).</param>
    /// <param name="timeout">Таймаут, задаётся в любом случае.</param>
    /// <returns><see langword="true"/>, если необходим таймаут.</returns>
    public bool IsTimeout(ChangePasswordRequestOptions options, out TimeSpan timeout)
    {
        timeout = options.Timeout;

        if (this.CreatedAt.Add(timeout) > DateTime.UtcNow)
            return true;

        return false;
    }
}