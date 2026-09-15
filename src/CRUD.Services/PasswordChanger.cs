using CRUD.Models.Dtos.Password;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace CRUD.Services;

/// <inheritdoc cref="IPasswordChanger"/>
public sealed class PasswordChanger : IPasswordChanger
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<ChangePasswordDto> _changePasswordDtoValidator;
    private readonly IValidator<SetPasswordDto> _setPasswordDtoValidator;
    private readonly IChangePasswordRequestManager _changePasswordRequestManager;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITimeLimitedDataProtector _protector;

    public PasswordChanger(ApplicationDbContext db, IValidator<ChangePasswordDto> changePasswordDtoValidator, IValidator<SetPasswordDto> setPasswordDtoValidator, IChangePasswordRequestManager changePasswordRequestManager, IPasswordHasher passwordHasher, IDataProtectionProvider provider)
    {
        _db = db;
        _changePasswordDtoValidator = changePasswordDtoValidator;
        _setPasswordDtoValidator = setPasswordDtoValidator;
        _changePasswordRequestManager = changePasswordRequestManager;
        _passwordHasher = passwordHasher;

        // Создаём протектор с ограничением времени
        _protector = provider.CreateProtector(ChangePasswordRequestManager.Purpose).ToTimeLimitedDataProtector();
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

        // Добавляем токен в кэш и отправляем письмо
        var resultAddTokenToStorageAndSendLetter = await _changePasswordRequestManager.AddTokenToStorageAndSendLetterAsync(userFromDb.Id, userFromDb.Email, userFromDb.LanguageCode,
            _passwordHasher.GenerateHashedPassword(changePasswordDto.NewPassword), ct);

        // Есть ошибка
        if (resultAddTokenToStorageAndSendLetter.ErrorMessage != null)
            return ServiceResult.Fail(resultAddTokenToStorageAndSendLetter.ErrorMessage, resultAddTokenToStorageAndSendLetter.ErrorParams);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ChangePasswordAsync(string token, CancellationToken ct = default)
    {
        // Пустые данные
        ArgumentNullException.ThrowIfNull(token);

        try
        {
            // Расшифровываем полезную нагрузку
            string payloadJson = _protector.Unprotect(token);

            // Достаём данные из полезной нагрузки
            var payload = JsonSerializer.Deserialize<ChangePasswordPayload>(payloadJson);

            // Невалидный токен
            if (payload == null)
                return ServiceResult.Fail(ErrorMessages.InvalidToken);

            // Меняем пароль
            var updatedRows = await _db.Users.Where(x => x.Id == payload.UserId)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.HashedPassword, payload.HashedNewPassword), ct);

            // Найдено 0 строк. Пользователь не найден
            if (updatedRows == 0)
                return ServiceResult.Fail(ErrorMessages.UserNotFound);
        }
        catch (System.Security.Cryptography.CryptographicException) // Защищенные полезные данные не могут быть проверены или расшифрованы
        {
            return ServiceResult.Fail(ErrorMessages.InvalidToken);
        }

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