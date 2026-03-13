var builder = WebApplication.CreateSlimBuilder(args);
ProgramOptions programOptions = builder.Configuration.GetSection(ProgramOptions.SectionName).Get<ProgramOptions>()!;

builder.ConfigureServer();

// Пропускаем ли логирование
if (!programOptions.SkipLogging)
    builder.ConfigureLogging();
else
    builder.Logging.ClearProviders();

builder.LoadOptions();
builder.ConfigureCors();
builder.ConfigureAuthentication();
builder.ConfigureAuthorization();
builder.ConfigureHealthChecks();
builder.ConfigureOpenTelemetry();

builder.Services.AddHttpClient(); // В Healthz используется

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ExceptionInterceptor>();
});

#region Сервисы
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IEmailSenderBackgroundCore, EmailSenderBackgroundCore>();
builder.Services.AddSingleton<ISaveLogsToS3BackgroundCore, SaveLogsToS3BackgroundCore>();
builder.Services.AddSingleton<IQueueEmail, QueueEmail>();
builder.Services.AddSingleton<IS3Manager, S3Manager>();
builder.Services.AddSingleton<IRabbitMqConsumerBackgroundCore, RabbitMqConsumerBackgroundCore>();

builder.Services.AddHostedService<EmailSenderBackgroundService>();
builder.Services.AddHostedService<SaveLogsToS3BackgroundService>();
builder.Services.AddHostedService<RabbitMqConsumerBackgroundService>();
#endregion

var app = builder.Build();

// Пропускаем ли логирование
if (!programOptions.SkipLogging)
    app.UseReadyRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else if (app.Environment.IsProduction())
{
    // Добавить глобальный обработчик ошибок в pipeline, чтобы вместо трейса и других внутренностей была грамотно сформированная ошибка для клиента (выше добавлен AddExceptionHandler)
    app.UseExceptionHandler(options => { }); // Если не прописать options исключение (https://github.com/dotnet/aspnetcore/issues/51888)
    app.UseHsts();
}

//app.UseHttpsRedirection(); // В большинстве случаев редирект не нужен, т.к приложение, обычно, стоит за обратным прокси, например, nginx, где уже настроенна переадресация
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

#region gRPC
app.MapGrpcService<GrpcEmailSenderService>()
    .RequireAuthorization(); // С авторизацией
#endregion

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
    .RequireAuthorization(); // С авторизацией
#endregion

#region Metrics
app.MapPrometheusScrapingEndpoint().RequireCors(CorsPolicyNames.Metrics); // Телеметрия (/metrics)
#endregion

#region robots.txt, favicon.ico
app.MapShortCircuit(404, "robots.txt", "favicon.ico"); // Т.к у меня нет этих файлов, я могу уменьшить нагрузку на сервер, путём пропуска нескольких Middleware'ов (CORS, Endpoint...)
// (https://andrewlock.net/exploring-the-dotnet-8-preview-short-circuit-routing | https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/routing?view=aspnetcore-9.0#short-circuit-middleware-after-routing)
#endregion

app.Logger.LogInformation("Приложение запущено.");

app.Run();