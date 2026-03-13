using CRUD.Models.Dtos.Password;
using CRUD.Services.Interfaces;

namespace CRUD.Services;

/// <inheritdoc cref="IPasswordChanger"/>
public class PasswordChanger : IPasswordChanger
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<ChangePasswordDto> _changePasswordDtoValidator;
    private readonly IValidator<SetPasswordDto> _setPasswordDtoValidator;
    private readonly IValidator<User> _userValidator;
    private readonly IChangePasswordRequestManager _changePasswordRequestManager;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordChanger(ApplicationDbContext db, IValidator<ChangePasswordDto> changePasswordDtoValidator, IValidator<SetPasswordDto> setPasswordDtoValidator, IValidator<User> userValidator, IChangePasswordRequestManager changePasswordRequestManager, IPasswordHasher passwordHasher)
    {
        _db = db;
        _changePasswordDtoValidator = changePasswordDtoValidator;
        _setPasswordDtoValidator = setPasswordDtoValidator;
        _userValidator = userValidator;
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
        var changePasswordRequestFromDb = await _db.ChangePasswordRequests.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == token, ct);
        if (changePasswordRequestFromDb == null)
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Удаляем токен из базы (в любом случае надо удалить токен, он одноразовый)
        _db.ChangePasswordRequests.Remove(changePasswordRequestFromDb);
        await _db.SaveChangesAsync(ct);

        // Проверка срока действия токена
        if (changePasswordRequestFromDb.IsExpired())
            return ServiceResult.Fail(ErrorMessages.InvalidToken);

        // Пользователь не найден
        var userFromDb = changePasswordRequestFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Меняем пароль
        userFromDb.HashedPassword = changePasswordRequestFromDb.HashedNewPassword;

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _userValidator.ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        // Сохраняем изменения
        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

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

        // Пользователь не найден
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Меняем пароль
        userFromDb.HashedPassword = _passwordHasher.GenerateHashedPassword(setPasswordDto.NewPassword);

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _userValidator.ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        // Сохраняем изменения
        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}