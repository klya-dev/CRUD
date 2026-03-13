namespace CRUD.Services.BackgroundServices.DeleteExpiredRequestsBackground;

/// <inheritdoc cref="IDeleteExpiredRequestsBackgroundCore"/>
public class DeleteExpiredRequestsBackgroundCore : IDeleteExpiredRequestsBackgroundCore
{
    private readonly ApplicationDbContext _db;

    public DeleteExpiredRequestsBackgroundCore(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // К сожалению, 'ExecuteDelete'/'ExecuteUpdate' operations on hierarchies mapped as TPT is not supported
        // Поэтому придётся прогружать в память

        // Удаляем все истёкшие запросы пользователей
        var expiredRequests = await _db.Requests.Where(x => x.Expires < DateTime.UtcNow).ToListAsync(ct);

        _db.Requests.RemoveRange(expiredRequests);
        await _db.SaveChangesAsync(ct);
    }
}