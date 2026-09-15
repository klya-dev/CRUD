using Amazon.S3.Model;

namespace CRUD.Infrastructure.S3;

/// <summary>
/// Сервис для работы с S3.
/// </summary>
public interface IS3Manager
{
    /// <summary>
    /// Получает объект по ключу.
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
    /// <term>Объект не найден</term>
    /// <description><see cref="ErrorMessages.FileNotFound"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="options">Дополнительные опции к запросу.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="key"/> <see cref="string.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса c <see cref="S3FileContent"/> объекта.</returns>
    Task<ServiceResult<S3FileContent>> GetObjectAsync(string key, Action<GetObjectRequest>? options = null, CancellationToken ct = default);

    /// <summary>
    /// Получает временный URL объекта для его получения.
    /// </summary>
    /// <remarks>
    /// <para>Дата истечения срока действия URL по умолчанию час (<c>DateTime.UtcNow.AddHours(1)</c>).</para>
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
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="expires">Дата истечения срока действия URL.</param>
    /// <param name="options">Дополнительные опции к запросу.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="key"/> <see cref="string.Empty"/>.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса cо строкой URL объекта.</returns>
    Task<ServiceResult<string>> GetPresignedUrlAsync(string key, DateTime? expires = null, Action<GetPreSignedUrlRequest>? options = null);

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
    /// <term>Если <paramref name="sourceKey"/> <see cref="string.Empty"/></term>
    /// <description>исключение <see cref="ArgumentException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="destinationKey"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// <item>
    /// <term>Если <paramref name="destinationKey"/> <see cref="string.Empty"/></term>
    /// <description>исключение <see cref="ArgumentException"/>.</description>
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
    /// <param name="options">Дополнительные опции к запросу.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="sourceKey"/> или <paramref name="destinationKey"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="sourceKey"/> или <paramref name="destinationKey"/> <see cref="string.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult<S3OperationResult>> CopyObjectAsync(string sourceKey, string destinationKey, Action<CopyObjectRequest>? options = null, CancellationToken ct = default);

    /// <summary>
    /// Создаёт объект по потоку и по ключу.
    /// </summary>
    /// <remarks>
    /// <para>Если <paramref name="stream"/> <see langword="null"/>, то создатся пустой объект ("папка"), что дурной тон.</para>
    /// <para>Если <paramref name="checkExists"/> <see langword="true"/> выполнится проверка на существование объекта, и если объект существует, то <see cref="ErrorMessages.FileAlreadyExists"/>.</para>
    /// <para>Если <paramref name="checkExists"/> <see langword="false"/>, то объект будет перезапишен.</para>
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
    /// <param name="stream">Поток файла.</param>
    /// <param name="options">Дополнительные опции к запросу.</param>
    /// <param name="checkExists">Проверять ли существование объекта, если объект существует, то <see cref="ErrorMessages.FileAlreadyExists"/>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="key"/> <see cref="string.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult<S3OperationResult>> CreateObjectAsync(string key, Stream? stream = null, Action<PutObjectRequest>? options = null, bool checkExists = true, CancellationToken ct = default);

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
    /// <item>
    /// <term>Если <paramref name="key"/> <see cref="string.Empty"/></term>
    /// <description>исключение <see cref="ArgumentException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// Возможные ошибки сервиса:
    /// <list type="bullet">
    /// <item>
    /// <term>Конфликт параллельности</term>
    /// <description><see cref="ErrorMessages.ConcurrencyConflicts"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="options">Дополнительные опции к запросу.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="key"/> <see cref="string.Empty"/>.</exception>
    /// <exception cref="OperationCanceledException">Если операция отменена.</exception>
    /// <exception cref="Amazon.S3.AmazonS3Exception">Непредвиденное исключение.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса.</returns>
    Task<ServiceResult<S3OperationResult>> DeleteObjectAsync(string key, Action<DeleteObjectRequest>? options = null, CancellationToken ct = default);

    /// <summary>
    /// Проверяет существует ли объект по ключу.
    /// </summary>
    /// <remarks>
    /// <para><paramref name="key"/> указывать без начального '/' ("avatars/default.png").</para>
    /// <para>Если <paramref name="key"/> это пустой объект ("папка"), то необходимо указать '/' в конце ("avatars/").</para>
    /// <para>Обязательно нужно знать разницу между пустым объектом ("папкой") и "визуальной" папкой в UI.</para>
    /// <para>Это метод проверяет конкретно объект по его метаданным, так как задумавалось S3, определения папок вообще нет, только ОБЪЕКТ в понятии объектного хранилища.</para>
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
    /// </remarks>
    /// <param name="key">Ключ. Например, "avatars/default.png".</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="key"/> <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="key"/> <see cref="string.Empty"/>.</exception>
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