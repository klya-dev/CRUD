using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет консистенцию базы данных на наличие всех таблиц.
/// </summary>
public class DatabaseConsistencyHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DatabaseConsistencyHealthCheck(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Коллекция несуществующих таблиц, которые должны были существовать
        var notExistsTables = await GetNotExistsTablesAsync(cancellationToken);

        if (!notExistsTables.Any())
            return HealthCheckResult.Healthy();
        else
        {
            var tableNames = notExistsTables.Select(table => $"'{table}'");
            var names = string.Join(", ", tableNames);

            return HealthCheckResult.Unhealthy($"Tables: {names} does not exist");
        }
    }

    private async Task<IEnumerable<string>> GetNotExistsTablesAsync(CancellationToken ct = default)
    {
        // Получаем все типы сущностей, зарегистрированные в модели
        var entityTypes = _db.Model.GetEntityTypes();

        var tableNames = entityTypes.Select(x => x.GetTableName());

        var notExistsTables = new List<string>();
        foreach (var table in tableNames)
        {
            if (table == null)
                continue;

            bool hasTable = await IsTableExistsAsync(table, ct);
            if (!hasTable)
                notExistsTables.Add(table);
        }

        return notExistsTables;
    }

    private async Task<bool> IsTableExistsAsync(string tableName, CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) as Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME  = '{tableName}'";

        var result = await command.ExecuteScalarAsync(ct);

        var count = (Int64)(result ?? 0);
        return count > 0;
    }
}