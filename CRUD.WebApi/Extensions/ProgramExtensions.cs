using CRUD.WebApi.HealthChecks;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.FileProviders;
using OpenTelemetry.Metrics;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.RateLimiting;

namespace CRUD.WebApi.Extensions;

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
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            // В целом, все значения по умолчанию меня устраивают, поэтому менять нечего

            // Можно донастроить конечные точки, т.к далеко не всё можно сделать через appsettings.json. Я нашёл такое применение, порт/протокол в appsettings, а более сложное уже тут, например, connection middleware (https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/servers/kestrel/connection-middleware?view=aspnetcore-10.0#create-custom-connection-middleware)
            // https://learn.microsoft.com/ru-ru/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0#configurationloader
            var kestrelSection = context.Configuration.GetSection("Kestrel");
            options.Configure(kestrelSection)
                .Endpoint("Https", (EndpointConfiguration endpointConfiguration) =>
                {
                    // ...
                })
                .Endpoint("Http", (EndpointConfiguration endpointConfiguration) =>
                {
                    // ...
                });

            options.Limits.MaxConcurrentConnections = 100; // Максимальное количество одновременных соединений
            options.Limits.MaxConcurrentUpgradedConnections = 100; // Максимальное количество обновлённых (соединение, которое было переключено с HTTP на другой протокол) соединений

            options.Limits.Http2.KeepAlivePingDelay = TimeSpan.FromSeconds(30); // Если сервер не получает никаких запросов от клиента в течение 30 секунд, он отправляет клиенту keep-alive пакет для проверки соединения (каждые 30 секунд неактивности отправляются пинги)
            options.Limits.Http2.KeepAlivePingTimeout = TimeSpan.FromMinutes(1); // Если клиент не отвечает на keep-alive или вообще ничего не отправляет в течении минуты - соединение разрывается (закрывает соединение, если в течении минуты не получен ответ)
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
            configuration.Enrich.WithSensitiveDataMasking(options =>
            {
                options.MaskingOperators.Clear(); // По дефолту тут три оператора
                options.MaskingOperators.Add(new AccessTokenMaskingOperator()); // Добавляем маскировку access_token'а
            });
        });
    }

    /// <summary>
    /// Заполняет опции из <see cref="CRUD.Utility.Options"/>, беря данные из <c>appsettings.json</c>.
    /// </summary>
    public static void LoadOptions(this WebApplicationBuilder builder)
    {
        var optionsProgramSection = builder.Configuration.GetSection(ProgramOptions.SectionName);
        builder.Services.Configure<ProgramOptions>(optionsProgramSection); // Заполняем ProgramOptions

        var optionsS3Section = builder.Configuration.GetSection(S3Options.SectionName);
        builder.Services.Configure<S3Options>(optionsS3Section); // Заполняем S3Options

        var optionsS3InitializerSection = builder.Configuration.GetSection(S3InitializerOptions.SectionName);
        builder.Services.Configure<S3InitializerOptions>(optionsS3InitializerSection); // Заполняем S3InitializerOptions

        var optionsEmailSenderSection = builder.Configuration.GetSection(EmailSenderOptions.SectionName);
        builder.Services.Configure<EmailSenderOptions>(optionsEmailSenderSection); // Заполняем EmailSenderOptions

        var optionsSmsSenderSection = builder.Configuration.GetSection(SmsSenderOptions.SectionName);
        builder.Services.Configure<SmsSenderOptions>(optionsSmsSenderSection); // Заполняем SmsSenderOptions

        var optionsTelegramIntegrationSection = builder.Configuration.GetSection(TelegramIntegrationOptions.SectionName);
        builder.Services.Configure<TelegramIntegrationOptions>(optionsTelegramIntegrationSection); // Заполняем TelegramIntegrationOptions

        var optionsPayManagerSection = builder.Configuration.GetSection(PayManagerOptions.SectionName);
        builder.Services.Configure<PayManagerOptions>(optionsPayManagerSection); // Заполняем PayManagerOptions

        var optionsOAuthMailRuSection = builder.Configuration.GetSection(OAuthMailRuOptions.SectionName);
        builder.Services.Configure<OAuthMailRuOptions>(optionsOAuthMailRuSection); // Заполняем OAuthMailRuOptions

        var optionsRateLimiterSection = builder.Configuration.GetSection(RateLimiterOptions.SectionName);
        builder.Services.Configure<RateLimiterOptions>(optionsRateLimiterSection) // Заполняем RateLimiterOptions
            .AddOptionsWithValidateOnStart<RateLimiterOptions>().ValidateDataAnnotations().ValidateOnStart(); // И валидируем через атрибуты DataAnnotations при запуске

        var optionsMetricsSection = builder.Configuration.GetSection(MetricsOptions.SectionName);
        builder.Services.Configure<MetricsOptions>(optionsMetricsSection); // Заполняем MetricsOptions

        var optionsClientsSection = builder.Configuration.GetSection(ClientsOptions.SectionName);
        builder.Services.Configure<ClientsOptions>(optionsClientsSection); // Заполняем ClientsOptions

        // К сожалению, нет возможность провалидировать опции при изменении "на лету", точнее провалидировать можно (только если всё удачно спарсится),
        // а вот, если будет введено not parsing значение, то исключение выбросится только при попытке обратиться к полю через .CurrentValue
        // https://github.com/dotnet/runtime/issues/44381

        var optionsAuthSection = builder.Configuration.GetSection(AuthOptions.SectionName);
        builder.Services.Configure<AuthOptions>(optionsAuthSection); // Заполняем AuthOptions

        var optionsAuthWebApiSection = builder.Configuration.GetSection(AuthWebApiOptions.SectionName);
        builder.Services.Configure<AuthWebApiOptions>(optionsAuthWebApiSection); // Заполняем AuthWebApiOptions

        var optionsAuthEmailSenderSection = builder.Configuration.GetSection(AuthEmailSenderOptions.SectionName);
        builder.Services.Configure<AuthEmailSenderOptions>(optionsAuthEmailSenderSection); // Заполняем AuthEmailSenderOptions

        var optionsSaveLogsToS3BackgroundServiceSection = builder.Configuration.GetSection(SaveLogsToS3BackgroundServiceOptions.SectionName);
        builder.Services.Configure<SaveLogsToS3BackgroundServiceOptions>(optionsSaveLogsToS3BackgroundServiceSection); // Заполняем SaveLogsToS3BackgroundServiceOptions

        var optionsDeleteExpiredRequestsBackgroundServiceSection = builder.Configuration.GetSection(DeleteExpiredRequestsBackgroundServiceOptions.SectionName);
        builder.Services.Configure<DeleteExpiredRequestsBackgroundServiceOptions>(optionsDeleteExpiredRequestsBackgroundServiceSection); // Заполняем DeleteExpiredRequestsBackgroundServiceOptions

        var optionsRevokeExpiredRefreshTokensBackgroundServiceSection = builder.Configuration.GetSection(RevokeExpiredRefreshTokensBackgroundServiceOptions.SectionName);
        builder.Services.Configure<RevokeExpiredRefreshTokensBackgroundServiceOptions>(optionsRevokeExpiredRefreshTokensBackgroundServiceSection); // Заполняем RevokeExpiredRefreshTokensBackgroundServiceOptions

        var optionsChangePasswordRequestSection = builder.Configuration.GetSection(ChangePasswordRequestOptions.SectionName);
        builder.Services.Configure<ChangePasswordRequestOptions>(optionsChangePasswordRequestSection); // Заполняем ChangePasswordRequestOptions

        var optionsConfirmEmailRequestSection = builder.Configuration.GetSection(ConfirmEmailRequestOptions.SectionName);
        builder.Services.Configure<ConfirmEmailRequestOptions>(optionsConfirmEmailRequestSection); // Заполняем ConfirmEmailRequestOptions

        var optionsVerificationPhoneNumberRequestSection = builder.Configuration.GetSection(VerificationPhoneNumberRequestOptions.SectionName);
        builder.Services.Configure<VerificationPhoneNumberRequestOptions>(optionsVerificationPhoneNumberRequestSection); // Заполняем VerificationPhoneNumberRequestOptions

        var optionsAvatarManagerSection = builder.Configuration.GetSection(AvatarManagerOptions.SectionName);
        builder.Services.Configure<AvatarManagerOptions>(optionsAvatarManagerSection); // Заполняем AvatarManagerOptions
    }

    /// <summary>
    /// Настраивает базу данных.
    /// </summary>
    public static void ConfigureDb(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 25)); // Версия на главной странице phpMyAdmin
            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3, // Бывает не с первого раза подключается к базе +реконект полезен и в других случаях
                        maxRetryDelay: TimeSpan.FromSeconds(15),
                        errorNumbersToAdd: null)).EnableDetailedErrors();
        });
    }

    /// <summary>
    /// Настраивает OpenApi.
    /// </summary>
    public static void ConfigureOpenApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); // Кнопка Authorize и применение к запросам
            options.AddOperationTransformer<AcceptLanguageHeaderParameterTransformer>(); // Поле Accept-Language
            options.AddDocumentTransformer<InfoTransformer>(); // Информация об API, контакты
            options.AddOperationTransformer<ProduceTooManyRequestsTransformer>(); // Добавить всем эндпоинтам Produce TooManyRequests
            options.AddDocumentTransformer<HealthzInfoTransformer>(); // Добавляет "/healthz" в Swagger UI
            options.AddDocumentTransformer<MetricsInfoTransformer>(); // Добавляет "/metrics" в Swagger UI
            options.AddDocumentTransformer<TagsDescriptionTransformer>(); // Добавляет описание к тегам
        });

        builder.Services.AddOpenApi("v2", options =>
        {
            // В /openapi/v2.json будет только указанная конечная точка, остальные даже не сгенерируются. НУЖНО закомментировать несколько трансформеров ниже
            //options.ShouldInclude = (apiDescription) => apiDescription.HttpMethod == "GET" && apiDescription.RelativePath == "v2/publications/";

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); // Кнопка Authorize и применение к запросам
            options.AddOperationTransformer<AcceptLanguageHeaderParameterTransformer>(); // Поле Accept-Language
            options.AddDocumentTransformer<InfoTransformer>(); // Информация об API, контакты
            options.AddOperationTransformer<ProduceTooManyRequestsTransformer>(); // Добавить всем эндпоинтам Produce TooManyRequests
            options.AddDocumentTransformer<HealthzInfoTransformer>(); // Добавляет "/healthz" в Swagger UI
            options.AddDocumentTransformer<MetricsInfoTransformer>(); // Добавляет "/metrics" в Swagger UI
            options.AddDocumentTransformer<TagsDescriptionTransformer>(); // Добавляет описание к тегам
        });
    }

    /// <summary>
    /// Настраивает API версионирование.
    /// </summary>
    public static void ConfigureApiVersioning(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = false; // Отключаю автоматическое переадресование, если в URL не указали версию | https://stackoverflow.com/questions/52490065/assumedefaultversionwhenunspecified-is-not-working-as-expected
            //options.DefaultApiVersion = new ApiVersion(1, 0); // Нужно для AssumeDefaultVersionWhenUnspecified, например https://localhost:7260/publications?count=2 будет переадресован на https://localhost:7260/v1/publications?count=2
            options.ReportApiVersions = true; // Добавлять ли в заголовок ответа "api-supported-versions", "api-deprecated-versions"
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Версию указывается по URL
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V"; // v2, v2.0, но не v2.0.0 https://localhost:7260/v2.0/publications?count=1 // https://github.com/dotnet/aspnet-api-versioning/wiki/Version-Format#custom-api-version-format-strings
            options.SubstituteApiVersionInUrl = true; // Для подставки в роут-параметры
        });
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
                // Разрешаем только нашему клиенту (сайту)
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
    /// Настраивает Output Cache.
    /// </summary>
    /// <remarks>
    /// Кэширование HTTP ответов.
    /// </remarks>
    public static void ConfigureOutputCache(this WebApplicationBuilder builder)
    {
        // Подключение Redis к OutputCache
        builder.Services.AddStackExchangeRedisOutputCache(options =>
        {
            options.InstanceName = "localOutput";

            // Более гибкая настройка, чем "options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");"
            options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
            {
                EndPoints = new StackExchange.Redis.EndPointCollection()
                {
                    { builder.Configuration.GetConnectionString("RedisConnection")! } // HostAndPort
                },
                ConnectTimeout = 1000, // Не больше секунды на подключение. Если, например, к редису не удалось подключиться во время запроса "/publications?count=1", то API ответит только через секунду, т.к будет пытаться подключиться
                SyncTimeout = 1000 // Работает в паре с ConnectTimeout, иначе не меняется. Хотя в RedisConnectionHealthCheck без него работает
            };
        });

        builder.Services.AddOutputCache(options =>
        {
            // Политика по умолчанию. 200; GET, HEAD; запросы авторизованного пользователя не кэшируются; но время переопределенно | https://learn.microsoft.com/ru-ru/aspnet/core/performance/caching/output?view=aspnetcore-9.0#default-output-caching-policy
            options.AddBasePolicy(builder =>
                builder.Expire(TimeSpan.FromSeconds(10)));

            options.AddPolicy("Expire20", builder =>
                builder.Expire(TimeSpan.FromSeconds(20)));
        });
    }

    /// <summary>
    /// Настраивает Hybrid Cache.
    /// </summary>
    /// <remarks>
    /// Внутренее кэширование приложения с Redis.
    /// </remarks>
    public static void ConfigureHybridCache(this WebApplicationBuilder builder)
    {
        builder.Services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // Максимальный размер кэша в байтах
            options.MaximumKeyLength = 1024; // Максимальная длина ключа в символах
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5), // Время истечения для Redis (Distributed)
                LocalCacheExpiration = TimeSpan.FromMinutes(5) // Время истечения для приложения (Memory)
            };
        });

        // Подключение Redis к HybridCache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
            options.InstanceName = "localHybrid";
        });
    }

    /// <summary>
    /// Настраивает Authentication.
    /// </summary>
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        // Донастройка TokenValidationParameters, т.к я использую IOptionsMonitor, чтобы обновлять данные на лету. И соответственно, конфигурацию ниже тоже нужно обновлять после изменения
        builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, PostConfigureJwtBearerOptions>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Указывает, будет ли валидироваться издатель при валидации токена
                    ValidateAudience = true, // Будет ли валидироваться потребитель токена
                    ValidateLifetime = true, // Будет ли валидироваться время существования
                    ValidateIssuerSigningKey = true, // Валидация ключа безопасности

                    // Остальные параметры вписываются через PostConfigureJwtBearerOptions, т.к я использую IOptionsMonitor, чтобы обновлять данные на лету
                    // Если бы я использовал IOptions, то можно было бы спокойно получить опции (т.к она неизменяемая, пока не перезапустить приложение)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // Если запрос в хаб. SignalR не умеет передавать JWT-токен в заголовке, он его передаёт через строку запроса
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
                            context.Token = accessToken; // Токен из строки запроса вписываем в токен контекста

                        return Task.CompletedTask;
                    }
                };
            });
    }

    /// <summary>
    /// Настраивает Authorization.
    /// </summary>
    public static void ConfigureAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(UserRoles.Admin, policy =>
            {
                policy.RequireRole(UserRoles.Admin);
            })
            .AddPolicy("OnlyPremium", policy =>
            {
                policy.RequireClaim("premium", "true", "True");
            })
            .AddDefaultPolicy("LanguageDeny", policy =>
            {
                //policy.AddRequirements(new LanguageDeny("en"));
                policy.AddRequirements(new LanguageDeny("uk"));
            });
    }

    /// <summary>
    /// Настраивает <see cref="ForwardedHeadersOptions"/>.
    /// </summary>
    public static void ConfigureForwardedHeadersOptions(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // https://learn.microsoft.com/ru-ru/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-10.0&tabs=linux-ubuntu#use-a-reverse-proxy-server
            // Если прокси-сервер на одной машине с приложением, то адрес, считается доверенным (т.к 127.0.0.1) и его не нужно указывать в KnownProxies или KnownNetworks
            // Если прокси-сервер находится удалённо, то обязательно нужно указать KnownProxies (если IP-адрес один) или KnownNetworks (если IP-адресов несколько, например, целый кластер)

            var proxyUrl = builder.Configuration.GetValue<string>("Tuna") ?? string.Empty;
            var address = ForwardedHeadersHelper.GetIpByDomain(proxyUrl.Replace("https://", "")); // Получаю IP по домену | для прода указывать IP своего прокси (если удалённый)
            options.KnownProxies.Add(address); // IP моего прокси Tuna | Важно указать прокси, которому я доверяю, иначе можно подделать заголовок
        });
    }

    /// <summary>
    /// Настраивает RateLimiter.
    /// </summary>
    public static void ConfigureRateLimiter(this WebApplicationBuilder builder)
    {
        RateLimiterOptions rateLimiterOptions = builder.Configuration.GetSection(RateLimiterOptions.SectionName).Get<RateLimiterOptions>()!;

        builder.Services.AddRateLimiter(options =>
        {
            // Глобальный лимитер
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userRole = httpContext.User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                // Если пользователь админ, то не ограничиваем доступ
                if (userRole == UserRoles.Admin)
                    return RateLimitPartition.GetNoLimiter(userRole);

                // Лимитер для "/publications... GET"
                string path = httpContext.Request.Path.ToString();
                var apiVersion = httpContext.GetRequestedApiVersion();
                if (path.StartsWith($"/v{apiVersion}/publications")
                    && httpContext.Request.Method == "GET")
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{httpContext.Connection.RemoteIpAddress}-public",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimiterOptions.PublicationsGet.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimiterOptions.PublicationsGet.Window),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rateLimiterOptions.PublicationsGet.QueueLimit
                        });

                // Лимитер для "/metrics..."
                if (path.StartsWith("/metrics"))
                    return RateLimitPartition.GetNoLimiter(path);

                // Основной лимитер
                return RateLimitPartition.GetFixedWindowLimiter(
                   partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                   factory: partition => new FixedWindowRateLimiterOptions
                   {
                       PermitLimit = rateLimiterOptions.Global.PermitLimit,
                       QueueLimit = rateLimiterOptions.Global.QueueLimit,
                       Window = TimeSpan.FromSeconds(rateLimiterOptions.Global.Window)
                   });
            });

            // При достижении лимита
            options.OnRejected = async (context, ct) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IResourceLocalizer>();

                var problem = TypedResults.Extensions.Problem(ApiErrorConstants.RateLimitExceeded, localizer);

                // Устанавливаем заголовок RetryAfter
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

                await problem.ExecuteAsync(context.HttpContext);
            };
        });
    }

    /// <summary>
    /// Настраивает <see cref="IHttpClientFactory"/> и клиентов через <see cref="HttpClientFactoryServiceCollectionExtensions.AddHttpClient(IServiceCollection, string)"/>.
    /// </summary>
    public static void ConfigureHttpClientFactory(this WebApplicationBuilder builder)
    {
        PayManagerOptions payManagerOptions = builder.Configuration.GetSection(PayManagerOptions.SectionName).Get<PayManagerOptions>()!;
        SmsSenderOptions smsSenderOptions = builder.Configuration.GetSection(SmsSenderOptions.SectionName).Get<SmsSenderOptions>()!;
        TelegramIntegrationOptions telegramIntegrationOptions = builder.Configuration.GetSection(TelegramIntegrationOptions.SectionName).Get<TelegramIntegrationOptions>()!;
        EmailSenderOptions emailSenderOptions = builder.Configuration.GetSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>()!;
        MetricsOptions metricsOptions = builder.Configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>()!;

        builder.Services.AddHttpClient();

        // PayManager
        builder.Services.AddHttpClient(HttpClientNames.PayManager, options =>
        {
            options.BaseAddress = new Uri(payManagerOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{payManagerOptions.ShopId}:{payManagerOptions.ApiKey}")));
            // ASCII, потому что это стандарт для "Basic" (хотя сейчас уже на UTF-8 переходят по RFC) +мне достаточно символов ASCII, без всяких китайских символов, и кириллицы.
            // Т.е, если, например, в ShopId будет кириллица, то она будет кодироваться знаками "?".
            // Ну, и поддержку старых систем никто не отменял, а новые будут работать, т.к символы ASCII в кодировке UTF-8 в точности совпадают с их кодировкой в ​​ASCII
            options.DefaultRequestHeaders.Add("Idempotence-Key", Guid.NewGuid().ToString());
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // SmsSender
        builder.Services.AddHttpClient(HttpClientNames.SmsSender, options =>
        {
            options.BaseAddress = new Uri(smsSenderOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{smsSenderOptions.Email}:{smsSenderOptions.ApiKey}")));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // TelegramIntegration
        builder.Services.AddHttpClient(HttpClientNames.TelegramIntegration, options =>
        {
            options.BaseAddress = new Uri(telegramIntegrationOptions.ServiceURL);

            // Авторизация для каждого запроса
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", telegramIntegrationOptions.ApiKey);
            options.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // EmailSender
        builder.Services.AddHttpClient(HttpClientNames.EmailSender, (serviceProvider, options) =>
        {
            options.BaseAddress = new Uri(emailSenderOptions.ServiceURL);

            // Авторизация для каждого запроса
            using var scope = serviceProvider.CreateScope();
            var grpcTokenManager = scope.ServiceProvider.GetRequiredService<IGrpcTokenManager>();
            var token = grpcTokenManager.GenerateAuthEmailSenderToken();
            options.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // Prometheus
        builder.Services.AddHttpClient(HttpClientNames.Prometheus, (serviceProvider, options) =>
        {
            options.BaseAddress = new Uri(metricsOptions.PrometheusURL);
        })
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600)));

        // PollyWaitAndRetry
        // Неудачные (мои) запросы повторяются до трех раз с задержкой 600 мс между попытками
        builder.Services.AddHttpClient(HttpClientNames.PollyWaitAndRetry)
            .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.WaitAndRetryAsync(3, retryNumber => TimeSpan.FromMilliseconds(600))); // В счёт идут только 5XX, 408, System.Net.Http.HttpRequestException

        // PollyDynamic
        // Если исходящий (мой) запрос является запросом GET, применяется время ожидания 10 секунд. Для остальных методов время ожидания — 20 секунд
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var longTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(20));
        builder.Services.AddHttpClient(HttpClientNames.PollyDynamic)
            .AddPolicyHandler(httpRequestMessage => httpRequestMessage.Method == HttpMethod.Get ? timeoutPolicy : longTimeoutPolicy);
    }

    /// <summary>
    /// Настраивает HealthChecks.
    /// </summary>
    public static void ConfigureHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseConnectionHealthCheck>(nameof(DatabaseConnectionHealthCheck)) // Проверка подключения к базе
            .AddCheck<DatabaseConsistencyHealthCheck>(nameof(DatabaseConsistencyHealthCheck)) // Проверка консистенции базы данных (существуют ли все таблицы)
            .AddCheck<S3ConnectionHealthCheck>(nameof(S3ConnectionHealthCheck)) // Проверка подключения к S3
            .AddCheck<S3ConsistencyHealthCheck>(nameof(S3ConsistencyHealthCheck)) // Проверка консистенции S3 (существуют ли все объекты)
            .AddCheck<RedisConnectionHealthCheck>(nameof(RedisConnectionHealthCheck)) // Проверка подключения к Redis серверу
            .AddCheck<EmailConnectionHealthCheck>(nameof(EmailConnectionHealthCheck)) // Проверка подключения к Email серверу
            .AddCheck<SmsConnectionHealthCheck>(nameof(SmsConnectionHealthCheck)) // Проверка подключения к СМС серверу
            .AddCheck<TelegramConnectionHealthCheck>(nameof(TelegramConnectionHealthCheck)) // Проверка подключения к Telegram
            .AddCheck<PaymentConnectionHealthCheck>(nameof(PaymentConnectionHealthCheck)) // Проверка подключения к платёжному серверу
            .AddCheck<PrometheusConnectionHealthCheck>(nameof(PrometheusConnectionHealthCheck)) // Проверка подключения к Prometheus серверу
            .AddCheck<HubsConnectionHealthCheck>(nameof(HubsConnectionHealthCheck)) // Проверка подключения к хабам
            .AddCheck<OAuthMailRuConnectionHealthCheck>(nameof(OAuthMailRuConnectionHealthCheck)) // Проверка подключения к OAuth MailRu
            .AddCheck<RabbitMqConnectionHealthCheck>(nameof(RabbitMqConnectionHealthCheck)); // Проверка подключения к RabbitMQ
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
                    "Microsoft.AspNetCore.Http.Connections",
                    ApiMeters.MeterName);

                // Grpc.Net.Client метрики (в отличии от трассировок) используют EventCounter, поэтому просто добавить в AddMeter не выйдет (https://github.com/open-telemetry/opentelemetry-dotnet-contrib/issues/1618)
                builder.AddEventCountersInstrumentation(options =>
                {
                    options.AddEventSources("Grpc.Net.Client");
                });
            });
    }

    /// <summary>
    /// Настраивает клиенты gRPC.
    /// </summary>
    public static void ConfigureGrpcClients(this WebApplicationBuilder builder)
    {
        var emailSenderOptions = builder.Configuration.GetSection(EmailSenderOptions.SectionName).Get<EmailSenderOptions>()!;

        // EmailSender
        builder.Services.AddGrpcClient<GrpcEmailSender.GrpcEmailSenderClient>(GrpcClientNames.GrpcEmailSender, options =>
        {
            options.Address = new Uri(emailSenderOptions.ServiceURL); // Url-адрес сервиса
        })
            .ConfigureChannel(options => // Настройка канала
            {
                // Политика повторов (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/retries?view=aspnetcore-10.0)
                var defaultMethodConfig = new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = 3, // Максимальное количество попыток, считая первый запрос
                        InitialBackoff = TimeSpan.FromSeconds(1), // Начальная задержка между повторными попытками
                        MaxBackoff = TimeSpan.FromSeconds(3), // Верхний предел для увеличения экспоненциальной задержки (не больше 3 секунд задержка)
                        BackoffMultiplier = 1.5, // Множитель задержки (1с - 1.5с - 2.25с...) +это не точные числа, т.к внутри применяется рандом, чтобы не допустить объединения повторных попыток из нескольких вызовов в кластер и потенциальной перегрузки сервера
                        RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable } // Статус коды, на которые будет отрабатывать политика повторов
                    }
                };
                options.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
                options.MaxRetryAttempts = 3; // Что не было бы написано в конфиге выше, даём ограничение в 3 попытки +эта настройка только ограничивает, но не включает сам механизм повторов
                //options.LoggerFactory берётся из DI автоматически (https://learn.microsoft.com/en-us/aspnet/core/grpc/clientfactory?view=aspnetcore-10.0#configure-channel) +для Grpc.Net.Client.Internal.GrpcCall я указал минимальный уровень "Error"

                // Держать соединение открытым (keep-alive) (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/performance?view=aspnetcore-10.0#keep-alive-pings)
                // +сервер микросервиса должен поддерживать keep-alive (options.Limits.KeepAliveTimeout)
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan, // Как долго соединение может быть неактивным, чтобы можно было переиспользовать соединение
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60), // Раз в 60 секунд пинговать (отправляет пакет keep-alive на сервер каждые 60 секунд в периоды бездействия)
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30) // Если сервер в течении 30 секунд не ответит - разрываем соединение
                };

                // Взаимодействовать ли через Unix Domain Socket вместо TCP (в основном, если EmailSender и WebApi на одном ПК)
                if (emailSenderOptions.UseUnixDomainSocketGRPC)
                {
                    var udsEndPoint = new UnixDomainSocketEndPoint(Path.Combine(Path.GetTempPath(), emailSenderOptions.FileNameInTempFolder));
                    var connectionFactory = new UnixDomainSocketsConnectionFactory(udsEndPoint);
                    handler.ConnectCallback = connectionFactory.ConnectAsync;
                }

                options.HttpHandler = handler;
            })
            .AddCallCredentials((context, metadata, serviceProvider) => // Аутентификация и авторизация
            {
                using var scope = serviceProvider.CreateScope();
                var grpcTokenManager = scope.ServiceProvider.GetRequiredService<IGrpcTokenManager>();
                var token = grpcTokenManager.GenerateAuthEmailSenderToken();
                metadata.Add("Authorization", $"Bearer {token}");

                return Task.CompletedTask;
            });
        //.EnableCallContextPropagation(); // Передавать ct, deadline во внутрение вызовы сервисов АВТОМАТИЧЕСКИ (можно ручками через контекст) (https://learn.microsoft.com/ru-ru/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-10.0#propagating-deadlines)
        // У меня внутрених вызовов нет, у меня только один микросервис. Необходим нугет Grpc.AspNetCore.Server.ClientFactory
    }

    /// <summary>
    /// Использует готовую настройку статических файлов и отображение иерархии файлов в браузере.
    /// </summary>
    /// <remarks>
    /// <see cref="StaticFileExtensions.UseStaticFiles(IApplicationBuilder, StaticFileOptions)"/> и <see cref="DirectoryBrowserExtensions.UseDirectoryBrowser(IApplicationBuilder, DirectoryBrowserOptions)"/>.
    /// </remarks>
    public static void UseReadyStaticFilesAndDirectoryBrowser(this WebApplication app)
    {
        // Забавный момент, согласно документации, UseStaticFiles не использует сжатие при публикации, но в .NET 9 сжимает, т.к это на уровне SDK, немного непредсказуемое поведение, но меня устраивает
        // https://github.com/dotnet/aspnetcore/issues/59518

        var fileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "public"));
        app.UseStaticFiles(new StaticFileOptions
        {
            // Клиент может получить только файлы из папки public
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
            {
                if (context.File.Name == "readme.txt")
                {
                    context.Context.Response.Headers.Append("Content-Type", "text/plain; charset=utf-8"); // Файл на кириллице, поэтому utf-8
                    context.Context.Response.Headers.Append("Cache-Control", "public, max-age=604800"); // Файл может хранится в кэше неделю
                }
            },
            RequestPath = "/public" // Используем путь "https://localhost:7260/public/readme.txt", а не https://localhost:7260/readme.txt
        });

        // Отображение иерархии папок в браузере для удобства
        app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            FileProvider = fileProvider, // Отображаем только папку public
            RequestPath = "/public" // Чтобы отобразить иерархию нужно перейти в "/public", а не как по умолчанию "/"
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