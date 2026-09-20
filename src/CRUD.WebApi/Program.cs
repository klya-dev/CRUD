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