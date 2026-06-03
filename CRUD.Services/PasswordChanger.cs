using CRUD.Models.Dtos.Password;
using CRUD.Services.Interfaces;

namespace CRUD.Services;

/// <inheritdoc cref="IPasswordChanger"/>
public sealed class PasswordChanger : IPasswordChanger
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<ChangePasswordDto> _changePasswordDtoValidator;
    private readonly IValidator<SetPasswordDto> _setPasswordDtoValidator;
    private readonly IChangePasswordRequestManager _changePasswordRequestManager;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordChanger(ApplicationDbContext db, IValidator<ChangePasswordDto> changePasswordDtoValidator, IValidator<SetPasswordDto> setPasswordDtoValidator, IChangePasswordRequestManager changePasswordRequestManager, IPasswordHasher passwordHasher)
    {
        _db = db;
        _changePasswordDtoValidator = changePasswordDtoValidator;
        _setPasswordDtoValidator = setPasswordDtoValidator;
        _changePasswordRequestManager = changePasswordRequestManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(changePasswordDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _changePasswordDtoValidator.ValidateAsync(changePasswordDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(ChangePasswordDto), validationResult.Errors));

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.HashedPassword, x.Id, x.Email, x.LanguageCode }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Проверка пароля
        if (!_passwordHasher.Verify(changePasswordDto.Password, userFromDb.HashedPassword))
            return ServiceResult.Fail(ErrorMessages.InvalidPassword);

        // Добавляем токен в базу и отправляем письмо
        var resultAddTokenToDbAndSendLetter = await _changePasswordRequestManager.AddTokenToDatabaseAndSendLetterAsync(userFromDb.Id, userFromDb.Email, userFromDb.LanguageCode,
            _passwordHasher.GenerateHashedPassword(changePasswordDto.NewPassword), ct);

        // Есть ошибка
        if (resultAddTokenToDbAndSendLetter.ErrorMessage != null)
            return ServiceResult.Fail(resultAddTokenToDbAndSendLetter.ErrorMessage, resultAddTokenToDbAndSendLetter.ErrorParams);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ChangePasswordAsync(string token, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(token);

        // Запрос не найден
        var changePasswordRequestFromDb = await _db.ChangePasswordRequests.Where(x => x.Token == token)
            .AsNoTracking()
            .Select(x => new
            {
                // Очень осторожно с полной сущностью (Request = x), если где-то загрузили/создали всю сущность, а потом вызвали ExecuteUpdateAsync, то из-за кэша значения могут разниться
                // Прикол, в том, что EF и вправду делает запрос в базу (FirstOrDefaultAsync), но после получения этих данных он сравнивает ID сущности с ID сущности в кэше и просто отдаёт кэшированные значения - так и работает ChangeTracker (в одном контексте базы)
                // Поэтому когда грузим всю сущность через .Select() - .AsNoTracking() обязательно
                Request = x,
                User = new { x.User!.Id, x.User.IsEmailConfirm, x.User.RowVersion }
            })
            .FirstOrDefaultAsync(ct);

        if (changePasswordRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Удаляем токен из базы (в любом случае надо удалить токен, он одноразовый)
        _db.ChangePasswordRequests.Remove(changePasswordRequestFromDb.Request);
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (changePasswordRequestFromDb.Request.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Пользователь не найден
        var userFromDb = changePasswordRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Меняем пароль
        var updatedRows = await _db.Users.Where(x => x.Id == userFromDb.Id && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.HashedPassword, changePasswordRequestFromDb.Request.HashedNewPassword), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetPasswordAsync(Guid userId, SetPasswordDto setPasswordDto, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(setPasswordDto);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Валидация модели
        var validationResult = await _setPasswordDtoValidator.ValidateAsync(setPasswordDto, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(SetPasswordDto), validationResult.Errors));

        var newHashedPassword = _passwordHasher.GenerateHashedPassword(setPasswordDto.NewPassword);

        // Меняем пароль
        var updatedRows = await _db.Users.Where(x => x.Id == userId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.HashedPassword, newHashedPassword), ct);

        // Пользователь не найден
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        return ServiceResult.Success();
    }
}