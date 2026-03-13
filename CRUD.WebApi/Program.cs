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
    options.SerializerOptions.Converters.Add(new DateTimeConverter());
    options.SerializerOptions.Converters.Add(new TrimStringConverter());
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

#region Сервисы
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserDtoValidator>();
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserDtoValidator>();
builder.Services.AddScoped<IValidator<DeleteUserDto>, DeleteUserDtoValidator>();
builder.Services.AddScoped<IValidator<User>, UserValidator>();
builder.Services.AddScoped<IValidator<LoginDataDto>, LoginDataDtoValidator>();
builder.Services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordDtoValidator>();
builder.Services.AddScoped<IValidator<SetPasswordDto>, SetPasswordDtoValidator>();
builder.Services.AddScoped<IValidator<SetRoleDto>, SetRoleDtoValidator>();
builder.Services.AddScoped<IValidator<GetPublicationsDto>, GetPublicationsDtoValidator>();
builder.Services.AddScoped<IValidator<GetAuthorsDto>, GetAuthorsDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePublicationDto>, UpdatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<UpdatePublicationFullDto>, UpdatePublicationFullDtoValidator>();
builder.Services.AddScoped<IValidator<CreatePublicationDto>, CreatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<Publication>, PublicationValidator>();
builder.Services.AddScoped<IValidator<ClientApiCreatePublicationDto>, ClientApiCreatePublicationDtoValidator>();
builder.Services.AddScoped<IValidator<ConfirmEmailRequest>, ConfirmEmailRequestValidator>();
builder.Services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
builder.Services.AddScoped<IValidator<VerificationPhoneNumberRequest>, VerificationPhoneNumberRequestValidator>();
builder.Services.AddScoped<IValidator<Order>, OrderValidator>();
builder.Services.AddScoped<IValidator<Product>, ProductValidator>();
builder.Services.AddScoped<IValidator<Notification>, NotificationValidator>();
builder.Services.AddScoped<IValidator<CreateNotificationDto>, CreateNotificationDtoValidator>();
builder.Services.AddScoped<IValidator<CreateNotificationSelectedUsersDto>, CreateNotificationSelectedUsersDtoValidator>();
builder.Services.AddScoped<IValidator<GetUserNotificationsDto>, GetUserNotificationsDtoValidator>();
builder.Services.AddScoped<IValidator<GetPaginatedListDto>, GetPaginatedListDtoValidator>();
builder.Services.AddScoped<IValidator<AuthRefreshToken>, AuthRefreshTokenValidator>();
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
builder.Services.AddSingleton<IS3Manager,  S3Manager>();
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

builder.Services.AddTransient<IAuthorizationHandler, LanguageDenyHandler>();

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
    app.UseSwaggerUi(options =>
    {
        options.Path = "/openapi";
        options.DocumentPath = "/openapi/v1.json";
        options.DocumentTitle = "CRUD"; // Название вкладки
    });
}
else if (app.Environment.IsProduction())
{
    // Добавить глобальный обработчик ошибок в pipeline, чтобы вместо трейса и других внутренностей была грамотно сформированная ошибка для клиента (выше добавлен AddExceptionHandler)
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseMiddleware<UsefulBadRequestMiddleware>(); // Обязательно после UseExceptionHandler

//app.UseHttpsRedirection(); // Если не закомментировать, то ЮКасса не будет работать с Tuna (307 статус код)
app.UseReadyStaticFilesAndDirectoryBrowser();
app.UseRouting();
app.UseRequestTimeouts();
app.UseRequestLocalization();
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
    .RequireAuthorization(UserRoles.Admin) // С авторизацией
    .DisableHttpMetrics(); // Без метрик
#endregion

#region Metrics
app.MapPrometheusScrapingEndpoint().RequireCors(CorsPolicyNames.Metrics); // Телеметрия (/metrics)
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

app.Logger.LogInformation("Приложение запущено.");

app.Run();


async Task InitializeDatabaseAsync(CancellationToken ct = default)
{
    using var scope = app.Services.CreateScope();
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.InitializeAsync(ct);

    var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();
    await userManager.CreateAdminUserAsync(ct);

    var productManager = scope.ServiceProvider.GetRequiredService<IProductManager>();
    await productManager.AddProductsToDbAsync(ct);
}

async Task InitializeS3EcosystemAsync(CancellationToken ct = default)
{
    using var scope = app.Services.CreateScope();
    var s3Initializer = scope.ServiceProvider.GetRequiredService<IS3Initializer>();
    await s3Initializer.InitializeAsync(ct);
}