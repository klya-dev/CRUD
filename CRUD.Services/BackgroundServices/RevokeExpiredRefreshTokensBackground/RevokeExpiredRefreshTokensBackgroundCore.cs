namespace CRUD.Services.BackgroundServices.RevokeExpiredRefreshTokensBackground;

/// <inheritdoc cref="IRevokeExpiredRefreshTokensBackgroundCore"/>
public class RevokeExpiredRefreshTokensBackgroundCore : IRevokeExpiredRefreshTokensBackgroundCore
{
    private readonly ApplicationDbContext _db;

    public RevokeExpiredRefreshTokensBackgroundCore(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // Удаляем все истёкшие Refresh-токены пользователей
        await _db.AuthRefreshTokens.Where(x => x.Expires < DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}