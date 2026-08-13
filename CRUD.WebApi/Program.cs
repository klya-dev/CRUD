using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
ProgramOptions programOptions = builder.Configuration.GetSection(ProgramOptions.SectionName).Get<ProgramOptions>()!;

builder.ConfigureServer();

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    { 
        Timeout = TimeSpan.FromSeconds(25) // На каждый запрос (мой ответ) отводится небольше 25 секунд, иначе 504 ошибка | RequestTimeoutsSystemTest
    };
});

// Пропускаем ли логирование
if (!programOptions.SkipLogging)
    builder.ConfigureLogging();
else
    builder.Logging.ClearProviders();

builder.LoadOptions();
builder.ConfigureDb();
builder.ConfigureForwardedHeadersOptions();
builder.Services.AddEndpointsApiExplorer();
builder.ConfigureOpenApi();
builder.ConfigureApiVersioning();

// Порядок регистраций обработчиков имеет значение, 1 - BadRequestExceptionHandler (он и будет обрабатывать первый), 2 - ConcurrencyConflictExceptionHandler, 3 - GlobalExceptionHandler (порядок не как в Middleware)
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>(); // Обработка BadRequest исключений
builder.Services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>(); // Обработка конфликтов параллельности
// Глобальный обработчик ошибок только в Production, т.к он скрывает трейс
// P.S: К сожалению, UseDeveloperExceptionPage и UseExceptionHandler не работают в связке, поэтому в Dev не будет отрабатывать UseDeveloperExceptionPage. В Prod всё норм, все обработчики
if (builder.Environment.IsProduction())
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true); // Выбрасывать исключение BadRequest в Production +у меня есть обработчик этих исключений // https://github.com/dotnet/aspnetcore/issues/48355
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
    };
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new DateTimeConverter()); // Изменить формат записи даты
    options.SerializerOptions.Converters.Add(new TrimStringConverter()); // Обрезать все лишние пробелы в начале и конце строки
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Парсить enum, как строку, а не int (при привязке параметров)
});

builder.ConfigureRateLimiter();

builder.ConfigureHttpClientFactory();
builder.Services.AddHttpContextAccessor();

builder.Services.AddReadyLocalization();

builder.ConfigureCors();

builder.ConfigureAuthentication();
builder.ConfigureAuthorization();

builder.ConfigureOutputCache();
builder.ConfigureHybridCache();

builder.ConfigureHealthChecks();

builder.ConfigureOpenTelemetry();

builder.Services.AddDirectoryBrowser();
builder.Services.AddSignalR()
    .AddMessagePackProtocol(); // Добавляем поддержку MessagePack протокола. По дефолту JSON уже есть. MessagePack протокол быстрее, чем Json (https://learn.microsoft.com/ru-ru/aspnet/core/signalr/messagepackhubprotocol?view=aspnetcore-10.0)

builder.ConfigureGrpcClients();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();
// Чтобы ключи шифрования Data Protection не исчезали, можно сохранять их в хранилище, например - в Redis'е, в папке, в базе
// И тогда, после перезагрузки приложения ничего не сломается

#region Сервисы
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<DeleteUserDto>, DeleteUserDtoValidator>();
builder.Services.AddScoped<IValidator<LoginDataDto>, LoginDataDtoValidator>();
builder.Services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordDtoValidator>();
builder.Services.AddScoped<IValidator<SetPasswordDto>, SetPasswordDtoValidator>();
builder.Services.AddScoped<IValidator<SetRoleDto>, SetRoleDtoValidator>();
builder.Services.AddScoped<IValidator<GetPublicationsDto>, GetPublicationsDtoValidator>();
builder.Services.AddScoped<IValidator<GetAuthorsDto>, GetAuthorsDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePublicationDto>, UpdatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePublicationFullDto>, UpdatePublicationFullDtoValidator>();
builder.Services.AddScoped<IValidator<CreatePublicationDto>, CreatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<ClientApiCreatePublicationDto>, ClientApiCreatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<CreateNotificationDto>, CreateNotificationDtoValidator>();
builder.Services.AddScoped<IValidator<CreateNotificationSelectedUsersDto>, CreateNotificationSelectedUsersDtoValidator>();
builder.Services.AddScoped<IValidator<GetUserNotificationsDto>, GetUserNotificationsDtoValidator>();
builder.Services.AddScoped<IValidator<GetPaginatedListDto>, GetPaginatedListDtoValidator>();
builder.Services.AddScoped<IValidator<OAuthCompleteRegistrationDto>, OAuthCompleteRegistrationDtoValidator>();

