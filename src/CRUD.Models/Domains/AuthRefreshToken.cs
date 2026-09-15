namespace CRUD.Models.Domains;

/// <summary>
/// Domain модель токена обновления для аутентификации.
/// </summary>
public class AuthRefreshToken
{
    /// <summary>
    /// Id токена.
    /// </summary>
    /// <remarks>
    /// Генерируется при создании экземпляра, автоматически.
    /// </remarks>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Токен обновления.
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
    /// Срок истечения токена.
    /// </summary>
    public required DateTime Expires { get; set; }

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
}