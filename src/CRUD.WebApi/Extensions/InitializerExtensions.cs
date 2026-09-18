namespace CRUD.WebApi.Extensions;

/// <summary>
/// Расширения для вызова инициализаторов и начальных сведений.
/// </summary>
/// <remarks>
/// Универсальное применение (<see cref="IHost"/>).
/// </remarks>
public static class InitializerExtensions
{
    // Можно вынести в инициализаторы в IHostedService (StartAsync), это сработает, но появится другая проблема - "гонка", хоть эти сервисы и будут зарегистрированны первые builder.Services.AddHostedService<>();
    // Благодаря асинхронности контекст будет скакать туда сюда, и есть шанс, что какой-то фоновой сервис обратится к базе быстрее, чем она успеет инициализируется
    // Поэтому правильным решением будет вызывать инициализаторы до каких-либо IHostedService прямо в Program.cs

    // this IHost - это общая инфраструктурная область (не только веб-приложения)
    // this WebApplication - это уже касается веб-приложений (наследуется от IHost)

    // WebApplicationBuilder у меня есть в ProgramExtensions
    // А этот класс InitializerExtensions хочу сделать универсальным (IHost), чтобы его можно быть использовать не только веб-приложениями
    // Вряд ли, кроме веб-приложения будет кто-то это вызывать, но чисто для примера.

    /// <summary>
    /// Инициализирует базу данных, применяет миграции и заполняет обязательными данными.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// <term>Применяет все миграции или создаёт базу</term>
    /// <description><see cref="IDbInitializer.InitializeAsync(CancellationToken)"/></description>
    /// </item>
    /// <item>
    /// <term>Создаёт админа</term>
    /// <description><see cref="IUserManager.CreateAdminUserAsync(CancellationToken)"/></description>
    /// </item>
    /// <item>
    /// <term>Добавляет все продукты в базу</term>
    /// <description><see cref="IProductManager.AddProductsToDbAsync(CancellationToken)"/></description>
    /// </item>
    /// </list>
    /// </remarks>
    public static async Task InitializeDatabaseAsync(this IHost host, CancellationToken ct = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InitializerExtensions));

        logger.LogInformation("Инициализация базы данных и её заполнение.");

        try
        {
            // Применяем все миграции или создаём базу
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.InitializeAsync(ct);

            // Создаём админа
            var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
            await userManager.CreateAdminUserAsync(ct);

            // Добавляем все продукты в базу
            var productManager = scope.ServiceProvider.GetRequiredService<IProductManager>();
            await productManager.AddProductsToDbAsync(ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла ошибка при попытке инициализировать базу / создать админа / добавить продукты в базу: \"{message}\"", ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Инициализирует экосистему S3, заполняет обязательными данными.
    /// </summary>
    public static async Task InitializeS3EcosystemAsync(this IHost host, CancellationToken ct = default)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InitializerExtensions));

        logger.LogInformation("Инициализация экосистемы S3 и её заполнение.");

        try
        {
            // Инициализируем экосистему S3
            var s3Initializer = scope.ServiceProvider.GetRequiredService<IS3Initializer>();
            await s3Initializer.InitializeAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла ошибка при попытке инициализировать экосистему S3: \"{message}\"", ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Информирует о статусе удалённого прокси сервера.
    /// </summary>
    public static void LogProxyStatus(this IHost host, IConfiguration configuration)
    {
        using var scope = host.Services.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InitializerExtensions));

        // Уведомление о отсутствии удалённого прокси сервера
        var proxyIps = configuration.GetSection(ProxiesOptions.SectionName).Get<ProxiesOptions>()!.RemoteProxyIps;
        if (proxyIps.Length == 0)
            logger.LogInformation("Удалённые прокси сервера не указаны, в качестве доверенного прокси используется локальный диапазон IP-адресов.");
    }
}