builder.Services.AddScoped<IClientApiManager, ClientApiManager>();
builder.Services.AddScoped<IPremiumManager, PremiumManager>();
builder.Services.AddSingleton<IUserApiKeyManager, UserApiKeyManager>();
builder.Services.AddScoped<IPasswordChanger, PasswordChanger>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<ITokenManager, TokenManager>();
if (!programOptions.SkipInitializers) // Пропускаем ли инициализаторы
{
    builder.Services.AddScoped<IDbInitializer, DbInitializer>();
    builder.Services.AddScoped<IS3Initializer, S3Initializer>();
}
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IPublicationManager, PublicationManager>();
builder.Services.AddSingleton<IS3Manager, S3Manager>();
builder.Services.AddScoped<IAvatarManager, AvatarManager>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddSingleton<IHtmlHelper, HtmlHelper>();
builder.Services.AddSingleton<ISaveLogsToS3BackgroundCore, SaveLogsToS3BackgroundCore>();
builder.Services.AddSingleton<IQueueEmail, QueueEmail>();
builder.Services.AddSingleton<ISmsSender, SmsSender>();
builder.Services.AddSingleton<ITelegramIntegrationManager, TelegramIntegrationManager>();
builder.Services.AddScoped<IPayManager, PayManager>();
builder.Services.AddScoped<IOrderUpdater, OrderUpdater>();
builder.Services.AddScoped<IProductManager, ProductManager>();
builder.Services.AddScoped<IOrderIssuer, OrderIssuer>();
builder.Services.AddScoped<IOrderCreator, OrderCreator>();
builder.Services.AddScoped<IConfirmEmailRequestManager, ConfirmEmailRequestManager>();
builder.Services.AddScoped<IVerificationPhoneNumberRequestManager, VerificationPhoneNumberRequestManager>();
builder.Services.AddScoped<IChangePasswordRequestManager, ChangePasswordRequestManager>();
builder.Services.AddSingleton<IImageSingnatureChecker, ImageSingnatureChecker>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<IGrpcTokenManager, GrpcTokenManager>();
builder.Services.AddScoped<IRevokeExpiredRefreshTokensBackgroundCore, RevokeExpiredRefreshTokensBackgroundCore>();
builder.Services.AddScoped<IDeleteExpiredRequestsBackgroundCore, DeleteExpiredRequestsBackgroundCore>();
builder.Services.AddScoped<IAuthRefreshTokenManager, AuthRefreshTokenManager>();
builder.Services.AddSingleton<IOAuthMailRuProvider, OAuthMailRuProvider>();
builder.Services.AddSingleton<IPremiumInformator, PremiumInformator>();

builder.Services.AddSingleton<IAuthorizationHandler, LanguageDenyHandler>();

builder.Services.AddHostedService<SaveLogsToS3BackgroundService>();
builder.Services.AddHostedService<RevokeExpiredRefreshTokensBackgroundService>();
builder.Services.AddHostedService<DeleteExpiredRequestsBackgroundService>();

builder.Services.AddSingleton<ApiMeters>();
#endregion

var app = builder.Build();

// Пропускаем ли логирование
if (!programOptions.SkipLogging)
    app.UseReadyRequestLogging();
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Этот Middleware и так захардкожен по дефолту для Development (https://github.com/dotnet/aspnetcore/blob/main/src/DefaultBuilder/src/WebApplicationBuilder.cs#L402-L405)
    // Для API генерируется грамотный, красивый ответ application/problem+json, учитывая, что я выше добавил .AddProblemDetails()

    app.MapOpenApi(); // Конечная точка "/openapi/v1.json"
    app.MapScalarApiReference();
    app.UseSwaggerUi(options =>
    {
        options.Path = "/openapi";
        options.DocumentPath = "/openapi/v1.json";
        options.DocumentTitle = "CRUD"; // Название вкладки
    });
}

