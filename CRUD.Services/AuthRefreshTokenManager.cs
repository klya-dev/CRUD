using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IAuthRefreshTokenManager"/>
public class AuthRefreshTokenManager : IAuthRefreshTokenManager
{
    private readonly ApplicationDbContext _db;
    private readonly IValidator<AuthRefreshToken> _authRefreshTokenValidator;
    private readonly IOptionsMonitor<AuthWebApiOptions> _authWebApiOptions;

    public AuthRefreshTokenManager(ApplicationDbContext db, IValidator<AuthRefreshToken> authRefreshTokenValidator, IOptionsMonitor<AuthWebApiOptions> authWebApiOptions)
    {
        _db = db;
        _authRefreshTokenValidator = authRefreshTokenValidator;
        _authWebApiOptions = authWebApiOptions;
    }

    public async Task AddRefreshTokenAndDeleteOldersAsync(string newRefreshToken, Guid userId, string? usedRefreshToken = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(newRefreshToken);

        // Пустой GUID
        if (userId == Guid.Empty)
            throw new InvalidOperationException(ErrorMessages.EmptyUniqueIdentifier);

        // Добавляем в базу новый Refresh-токен
        var authRefreshToken = new AuthRefreshToken()
        {
            Token = newRefreshToken,
            UserId = userId,
            Expires = DateTime.UtcNow.Add(_authWebApiOptions.CurrentValue.ExpiresRefreshToken),
        };

        // Валидация модели
        var validationResult = await _authRefreshTokenValidator.ValidateAsync(authRefreshToken, ct);
        if (!validationResult.IsValid)
            throw new InvalidOperationException(ErrorMessages.ModelIsNotValid(nameof(AuthRefreshToken), validationResult.Errors));

        await _db.AuthRefreshTokens.AddAsync(authRefreshToken, ct);
        await _db.SaveChangesAsync(ct);

        // Удаляем использованный Refresh-токен
        if (usedRefreshToken != null)
            await _db.AuthRefreshTokens.Where(x => x.Token == usedRefreshToken)
                .ExecuteDeleteAsync(ct);

        // Если количество Refresh-токенов пользователя превышает трёх, то удаляем старые Refresh-токены, пока их не станет 3
        var countRefreshTokens = await _db.AuthRefreshTokens.CountAsync(x => x.UserId == userId, ct);
        if (countRefreshTokens > _authWebApiOptions.CurrentValue.MaxCountRefreshTokens)
        {
            await _db.AuthRefreshTokens.Where(x => x.UserId == userId)
                .OrderBy(x => x.Expires)
                .Take(countRefreshTokens - _authWebApiOptions.CurrentValue.MaxCountRefreshTokens)
                .ExecuteDeleteAsync(ct);
        }
    }
}