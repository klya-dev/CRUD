namespace CRUD.Infrastructure.S3;

/// <summary>
/// Сервис для работы с S3.
/// </summary>
public interface IS3Manager
{
    /// <summary>
    /// Получает объект потоком по ключу.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="key"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="key"/> <see cref="string.Empty"/></term>
    /// <description>исключение <see cref="ArgumentException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Файл не найден</term>
    /// <description><see cref="ErrorMessages.FileNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса cо <see cref="Stream"/> файла.</returns>
    Task<ServiceResult<Stream>> GetObjectAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Копирует указанный объект в указанное место.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="sourceKey"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="destinationKey"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Объект не найден</term>
    /// <description><see cref="ErrorMessages.FileNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Конфликт параллельности</term>
    /// <description><see cref="ErrorMessages.ConcurrencyConflicts"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="sourceKey">Ключ объекта источника.</param>
    /// <param name="destinationKey">Ключ места назначения.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="sourceKey"/> или <paramref name="destinationKey"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> CopyObjectAsync(string sourceKey, string destinationKey, CancellationToken ct = default);

    /// <summary>
    /// Создаёт объект по потоку и по ключу.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="stream"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="key"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Объект уже существует</term>
    /// <description><see cref="ErrorMessages.FileAlreadyExists"/>.</description>
    /// </item>
    /// <item>
    /// <term>Конфликт параллельности</term>
    /// <description><see cref="ErrorMessages.ConcurrencyConflicts"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="stream">Поток файла.</param>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="stream"/> или <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> CreateObjectAsync(Stream stream, string key, CancellationToken ct = default);

    /// <summary>
    /// Создаёт объект без контента по ключу, подойдёт для "папок".
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="key"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Объект уже существует</term>
    /// <description><see cref="ErrorMessages.FileAlreadyExists"/>.</description>
    /// </item>
    /// <item>
    /// <term>Конфликт параллельности</term>
    /// <description><see cref="ErrorMessages.ConcurrencyConflicts"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> CreateObjectAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Удаляет объект по ключу.
    /// </summary>
    /// <remarks>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="key"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Объект не найден</term>
    /// <description><see cref="ErrorMessages.FileNotFound"/>.</description>
    /// </item>
    /// <item>
    /// <term>Конфликт параллельности</term>
    /// <description><see cref="ErrorMessages.ConcurrencyConflicts"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult> DeleteObjectAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Проверяет существует ли объект по ключу.
    /// </summary>
    /// <remarks>
    /// <para><paramref name="key"/> указывать без начального '/'.</para>
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="key"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если объект существует.</returns>
    Task<bool> IsObjectExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Проверяет подключение к S3.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <returns><see langword="true"/>, если удалось подключиться.</returns>
    Task<bool> CheckConnectionAsync(CancellationToken ct = default);
}