app.UseRequestLocalization(); // В обработчиках исключений используется локализация

// Добавить обработчики ошибок в pipeline (выше добавлены AddExceptionHandler)
app.UseExceptionHandler(); // GlobalExceptionHandler, который скрывает внутренности включается только в Production, а остальные обработчики везде

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseMiddleware<BasicAuthMetricsMiddleware>();

//app.UseHttpsRedirection(); // Если не закомментировать, то ЮКасса не будет работать с Tuna (307 статус код). Приложение получает запрос от Tuna, а в ответ присылает редирект на https, но Tuna не умеет в редиректы
app.UseReadyStaticFilesAndDirectoryBrowser();
app.UseRouting();
app.UseRequestTimeouts();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter(); // Использует локализацию и аутентификацию
app.UseAuthorization();
app.UseOutputCache(); // Обязательно после UseCors и UseRouting

// Пропускаем ли инициализаторы
if (!programOptions.SkipInitializers)
{
    await InitializeDatabaseAsync();
    await InitializeS3EcosystemAsync();
}

var apiVersionSet = app.NewApiVersionSet()
    .HasDeprecatedApiVersion(new ApiVersion(1.0)) // Указываю, что v1 является устаревшим API
    .HasApiVersion(new ApiVersion(2.0)) // Поддерживаемая версия API
    .ReportApiVersions()
    .Build();

AuthEndpoints.Map(app);
AdminEndpoints.Map(app);
UsersEndpoints.Map(app, apiVersionSet);
UserEndpoints.Map(app, apiVersionSet);
ConfirmationsEndpoints.Map(app, apiVersionSet);
PublicationsEndpoints.Map(app, apiVersionSet);
ClientApiEndpoints.Map(app, apiVersionSet);
WebHooksEndpoints.Map(app);
WellKnownEndpoints.Map(app);

#region Healthz
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
{
    AllowCachingResponses = false,

    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
})
    .RequireAuthorization(AuthorizationPolicyNames.OnlyAdmin) // С авторизацией админа
    .DisableHttpMetrics(); // Без метрик
#endregion

#region Metrics
app.MapPrometheusScrapingEndpoint(); // Телеметрия (/metrics)
#endregion

#region Hubs
app.MapHub<NotificationHub>("/notificationHub", options =>
{
    options.AllowStatefulReconnects = true; // Если какие-то перебои, то сервер (и клиент) буферизирует данные и даёт возможность переподключится +withStatefulReconnect на клиенте
}).RequireAuthorization();
#endregion

#region robots.txt, favicon.ico
app.MapShortCircuit(404, "robots.txt", "favicon.ico"); // Т.к у меня нет этих файлов, я могу уменьшить нагрузку на сервер, путём пропуска нескольких Middleware'ов (CORS, Endpoint...)
// (https://andrewlock.net/exploring-the-dotnet-8-preview-short-circuit-routing | https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/routing?view=aspnetcore-9.0#short-circuit-middleware-after-routing)
#endregion

// Уведомление о отсутствии удалённого прокси сервера
var proxyIps = builder.Configuration.GetSection(ProxiesOptions.SectionName).Get<ProxiesOptions>()!.RemoteProxyIps;
if (proxyIps.Length == 0)
    app.Logger.LogInformation("Удалённые прокси сервера не указаны, в качестве доверенного прокси используется локальный диапазон IP-адресов.");

app.Logger.LogInformation("Приложение запущено.");

await app.RunAsync();


async Task InitializeDatabaseAsync(CancellationToken ct = default)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.InitializeAsync(ct);

    var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
    await userManager.CreateAdminUserAsync(ct);

    var productManager = scope.ServiceProvider.GetRequiredService<IProductManager>();
    await productManager.AddProductsToDbAsync(ct);
}

async Task InitializeS3EcosystemAsync(CancellationToken ct = default)
{
    await using var scope = app.Services.CreateAsyncScope();
    var s3Initializer = scope.ServiceProvider.GetRequiredService<IS3Initializer>();
    await s3Initializer.InitializeAsync(ct);
}