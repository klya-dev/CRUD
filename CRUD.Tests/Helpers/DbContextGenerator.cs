namespace CRUD.Tests.Helpers;

/// <summary>
/// Класс для работы с тестовой базой.
/// </summary>
public static class DbContextGenerator
{
    private static readonly string ConnectionString;
    private static readonly MySqlServerVersion ServerVersion = new(new Version(8, 0, 25));

    static DbContextGenerator()
    {
        ConnectionString = TestSettingsHelper.GetDbConnectionString<TestMarker>();
    }

    /// <summary>
    /// Полностью пересоздаёт тестовую базу данных, используя <c>UseInMemoryDatabase</c>.
    /// </summary>
    /// <remarks>
    /// <para>Чтобы не пересоздавать базу данных, а получить только контекст уже созданной базы данных, нужно указать <c><paramref name="create"/> = false</c>.</para>
    /// <para>Например, для теста конфликтов параллельности нужен хотя бы второй контекст той же базы данных.</para>
    /// </remarks>
    /// <returns>Контекст базы данных.</returns>
    public static ApplicationDbContext GenerateDbContextTestInMemory(string? databaseName = null, bool logging = false)
    {
        databaseName ??= Guid.NewGuid().ToString();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName);

        ApplicationDbContext db = null;
        if (logging)
        {
            using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
            ILogger<ApplicationDbContext> logger = loggerFactory.CreateLogger<ApplicationDbContext>();
            db = new ApplicationDbContext(optionsBuilder.Options, logger);
        }
        else
            db = new ApplicationDbContext(optionsBuilder.Options, null!);

        // Удаление таблиц и создание базы не требуется, это всё сделает UseInMemoryDatabase

        return db;
    }

    /// <summary>
    /// Полностью пересоздаёт тестовую базу данных.
    /// </summary>
    /// <remarks>
    /// <para>Чтобы не пересоздавать базу данных, а получить только контекст уже созданной базы данных, нужно указать <c><paramref name="create"/> = false</c>.</para>
    /// <para>Например, для теста конфликтов параллельности нужен хотя бы второй контекст той же базы данных.</para>
    /// </remarks>
    /// <param name="create">Применить ли миграции.</param>
    /// <returns>Контекст базы данных.</returns>
    public static ApplicationDbContext GenerateDbContextTest(bool create = true)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>().UseMySql(ConnectionString, ServerVersion, mySqlOptions =>
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null)).EnableDetailedErrors();

        using var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace).AddConsole());
        ILogger<ApplicationDbContext> logger = loggerFactory.CreateLogger<ApplicationDbContext>();

        var db = new ApplicationDbContext(optionsBuilder.Options, logger);
        if (create)
        {
            // Удаляем таблицы
            DeleteTables(db);

            db.Database.EnsureCreated(); // Создаём базу
        }

        return db;
    }

    /// <summary>
    /// Очищает все таблицы из базы данных (TRUNCATE).
    /// </summary>
    /// <param name="db">Контекст базы данных.</param>
    private static void ClearTables(ApplicationDbContext db)
    {
        string[] tables = ["Users", "Publications", "Requests", "ConfirmEmailRequests", "ChangePasswordRequests", "VerificationPhoneNumberRequests", "Orders", "Products", "OrderNumberSequences", "Notifications", "UserNotifications", "AuthRefreshTokens"];
        string query = "SET FOREIGN_KEY_CHECKS = 0;\n"; // Обязательно должно быть в одном запросе
        foreach (var table in tables)
            query += $"TRUNCATE TABLE {table};\n";

        try
        {
            db.Database.ExecuteSqlRaw(query);
        }
        catch { }

        // Иногда, быстре ловить исключения (если во время "запускать до сбоя" не вовремя стопнуть, то какие-то таблицы останутся, какие-то удалятся, и будет исключение)
        // Чем каждый раз отправлять запрос на существование базы
    }

    /// <summary>
    /// Удаляет все таблицы из базы данных (DROP).
    /// </summary>
    /// <param name="db">Контекст базы данных.</param>
    public static void DeleteTables(ApplicationDbContext db)
    {
        string[] tables = ["Users", "Publications", "Requests", "ConfirmEmailRequests", "ChangePasswordRequests", "VerificationPhoneNumberRequests", "Orders", "Products", "OrderNumberSequences", "Notifications", "UserNotifications", "AuthRefreshTokens"];
        string query = "SET FOREIGN_KEY_CHECKS = 0;\n"; // Обязательно должно быть в одном запросе
        foreach (var table in tables)
            query += $"DROP TABLE IF EXISTS {table};\n";

        try
        {
            db.Database.ExecuteSqlRaw(query);
        }
        catch { }

        // Иногда, быстре ловить исключения (если во время "запускать до сбоя" не вовремя стопнуть, то какие-то таблицы останутся, какие-то удалятся, и будет исключение)
        // Чем каждый раз отправлять запрос на существование базы
    }
}