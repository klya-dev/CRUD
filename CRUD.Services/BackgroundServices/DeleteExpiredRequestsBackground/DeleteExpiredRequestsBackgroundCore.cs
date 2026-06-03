using Microsoft.EntityFrameworkCore.Metadata;

namespace CRUD.Services.BackgroundServices.DeleteExpiredRequestsBackground;

/// <inheritdoc cref="IDeleteExpiredRequestsBackgroundCore"/>
public sealed class DeleteExpiredRequestsBackgroundCore : IDeleteExpiredRequestsBackgroundCore
{
    private readonly ApplicationDbContext _db;
    private readonly string _tableName;
    private readonly string _columnName;

    public DeleteExpiredRequestsBackgroundCore(ApplicationDbContext db)
    {
        _db = db;

        // Находим метаданные сущности Request
        var entityType = _db.Model.FindEntityType(typeof(Request)) ?? throw new NullReferenceException("The entityType must not be null.");

        // Получаем имя таблицы
        _tableName = entityType.GetTableName() ?? throw new NullReferenceException("The tableName must not be null.");

        // Находим метаданные свойства Expires
        var property = entityType.FindProperty(nameof(Request.Expires)) ?? throw new NullReferenceException("The property must not be null.");

        // Получаем имя колонки в БД
        _columnName = property.GetColumnName(StoreObjectIdentifier.Create(entityType, StoreObjectType.Table).GetValueOrDefault()) ?? throw new NullReferenceException("The columnName must not be null.");
    }

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // К сожалению, 'ExecuteDelete'/'ExecuteUpdate' operations on hierarchies mapped as TPT is not supported
        // Поэтому либо прогружаем в память, либо пишем сырой запрос

        // Удаляем все истёкшие запросы пользователей
        //var expiredRequests = await _db.Requests.Where(x => x.Expires < DateTime.UtcNow).ToListAsync(ct);
        //_db.Requests.RemoveRange(expiredRequests);
        //await _db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        var sql = $"DELETE FROM `{_tableName}` WHERE `{_columnName}` < {{0}};"; // Названия таблиц и колонок нельзя передавать как обычные SQL-параметры

        await _db.Database.ExecuteSqlRawAsync(sql, [now], cancellationToken: ct);
    }
}