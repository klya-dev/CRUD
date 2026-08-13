namespace CRUD.Models;

/// <summary>
/// Полезная нагрузка токена на смену пароля.
/// </summary>
public record class ChangePasswordPayload
{
    /// <summary>
    /// Хэш нового пароля.
    /// </summary>
    public required string HashedNewPassword { get; set; }

    /// <summary>
    /// Id пользователя.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Проверяет можно ли отправить запрос или необходим таймаут.
    /// </summary>
    /// <remarks>
    /// Выходной параметр <paramref name="timeout"/> содержит таймаут, чтобы при передачи аргумента в ошибку не нужно было искать значение таймаута.
    /// </remarks>
    /// <param name="options">Опции <see cref="ChangePasswordRequestOptions"/> (от туда берётся таймаут).</param>
    /// <param name="timeout">Таймаут, задаётся в любом случае.</param>
    /// <returns><see langword="true"/>, если необходим таймаут.</returns>
    public static bool IsTimeout(ChangePasswordRequestOptions options, DateTime createdAt, out TimeSpan timeout)
    {
        timeout = options.Timeout;

        if (createdAt.Add(timeout) > DateTime.UtcNow)
            return true;

        return false;
    }
}