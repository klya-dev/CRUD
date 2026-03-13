namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для работы с аватарками.
/// </summary>
public interface IAvatarManager
{
    /// <summary>
    /// Получает аватарку пользователя потоком по его Id.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Файл не найден</term>
    /// <description><see cref="ErrorMessages.FileNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса cо <see cref="Stream"/> файла и расширением файла (без точки).</returns>
    Task<ServiceResult<(Stream Stream, string FileExtension)>> GetAvatarAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает аватарку пользователю.
    /// </summary>
    /// <remarks>
    /// <para>Для валидации <see cref="User"/> используется <see cref="IValidator{User}"/>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="stream"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="userId"/> является <see cref="Guid.Empty"/></term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна, изменения не последуют</term>
    /// <description>исключение <see cref="InvalidOperationException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если возник конфликт параллельности</term>
    /// <description>исключение <see cref="DbUpdateConcurrencyException"/> | <see cref="DbUpdateException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Не подходит сигнатура файла</term>
    /// <description><see cref="ErrorMessages.DoesNotMatchSignature"/>.</description>
    /// </item>
    /// <item>
    /// <term>Достигнут допустимый размер файла</term>
    /// <description><see cref="ErrorMessages.FileSizeLimitExceeded"/>.</description>
    /// </item>
    /// <item>
    /// <term>Пользователь не найден</term>
    /// <description><see cref="ErrorMessages.UserNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="userId">Id пользователя.</param>
    /// <param name="stream">Поток файла.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="stream"/> <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если <paramref name="userId"/> является <see cref="Guid.Empty"/> или если после изменений данных сущности <see cref="User"/>, сущность окажется невалидна.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Если возник конфликт параллельности.</exception>
    /// <exception cref="DbUpdateException">Если возник конфликт параллельности.</exception>
    /// <exception cref="NotSupportedException">Если не удалось разрешить конфликт параллельности.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> SetAvatarAsync(Guid userId, Stream stream, CancellationToken ct = default);

    /// <summary>
    /// Удаляет аватарку пользователя, если она не равна <see cref="AvatarManagerOptions.DefaultAvatarPath"/>.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="avatarUrl"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Все возможные ошибки из</term>
    /// <description><see cref="IS3Manager.DeleteObjectAsync(string, CancellationToken)"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="avatarUrl">Путь до аватарки (<see cref="User.AvatarURL"/>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="avatarUrl"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> DeleteAvatarAsync(string avatarUrl, CancellationToken ct = default);
}