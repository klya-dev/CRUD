namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с Refresh-токенами аутентификации.
/// </summary>
public interface IAuthRefreshTokenManager
{
    /// <summary>
    /// Добавляет в базу новый Refresh-токен и удаляет старые, пока количество токенов не станет <see cref="AuthOptions.MaxCountRefreshTokens"/> (если их изначально больше).
    /// </summary>
    /// <remarks>
    /// <para>Если указан <paramref name="usedRefreshToken"/>, то он удаляется.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="newRefreshToken"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="AuthRefreshToken"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="newRefreshToken">Новый Refresh-токен.</param>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="usedRefreshToken">Использованный Refresh-токен.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="newRefreshToken"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="AuthRefreshToken"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <returns></returns>
    Task AddRefreshTokenAndDeleteOldersAsync(string newRefreshToken, Guid userId, string? usedRefreshToken = null, CancellationToken ct = default);
}