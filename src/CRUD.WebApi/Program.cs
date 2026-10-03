using Serilog.Formatting.Compact;

// Консольный логгер (двухэтапная инициализация), он будет донастроен в UseSerilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter()) // По дефолту предпочитают выводить в JSON. В AddLogging у меня проверка, если Production, то все логи в JSON, иначе обычный текст
    .CreateBootstrapLogger();
// Благодаря "Bootstrap" (ReloadableLogger) логгер сможет донастроится в UseSerilog (двухэтапная инициализация).
// CreateLogger тоже будет работать,
// но если какой-то фоновый сервис создается до того, как UseSerilog полностью дочитал appsettings.json, этот сервис получит и навсегда зафиксирует внутри себя старый, урезанный логгер (только консоль)

// CreateLogger (Logger) - будет два разных инстанса, один создаётся в начале: Log.Logger = ...CreateLogger()
// А второй создаётся в UseSerilog. После создания в UseSerilog обновляется ссылка на Log.Logger, но тем сервисам, которым успели передать первый инстанс в конструктор, так и останутся с урезанным логгером

// CreateBootstrapLogger (ReloadableLogger) - будет один инстанс, он создаётся в начале: Log.Logger = ...CreateBootstrapLogger()
// В UseSerilog изменяются поля этого инстанса (донастраиваются), соответственно ссылка сохраняется и проблем таких, как в CreateLogger не будет

// И вся эта логика с двухэтапной инициализации нужна, чтобы логировать сведения до запуска приложения
// А try catch для логирования сведений и корректного завершения логгера (никакой лог не будет утерен)

try
{
    // Log.Logger по умолчанию равен Serilog.Core.Logger.None
    // поэтому всё, что до вызова services.AddSerilog() (extention AddLogging - UseSerilog) не залогируется
    // Для решения этой проблемы, создаём консольный логгер выше.
    // Итого: 
    // "Запуск приложения." - залогируется в консоль (в файле его не будет (можно настроить)), т. к. вызов до services.AddSerilog()
    // "Выключение завершено." - залогируется и в консоль (настроенную в AddSerilog), и в файл, т. к. вызов после services.AddSerilog()
    // И благодаря "Bootstrap" (ReloadableLogger), логгер будет один и тот же, а не два разных инстанса, если бы CreateLogger (Logger)
    Log.Information("Запуск приложения.");

    var builder = WebApplication.CreateBuilder(args);
    ProgramOptions programOptions = builder.Configuration.GetSection(ProgramOptions.SectionName).Get<ProgramOptions>()!;

    // Настройка на уровне Host'а
    builder
        .AddKestrel()
        .AddLogging(programOptions);

    // Настройка на уровне сервисов
    builder.Services
        .AddOptions(builder.Configuration) // Парсинг конфигурации в Options'ы
        .AddDatabase(builder.Configuration) // Добавление базы данных
        .AddApiDocumentationAndVersioning() // OpenApi, версионирование
        .AddWebInfrastructureAndFormatting(builder.Environment) // Локализация, форматированние, ProblemDetails и ExceptionHandler'ы
        .AddSecurityAndTrafficPolicy(builder.Configuration) // Авторизация, аутентификация, ограничения трафика, таймауты, Forwarded, CORS заголовки 
        .AddCachingAndOptimization(builder.Configuration) // Кэширование
        .AddMessagingAndCommunication(builder.Configuration, builder.Environment) // Сервисы взаимодействия: HTTP, gRPC, SignalR
        .AddObservabilityAndHealth() // Метрики и HealthCheck'и
        .AddApplicationCore(programOptions); // Прикладная бизнес-логика: сервисы, валидаторы

    var app = builder.Build();

    app.UseWebApplicationPipeline(programOptions);

    // Пропускаем ли инициализаторы
    if (!programOptions.SkipInitializers)
    {
        await app.InitializeDatabaseAsync();
        await app.InitializeS3EcosystemAsync();
        app.LogProxyStatus(app.Configuration);
    }

    app.MapApplicationEndpoints();
    app.MapApplicationHealthCheck();
    app.MapPrometheusScrapingEndpoint(); // Телеметрия (/metrics)
    app.MapApplicationHubs();
    app.MapShortCircuit(404, "robots.txt", "favicon.ico"); // Т.к у меня нет указанных файлов, я могу уменьшить нагрузку на сервер, путём пропуска нескольких Middleware'ов (CORS, Endpoint...)
    // (https://andrewlock.net/exploring-the-dotnet-8-preview-short-circuit-routing | https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/routing?view=aspnetcore-9.0#short-circuit-middleware-after-routing)

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение неожиданно завершило работу.");
}
finally
{
    Log.Information("Выключение завершено.");
    Log.CloseAndFlush(); // Завершаем работу логгера и принудительно логируем то, что ещё осталось в памяти (никакой лог не будет утерен)
}