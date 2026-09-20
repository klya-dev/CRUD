namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения аутентификации и авторизации.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Добавляет и настраивает аутентификацию.
    /// </summary>
    public static IServiceCollection AddCustomAuthentication(this IServiceCollection services)
    {
        // Донастройка TokenValidationParameters, т.к я использую IOptionsMonitor, чтобы обновлять данные на лету. И соответственно, конфигурацию ниже тоже нужно обновлять после изменения
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, PostConfigureJwtBearerOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Указание options.Authority и options.Audience, нужно обычно только для автоматического скачивания публичных ключей по пути "/.well-known/openid-configuration" (options.MetadataAddress)
                // Конкретно в WebApi, мне этого делать не нужно, т.к монолит, и не нужно скачивать ключи по URL
                // А в EmailSender это бы пригодилось, но у меня нет "openid-configuration", есть только "jwks.json" отдельно, поэтому в микросервисе у меня другая реализация скачивания публичных ключей (кастомный парсер)

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Указывает, будет ли валидироваться издатель при валидации токена
                    ValidateAudience = true, // Будет ли валидироваться потребитель токена
                    ValidateLifetime = true, // Будет ли валидироваться время существования
                    ValidateIssuerSigningKey = true, // Валидация ключа безопасности

                    // Остальные параметры вписываются через PostConfigureJwtBearerOptions, т.к я использую IOptionsMonitor, чтобы обновлять данные на лету
                    // Если бы я использовал IOptions, то можно было бы спокойно получить опции (т.к IOptions неизменяемый, пока не перезапустить приложение)
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

        return services;
    }

    /// <summary>
    /// Добавляет и настраивает авторизацию.
    /// </summary>
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicyNames.OnlyAdmin, policy => // Только Admin
            {
                // Для админа нет ограничений на язык
                policy.RequireRole(UserRoles.Admin); // Под капотом есть авторизация https://stackoverflow.com/q/58948479/31342728
            })
            .AddPolicy(AuthorizationPolicyNames.OnlyPremium, policy => // Только премиум
            {
                policy.RequireAuthenticatedUser(); // Требуем авторизацию, т.к у RequireClaim под капотом нет авторизации https://stackoverflow.com/q/64275186/31342728
                OnlyPermittedLanguagesRequirement(policy); // Ограничение на язык
                policy.RequireClaim(UserClaimTypes.IsPremium, "true", "True");
            })
            .AddPolicy(AuthorizationPolicyNames.OnlyEmailConfirmed, policy => // Только подтверждённая почта
            {
                policy.RequireAuthenticatedUser(); // Требуем авторизацию
                OnlyPermittedLanguagesRequirement(policy); // Ограничение на язык
                policy.RequireClaim(UserClaimTypes.IsEmailConfirm, "true", "True");
            })
            .AddPolicy(AuthorizationPolicyNames.OnlyPhoneNumberConfirmed, policy => // Только подтверждённый номер телефона
            {
                policy.RequireAuthenticatedUser(); // Требуем авторизацию
                OnlyPermittedLanguagesRequirement(policy); // Ограничение на язык
                policy.RequireClaim(UserClaimTypes.IsPhoneNumberConfirm, "true", "True");
            })
            .AddDefaultPolicy(AuthorizationPolicyNames.OnlyPermittedLanguages, policy => // Политика по умолчанию ([Authorize] без параметров). Только разрешённые языки
            {
                policy.RequireAuthenticatedUser(); // Требуем авторизацию, т.к грубо говоря, прописывая AddDefaultPolicy мы это переопределили
                OnlyPermittedLanguagesRequirement(policy); // Ограничение на язык
            })
            //.SetFallbackPolicy(new AuthorizationPolicyBuilder() // Резервная политика авторизации для всех конечных точек, у которых нет атрибутов авторизации. Анонимный пользователь не попадет ни в один метод, кроме тех, что помечены [AllowAnonymous] явно
            //    .RequireAuthenticatedUser() // Грубо говоря, всем конечным точкам по умолчанию указывается [Authorize]. Поэтому для другой схемы или политики нужно это указать в атрибуте ([Authorize(Policy = ...)]), и все анонимные конечные точки помечаем [AllowAnonymous] явно (в том числе Swagger)
            //    .Build()) // Т.е я сейчас могу удалить все RequireAuthorization (без параметров) (НО ТОГДА DefaultPolicy не сработает). AllowAnonymous, конечно, оставляем
            .SetInvokeHandlersAfterFailure(false); // Я не хочу, чтобы выполнялись следующие обработчики (требований), если хоть один обработчик вернул Fail (https://learn.microsoft.com/ru-ru/aspnet/core/security/authorization/policies?view=aspnetcore-10.0#what-should-a-handler-return)

        // P.S.: Убрал FallbackPolicy, мне не нравится, как возвращается 401, вместо 404, например, если параметр конечной точки указан неверно (IncorrectDataEndpointSystemTest)
        // Также приходится выдумывать велосипед для UseStaticFiles, чтобы разрешить анонимный доступ (если авторизация ниже в pipeline'е), прописывать AllowAnonymous для MapPrometheusScrapingEndpoint, MapOpenApi
        // Мне привычнее, когда авторизации по умолчанию нет, и я сам ручками указываю .RequireAuthorization() или .AllowAnonymous() явно
        // Также, если FallbackPolicy включен, а private.txt в wwwroot, но он не входит в публичную папку, вернётся 401 вместо 404, т.к ответа на эту конечную точку никто не дал, а проверку RequireAuthenticatedUser нужно провести - так и получается 401
        // *даже если UseAuthorization ниже, чем UseStaticFiles (UseStaticFiles пропускает запрос дальше, т.к private.txt не относится к публичной папке)

        // Вот как это работает:
        // DefaultPolicy: срабатывает, если УКАЗАН [Authorize] без параметров. Может показаться, что дефолтная политика учитывается во всех политиках, но это не так. Она срабатывает ТОЛЬКО, если указан [Authorize] без параметров
        // FallbackPolicy: срабатывает, если НЕ указан атрибут [Authorize]. И при этом DefaultPolicy не сработает
        // Именованная политика: если указано имя [Authorize(Policy = ...)], проверяется только эта политика (без учёта Default и Fallback).
        // Поэтому я указываю OnlyPermittedLanguagesRequirement в других политиках

        // Учитывая, настойки выше (SetFallbackPolicy указан):
        // .RequireAuthorization() НЕ указан - сработает FallbackPolicy, которая требует просто авторизацию
        // .RequireAuthorization() указан - сработает DefaultPolicy, которая требует авторизацию и проверяет язык
        // .RequireAuthorization(AuthorizationPolicyNames.OnlyAdmin) указан - сработает OnlyAdmin политика, которая требует авторизацию (под капотом) и проверяет роль пользователя
        // .RequireAuthorization(AuthorizationPolicyNames.OnlyPremium) указан - сработает OnlyPremium политика, которая требует авторизацию, проверяет язык и проверяет наличие премиума

        // Суммирование политик:
        // Если в группе конечных точек стоит .RequireAuthorization без параметров: var publicationsMap = app.MapGroup("/publications").RequireAuthorization();
        // А в самой конечной точке стоит .RequireAuthorization(AuthorizationPolicyNames.OnlyEmailConfirmed, AuthorizationPolicyNames.OnlyPhoneNumberConfirmed)
        // То тогда политики ПЛЮСУЮТСЯ, сработает DefaultPolicy и две именованных политики (необязательно должна быть группа, можно несколько раз вызвать .RequireAuthorization в конечной точке)

        // Вызов _logger.LogDebug("Requirements: {requirements}", context.Requirements); в LanguageDenyHandler:
        // Логирование произойдёт ТРИ раза (т.к в сумме три политики)
        // DenyAnonymousAuthorizationRequirement,LanguageDenyRequirement,DenyAnonymousAuthorizationRequirement,LanguageDenyRequirement,ClaimsAuthorizationRequirement:email_confirm,DenyAnonymousAuthorizationRequirement,LanguageDenyRequirement,ClaimsAuthorizationRequirement:phonenumber_confirm
        // Разбор:
        // 1) DenyAnonymousAuthorizationRequirement, LanguageDenyRequirement - DefaultPolicy
        // 2) DenyAnonymousAuthorizationRequirement, LanguageDenyRequirement, ClaimsAuthorizationRequirement:email_confirm - Именованная политика OnlyEmailConfirmed
        // 3) DenyAnonymousAuthorizationRequirement, LanguageDenyRequirement, ClaimsAuthorizationRequirement:phonenumber_confirm - Именованная политика OnlyPhoneNumberConfirmed

        // Если же убрать .RequireAuthorization из группы, то сработают только две именованные политики
        // DenyAnonymousAuthorizationRequirement,LanguageDenyRequirement,ClaimsAuthorizationRequirement:email_confirm,DenyAnonymousAuthorizationRequirement,LanguageDenyRequirement,ClaimsAuthorizationRequirement:phonenumber_confirm
        // Разбор:
        // 1) DenyAnonymousAuthorizationRequirement, LanguageDenyRequirement, ClaimsAuthorizationRequirement:email_confirm - Именованная политика OnlyEmailConfirmed
        // 2) DenyAnonymousAuthorizationRequirement, LanguageDenyRequirement, ClaimsAuthorizationRequirement:phonenumber_confirm - Именованная политика OnlyPhoneNumberConfirmed

        /// <summary>
        /// Только разрешённые языки.
        /// </summary>
        static void OnlyPermittedLanguagesRequirement(AuthorizationPolicyBuilder policy)
        {
            policy.AddRequirements(new LanguageDenyRequirement(["ua", "ww"])); // Список запрещённых языков

            // Если регистрировать несколько раз, то и логика будет выполняться несколько раз (несколько логирований, несколько поисков значения из claim'ов), поэтому я решил передавать коллекцию
            //policy.AddRequirements(new LanguageDenyRequirement("ua"));
            //policy.AddRequirements(new LanguageDenyRequirement("ww"));
        }

        return services;
    }

    /// <summary>
    /// Добавляет и настраивает аутентификацию и авторизацию.
    /// </summary>
    public static IServiceCollection AddCustomAuthenticationAndAuthorization(this IServiceCollection services)
    {
        services.AddCustomAuthentication();
        services.AddCustomAuthorization();

        return services;
    }
}