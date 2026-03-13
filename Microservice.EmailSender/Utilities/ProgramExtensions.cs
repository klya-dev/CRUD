using Microservice.EmailSender.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;

namespace Microservice.EmailSender.Utilities;

/// <summary>
/// Расширения для настройки приложения.
/// </summary>
/// <remarks>
/// Используется в <c>Program.cs</c>.
/// </remarks>
public static class ProgramExtensions
{
    /// <summary>
    /// Настраивает сервер.
    /// </summary>
    public static void ConfigureServer(this WebApplicationBuilder builder)
    {
        ProgramOptions programOptions = builder.Configuration.GetSection(ProgramOptions.SectionName).Get<ProgramOptions>()!;

        // UseKestrelHttpsConfiguration нужен для SlimBuilder
        builder.WebHost.UseKestrelHttpsConfiguration().ConfigureKestrel(options =>
        {
            options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2); // Если клиент не запингует сервер в течении двух минут - разрываем соединение (как долго сервер будет поддерживать неактивное соединение, прежде чем закрыть его)

            // Взаимодействовать ли через Unix
            if (programOptions.UseUnixDomainSocketGRPC == true)
            {
                // Слушаем "https://unix:C:\Temp\socket.tmp"
                // А, чтобы https://localhost:7261 был всё ещё открыт, нужно прописать в appsettings.json Kestrel:Endpoints:Http/Https
                // В итоге: unix для gRPC, а localhost для метрик, healtz и тд
                var socketPath = Path.Combine(Path.GetTempPath(), programOptions.FileNameInTempFolder);

                // Если файл "socket.tmp" существует - удаляем. Иначе ошибка: "Failed to bind to address unix address already in use" (якобы этот адрес уже используется)
                if (File.Exists(socketPath))
                    File.Delete(socketPath);

                options.ListenUnixSocket(socketPath, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    listenOptions.UseHttps();
                });
            }
        });
    }

    /// <summary>
    /// Настраивает логирование.
    /// </summary>
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        S3Options s3Options = builder.Configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;

        Console.OutputEncoding = System.Text.Encoding.UTF8; // Нормальная кодировка в консоле вместо "<" - "«", и другие мелочи
        builder.Logging.ClearProviders(); // Убираем ConsoleLoggerProvider, DebugLoggerProvider, EventSourceLoggerProvider, EventLogLoggerProvider
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration.Filter.ByExcluding(logEvent =>
            {
                // Исключаем некоторые эндпоинты из логирования
                if (logEvent.Properties.TryGetValue("RequestPath", out var pathProperty)
                    && logEvent.Level <= LogEventLevel.Information // Логируем Warning или выше
                    && pathProperty is ScalarValue scalarPath
                    && scalarPath.Value is string requestPath)
                {
                    return requestPath == "/metrics" || requestPath == "/healthz"; // Можно через StartWith
                }
                return false;
            });
            configuration.WriteTo.Console(outputTemplate: "[{ApplicationName}] [{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            configuration.WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, s3Options.LogsDirectory, "log-.txt"),
                outputTemplate: "[{ApplicationName}] [{SourceContext}] [{Timestamp:dd.MM.yyyy HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null);
            configuration.ReadFrom.Configuration(context.Configuration);
        });
    }

    /// <summary>
    /// Заполняет опции из <see cref="Options"/>, беря данные из <c>appsettings.json</c>.
    /// </summary>
    public static void LoadOptions(this WebApplicationBuilder builder)
    {
        var optionsProgramSection = builder.Configuration.GetSection(ProgramOptions.SectionName);
        builder.Services.Configure<ProgramOptions>(optionsProgramSection); // Заполняем ProgramOptions

        var optionsS3Section = builder.Configuration.GetSection(S3Options.SectionName);
        builder.Services.Configure<S3Options>(optionsS3Section); // Заполняем S3Options

        var optionsSmtpServerSection = builder.Configuration.GetSection(SmtpServerOptions.SectionName);
        builder.Services.Configure<SmtpServerOptions>(optionsSmtpServerSection); // Заполняем SmtpServerOptions

        var optionsEmailSenderBackgroundServiceSection = builder.Configuration.GetSection(EmailSenderBackgroundServiceOptions.SectionName);
        builder.Services.Configure<EmailSenderBackgroundServiceOptions>(optionsEmailSenderBackgroundServiceSection); // Заполняем EmailSenderBackgroundServiceOptions

        var optionsMetricsSection = builder.Configuration.GetSection(MetricsOptions.SectionName);
        builder.Services.Configure<MetricsOptions>(optionsMetricsSection); // Заполняем MetricsOptions

        var optionsClientsSection = builder.Configuration.GetSection(ClientsOptions.SectionName);
        builder.Services.Configure<ClientsOptions>(optionsClientsSection); // Заполняем ClientsOptions

        var optionsAuthSection = builder.Configuration.GetSection(AuthOptions.SectionName);
        builder.Services.Configure<AuthOptions>(optionsAuthSection); // Заполняем AuthOptions

        var optionsSaveLogsToS3BackgroundServiceSection = builder.Configuration.GetSection(SaveLogsToS3BackgroundServiceOptions.SectionName);
        builder.Services.Configure<SaveLogsToS3BackgroundServiceOptions>(optionsSaveLogsToS3BackgroundServiceSection); // Заполняем SaveLogsToS3BackgroundServiceOptions
    }

    /// <summary>
    /// Настраивает CORS.
    /// </summary>
    public static void ConfigureCors(this WebApplicationBuilder builder)
    {
        var metricsOptions = builder.Configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>()!;
        var clientsOptions = builder.Configuration.GetSection(ClientsOptions.SectionName).Get<ClientsOptions>()!;

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                // Разрешаем только нашему клиенту (WebApi)
                builder.WithOrigins(clientsOptions.WebClientURLs)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Разрешает отправку данных
            });

            options.AddPolicy(CorsPolicyNames.AllowAll, builder =>
            {
                builder.AllowAnyOrigin() // Принимаем запросы с любого адреса
                       .AllowAnyMethod() // С любыми методами (GET, POST...)
                       .AllowAnyHeader(); // С любыми заголовками
            });

            options.AddPolicy(CorsPolicyNames.Metrics, builder =>
            {
                // Разрешаем только нашему Prometheus серверу
                builder.WithOrigins(metricsOptions.PrometheusURL)
                    .WithMethods(HttpMethods.Get)
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }

    /// <summary>
    /// Настраивает Authentication.
    /// </summary>
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        // Донастройка TokenValidationParameters, чисто ради логгера, так бы и тут настроил
        builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, PostConfigureJwtBearerOptions>();

        var options = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()!;
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                
            });
    }

    /// <summary>
    /// Настраивает Authorization.
    /// </summary>
    public static void ConfigureAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();
    }

    /// <summary>
    /// Настраивает HealthChecks.
    /// </summary>
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<S3ConnectionHealthCheck>(nameof(S3ConnectionHealthCheck)) // Проверка подключения к S3
            .AddCheck<EmailConnectionHealthCheck>(nameof(EmailConnectionHealthCheck)) // Проверка подключения к Email серверу
            .AddCheck<PrometheusConnectionHealthCheck>(nameof(PrometheusConnectionHealthCheck)) // Проверка подключения к Prometheus серверу
            .AddCheck<RabbitMqConnectionHealthCheck>(nameof(RabbitMqConnectionHealthCheck)); // Проверка подключения к RabbitMQ серверу
    }

    /// <summary>
    /// Настраивает OpenTelemetry.
    /// </summary>
    public static void ConfigureOpenTelemetry(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddPrometheusExporter();

                // https://learn.microsoft.com/ru-ru/aspnet/core/log-mon/metrics/built-in?view=aspnetcore-10.0
                builder.AddMeter(
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Diagnostics",
                    "Microsoft.AspNetCore.RateLimiting",
                    "Microsoft.AspNetCore.Server.Kestrel",
                    "Microsoft.AspNetCore.Http.Connections");

                // Grpc.AspNetCore.Server метрики (в отличии от трассировок) используют EventCounter, поэтому просто добавить в AddMeter не выйдет (https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1618)
                builder.AddEventCountersInstrumentation(options =>
                {
                    options.AddEventSources("Grpc.AspNetCore.Server");
                });
            });
    }

    /// <summary>
    /// Использует готовую настройку логирования HTTP-запросов.
    /// </summary>
    /// <remarks>
    /// <see cref="Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(IApplicationBuilder, Action{Serilog.AspNetCore.RequestLoggingOptions}?)"/>.
    /// </remarks>
    public static void UseReadyRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging((configuration) =>
        {
            // Добавляю кастомное свойство для SerilogRequestLogging, т.к свойства NewLine по умолчанию нет
            // А я хочу сделать читабельный Request Logging
            configuration.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("NewLine", Environment.NewLine);
                diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
            };
            configuration.IncludeQueryInRequestPath = true;
            configuration.MessageTemplate = "{Protocol} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms{NewLine}";
        });
    }
}