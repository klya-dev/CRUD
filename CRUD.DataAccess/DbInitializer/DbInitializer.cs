using CRUD.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace CRUD.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _db;

    public DbInitializer(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Как грамотно добавить миграции и обновить базу:
        // add-migration ...
        // update-database ИЛИ запустить приложение (Database.MigrateAsync)
        // Главное не удалять прошлые миграции, т.к они все взаимосвязаны (remove-migration для удаления последней миграции)
        // Таким образом можно хоть новые столбцы добавлять и данные не сломаются

        try
        {
            var pendingMigrations = await _db.Database.GetPendingMigrationsAsync(ct); // Все миграции, которые определены в сборке, но не были применены к БД
            if (pendingMigrations.Any())
                await _db.Database.MigrateAsync(ct); // Создание БД, если не существует и добавление миграций
        }
        catch { }
    }
}