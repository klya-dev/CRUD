namespace CRUD.Services;

/// <inheritdoc cref="IPremiumManager"/>
public class PremiumManager : IPremiumManager
{
    private readonly ApplicationDbContext _db;
    private readonly IUserApiKeyManager _userApiKeyManager;
    private readonly IValidator<User> _userValidator;
    private readonly IPayManager _payManager;
    private readonly IPremiumInformator _premiumInformator;

    public PremiumManager(ApplicationDbContext db, IUserApiKeyManager userApiKeyManager, IValidator<User> userValidator, IPayManager payManager, IPremiumInformator premiumInformator)
    {
        _db = db;
        _userApiKeyManager = userApiKeyManager;
        _userValidator = userValidator;
        _payManager = payManager;
        _premiumInformator = premiumInformator;
    }

    public async Task<ServiceResult<string>> BuyPremiumAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userFromDb = await _db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.IsPremium }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult<string>.Fail(ErrorMessages.UserNotFound);

        // Уже есть премиум
        if (userFromDb.IsPremium)
            return ServiceResult<string>.Fail(ErrorMessages.UserAlreadyHasPremium);

        // Получаем данные заказа из ответа сервиса
        var result = await _payManager.PayAsync(Products.Premium, userId, ct);

        // Не удалось создать платёж
        if (result == null)
            return ServiceResult<string>.Fail(ErrorMessages.FailedToCreatePayment);

        // Возвращаем ссылку для оплаты
        return ServiceResult<string>.Success(result.Confirmation.ConfirmationUrl);
    }

    public async Task<ServiceResult> IssuePremiumAsync(Guid orderId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (orderId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Заказ не найден
        var orderFromDb = await _db.Orders.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (orderFromDb == null)
            return ServiceResult.Fail(ErrorMessages.OrderNotFound);

        // Пользователь не найден
        var userFromDb = orderFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Заказ уже выдан или отменён
        if (orderFromDb.Status != OrderStatuses.Accept)
            return ServiceResult.Fail(ErrorMessages.OrderAlreadyIssuedOrCanceled);

        // Оплата не завершена
        if (orderFromDb.PaymentStatus != PaymentStatuses.Succeeded)
            return ServiceResult.Fail(ErrorMessages.PaymentNotCompleted);

        // Уже есть премиум
        if (userFromDb.IsPremium)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyHasPremium);

        // Обновляем данные пользователя
        userFromDb.IsPremium = true;
        userFromDb.ApiKey = _userApiKeyManager.GenerateUserApiKey();
        userFromDb.DisposableApiKey = _userApiKeyManager.GenerateDisposableUserApiKey();

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _userValidator.ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

        // Информируем пользователя о получении премиума
        await _premiumInformator.InformateAsync(userFromDb.Email, userFromDb.LanguageCode, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetPremiumAsync(Guid userId, CancellationToken ct = default)
    {
        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Пользователь не найден
        var userFromDb = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Уже есть премиум
        if (userFromDb.IsPremium)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyHasPremium);

        // Обновляем данные пользователя
        userFromDb.IsPremium = true;
        userFromDb.ApiKey = _userApiKeyManager.GenerateUserApiKey();
        userFromDb.DisposableApiKey = _userApiKeyManager.GenerateDisposableUserApiKey();

        // Проверка валидности данных перед записью в базу
        var validationResultUser = await _userValidator.ValidateAsync(userFromDb, ct);
        if (!validationResultUser.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(User), validationResultUser.Errors));

        _db.Users.Update(userFromDb);
        await _db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}