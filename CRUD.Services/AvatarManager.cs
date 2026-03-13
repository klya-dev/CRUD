using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IAvatarManager"/>
public class AvatarManager : IAvatarManager
{
    private readonly IS3Manager _s3Manager;
    private readonly AvatarManagerOptions _options;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AvatarManager> _logger;
    private readonly IImageSingnatureChecker _imageSingnatureChecker;
    private readonly IValidator<User> _userValidator;

    public AvatarManager(IS3Manager s3Manager, IOptions<AvatarManagerOptions> options, ApplicationDbContext db, ILogger<AvatarManager> logger, IImageSingnatureChecker imageSingnatureChecker, IValidator<User> userValidator)
    {
        _s3Manager = s3Manager;
        _options = options.Value;
        _db = db;
        _logger = logger;
        _imageSingnatureChecker = imageSingnatureChecker;
        _userValidator = userValidator;
    }

    public async Task<ServiceResult<(Stream Stream, string FileExtension)>> GetAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.AvatarURL }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult<(Stream, string)>.Fail(ErrorMessages.UserNotFound);

        var key = userFromDb.AvatarURL;

        // Получаем файл из S3
        var result = await _s3Manager.GetObjectAsync(key, ct);

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult<(Stream, string)>.Fail(result.ErrorMessage);

        // Расширение без точки
        string extension = Path.GetExtension(userFromDb.AvatarURL).Remove(0, 1);

        return ServiceResult<(Stream, string)>.Success((result.Value!, extension));
    }

    public async Task<ServiceResult> SetAvatarAsync(Guid userId, Stream stream, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(stream);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Подходит ли сигнатура файла
        var fileInfo = _imageSingnatureChecker.IsFileValid(stream);
        if (!fileInfo.IsValid)
            return ServiceResult.Fail(ErrorMessages.DoesNotMatchSignature);

        // Соответствует ли размер файла
        if (stream.Length > _options.MaxFileSize)
            return ServiceResult.Fail(ErrorMessages.FileSizeLimitExceeded, args: _options.MaxFileSizeString);

        // Пользователь не найден
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        string pastAvatarURL = userFromDb.AvatarURL;

        // Создаём файл в S3
        var result = await CreateAvatarFileAsync(stream, fileInfo.FileExtension, CancellationToken.None); // Лучше будет, если вся логика пройдёт без отмены, иначе стопудово, что-то где-то отъебнёт

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage);

        userFromDb.AvatarURL = result.Value!;

        try
        {
            // Проверка валидности данных перед записью в базу
            var validationResultUser = await _userValidator.ValidateAsync(userFromDb, CancellationToken.None);
            if (!validationResultUser.IsValid) // Если данные невалидны, то я уже ничего не сделаю - исключение
            {
                // Удаляем напрасно созданный файл аватарки
                var deleteResult = await _s3Manager.DeleteObjectAsync(userFromDb.AvatarURL, CancellationToken.None);

                // Есть ошибка
                if (deleteResult.ErrorMessage != null)
                    _logger.LogError("Не удалось удалить напрасно созданную аватарку по причине: \"{error}\".", deleteResult.ErrorMessage);

                throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));
            }

            _db.Users.Update(userFromDb);
            await _db.SaveChangesAsync(CancellationToken.None);

            // Удаляем прошлую аватарку, если не дефолтная
            if (pastAvatarURL != _options.DefaultAvatarPath)
            {
                var resultDelete = await _s3Manager.DeleteObjectAsync(pastAvatarURL, CancellationToken.None);

                // Есть ошибка
                if (resultDelete.ErrorMessage != null)
                    return ServiceResult.Fail(resultDelete.ErrorMessage);
            }
        }
        catch (DbUpdateException ex)
        {
            // Не удалять прошлую аватарку (если исключение, то код, который связан с удалением всё равно не выполнится)
            // Откатить изменения в базе на прошлую аватарку (в базе останется значения после первого запроса)
            // Удалить новую аватарку, т.к пользователь с ней никак не связан, она ошибочно созданная
            if (DbExceptionHelper.IsConcurrencyConflict(ex))
            {
                // https://learn.microsoft.com/ru-ru/ef/core/saving/concurrency?tabs=data-annotations#resolving-concurrency-conflicts

                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is User)
                    {
                        var proposedValues = entry.CurrentValues;
                        var databaseValues = await entry.GetDatabaseValuesAsync(CancellationToken.None);

                        if (databaseValues == null)
                            throw new NotSupportedException($"{nameof(databaseValues)} является null, скорее всего сущности больше нет в базе данных, не удалось разрешить конфликт для \"{entry.Metadata.Name}\"");

                        foreach (var property in proposedValues.Properties)
                        {
                            var proposedValue = proposedValues[property];
                            var databaseValue = databaseValues[property];

                            // TODO: decide which value should be written to database
                            // proposedValues[property] = <value to be saved>;
                            proposedValues[property] = databaseValue; // Остаётся значение, которое было в базе (первый запрос обновил данные, а второй согласился, грубо говоря)

                            // Удаляем напрасно созданный файл аватарки
                            // При каждом запросе создаётся новая аватарка, а при конфликте эту новую аватарку от второго запроса никто не удаляет, исправляем
                            if (property.Name == nameof(User.AvatarURL))
                                await _s3Manager.DeleteObjectAsync(proposedValue!.ToString()!, CancellationToken.None);
                        }

                        // Refresh original values to bypass next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                    else
                        throw new NotSupportedException($"Не удалось разрешить конфликт для \"{entry.Metadata.Name}\"");
                }

                return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);
            }

            throw;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAvatarAsync(string avatarUrl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(avatarUrl);

        // Если у пользователя дефолтная аватарка
        if (avatarUrl == _options.DefaultAvatarPath)
            return ServiceResult.Success();

        // Удаляем аватарку
        var result = await _s3Manager.DeleteObjectAsync(avatarUrl, ct);

        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult.Fail(result.ErrorMessage);

        return ServiceResult.Success();
    }

    /// <summary>
    /// Создаёт файл аватарки в S3.
    /// </summary>
    /// <param name="stream">Поток файла.</param>
    /// <param name="fileExtension">Расширение без точки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="stream"/> <see langword="null"/>.</exception>
    /// <returns><see cref="ServiceResult"/> результат сервиса с ключом объекта S3.</returns>
    private async Task<ServiceResult<string>> CreateAvatarFileAsync(Stream stream, string fileExtension, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var guid = Guid.NewGuid();
        var key = $"{_options.AvatarsInS3Directory}/{guid}.{fileExtension}";

        // Создаём файл
        var result = await _s3Manager.CreateObjectAsync(stream, key, ct);
        
        // Есть ошибка
        if (result.ErrorMessage != null)
            return ServiceResult<string>.Fail(result.ErrorMessage);

        return ServiceResult<string>.Success(key);
    }
}