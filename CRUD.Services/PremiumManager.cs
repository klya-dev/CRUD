namespace CRUD.Services;

/// <inheritdoc cref="IPremiumManager"/>
public sealed class PremiumManager : IPremiumManager
{
    private readonly ApplicationDbContext _db;
    private readonly IUserApiKeyManager _userApiKeyManager;
    private readonly IPayManager _payManager;
    private readonly IPremiumInformator _premiumInformator;

    public PremiumManager(ApplicationDbContext db, IUserApiKeyManager userApiKeyManager, IPayManager payManager, IPremiumInformator premiumInformator)
    {
        _db = db;
        _userApiKeyManager = userApiKeyManager;
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
        var orderFromDb = await _db.Orders.Where(x => x.Id == orderId)
            .Select(x => new
            {
                Order = new { x.Status, x.PaymentStatus },
                User = new { x.User!.Id, x.User.Email, x.User.LanguageCode, x.User.IsPremium, x.User.RowVersion }
            })
            .FirstOrDefaultAsync(ct);

        if (orderFromDb == null)
            return ServiceResult.Fail(ErrorMessages.OrderNotFound);

        // Пользователь не найден
        var userFromDb = orderFromDb.User;
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Заказ уже выдан или отменён
        if (orderFromDb.Order.Status != OrderStatuses.Accept)
            return ServiceResult.Fail(ErrorMessages.OrderAlreadyIssuedOrCanceled);

        // Оплата не завершена
        if (orderFromDb.Order.PaymentStatus != PaymentStatuses.Succeeded)
            return ServiceResult.Fail(ErrorMessages.PaymentNotCompleted);

        // Уже есть премиум
        if (userFromDb.IsPremium)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyHasPremium);

        // Обновляем данные пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userFromDb.Id && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.IsPremium, true)
                .SetProperty(p => p.ApiKey, _userApiKeyManager.GenerateUserApiKey())
                .SetProperty(p => p.DisposableApiKey, _userApiKeyManager.GenerateDisposableUserApiKey()), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

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
        var userFromDb = await _db.Users.Where(x => x.Id == userId).Select(x => new { x.Id, x.IsPremium, x.RowVersion }).FirstOrDefaultAsync(ct);
        if (userFromDb == null)
            return ServiceResult.Fail(ErrorMessages.UserNotFound);

        // Уже есть премиум
        if (userFromDb.IsPremium)
            return ServiceResult.Fail(ErrorMessages.UserAlreadyHasPremium);

        // Обновляем данные пользователя
        var updatedRows = await _db.Users.Where(x => x.Id == userFromDb.Id && x.RowVersion == userFromDb.RowVersion)
            .ExecuteUpdateAsync(x =>
                x.SetProperty(p => p.IsPremium, true)
                .SetProperty(p => p.ApiKey, _userApiKeyManager.GenerateUserApiKey())
                .SetProperty(p => p.DisposableApiKey, _userApiKeyManager.GenerateDisposableUserApiKey()), ct);

        // Найдено 0 строк (Where;MySQL:UseAffectedRows). Вероятно, из-за разных RowVersion - конфликт
        if (updatedRows == 0)
            return ServiceResult.Fail(ErrorMessages.ConcurrencyConflicts);

        return ServiceResult.Success();
    }
}