using Microsoft.Extensions.Caching.Hybrid;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки авторизации/регистрации.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    public static void Map(WebApplication app)
    {
        var authMap = app.MapGroup("/")
            .AllowAnonymous()
            .WithTags(EndpointTags.Auth, EndpointTags.AllEndpointsForClient);
        authMap.MapPost("/login", async Task<Results<ProblemHttpResult, JsonHttpResult<AuthJwtResponse>>> ([FromBody] LoginDataDto loginData, IAuthManager authManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Вызов сервиса
            var result = await authManager.LoginAsync(loginData, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithValidation<LoginDataDto>()
            .WithSummary("Выполняет процесс аутентификации пользователя по предоставленным данным.")
            .WithDescription("При успешной аутентификации генерируется JWT-токен для дальнейшего использования.")
            .Produces<AuthJwtResponse>((int)HttpStatusCode.OK); // Swagger автоматически не может сгенерировать такой исход в документации

        authMap.MapPost("/refresh-login", async Task<Results<ProblemHttpResult, JsonHttpResult<AuthJwtResponse>>> ([FromBody] string refreshToken, IAuthManager authManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Вызов сервиса
            var result = await authManager.LoginAsync(refreshToken, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Если ошибка "InvalidToken", то меняем статус код на Unauthorized (а не как в константе)
            if (result.ErrorMessage == ErrorMessages.InvalidToken)
                return TypedResults.Extensions.Problem(ApiErrorConstants.InvalidToken.ChangeStatus(HttpStatusCode.Unauthorized), localizer);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Выполняет процесс аутентификации пользователя по предоставленному Refresh-токену.")
            .WithDescription("При успешной аутентификации генерируется JWT-токен для дальнейшего использования.")
            .Produces<AuthJwtResponse>((int)HttpStatusCode.OK);

        authMap.MapPost("/register", async Task<Results<ProblemHttpResult, JsonHttpResult<AuthJwtResponse>>> ([FromBody] CreateUserDto createUserDto, IAuthManager authManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await authManager.RegisterAsync(createUserDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.Json(result.Value); // JWT-токен

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый создал - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<CreateUserDto>()
            .WithSummary("Выполняет процесс регистрации пользователя по предоставленным данным.")
            .WithDescription("При успешной регистрации генерируется JWT-токен для дальнейшего использования.")
            .Produces<AuthJwtResponse>((int)HttpStatusCode.OK);

        authMap.MapGet("/oauth/link", async Task<Results<StatusCodeHttpResult, Ok<string>>> (IOAuthMailRuProvider oAuthMailRuProvider, CancellationToken ct) =>
        {
            // Вызов сервиса
            var link = await oAuthMailRuProvider.GetAuthorizationLinkAsync(ct);

            // Не удалось получить ссылку
            if (link == null)
                return TypedResults.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            return TypedResults.Ok(link);
        })
            .WithSummary("Возвращает ссылку для входа пользователя в аккаунт MailRu.")
            .WithDescription("Нужно перейти по ссылке и войти в аккаунт, а после по предоставленному коду и строке состояния, попытаться авторизоваться в приложении, если не удалось, повторить попытку, но в регистрационной конечной точке.");

        authMap.MapPost("/oauth/login", async Task<Results<StatusCodeHttpResult, ProblemHttpResult, JsonHttpResult<AuthJwtResponse>>> ([FromQuery] string code, [FromQuery] string state, IOAuthMailRuProvider oAuthMailRuProvider, IAuthManager authManager, HybridCache cache, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Кэшируем AccessToken MailRu
            // Кэшируем, потому что code одноразовый, и второй раз по нему не получится получить AccessToken MailRu
            var options = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(3600),
                LocalCacheExpiration = TimeSpan.FromSeconds(3600)
            };

            // Получаем AccessToken MailRu по коду и строке состояния
            // Достаём из кэша или кэшируем
            var accessToken = await cache.GetOrCreateAsync(
                $"{CacheKeys.OAuthAccessTokenMailRu}-{state}",
                async ct => await oAuthMailRuProvider.GetAccessTokenAsync(code, state, ct),
                options, cancellationToken: ct);

            // Не удалось получить AccessToken
            if (accessToken == null)
                return TypedResults.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            // Получаем данные пользователя по AccessToken'у
            var userInfo = await oAuthMailRuProvider.GetUserInfoAsync(accessToken, ct);

            // Не удалось получить UserInfo
            if (userInfo == null)
                return TypedResults.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            // Авторизуем пользователя
            var result = await authManager.LoginAsync(userInfo, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value); // Возвращаем свои токены

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .WithSummary("Возвращает авторизационный JWT-токен приложения после авторизации в MailRu.")
            .WithDescription("Необходим авторизационный код, строка состояния.");

        authMap.MapPost("/oauth/registration", async Task<Results<StatusCodeHttpResult, ProblemHttpResult, JsonHttpResult <AuthJwtResponse>>> ([FromQuery] string code, [FromQuery] string state, [FromBody] OAuthCompleteRegistrationDto oAuthCompleteRegistrationDto, IOAuthMailRuProvider oAuthMailRuProvider, IAuthManager authManager, HybridCache cache, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Кэшируем AccessToken MailRu
            // Кэшируем, потому что code одноразовый, и второй раз по нему не получится получить AccessToken MailRu
            var options = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(3600),
                LocalCacheExpiration = TimeSpan.FromSeconds(3600)
            };

            // Получаем AccessToken MailRu по коду и строке состояния
            // Достаём из кэша или кэшируем
            var accessToken = await cache.GetOrCreateAsync(
                $"{CacheKeys.OAuthAccessTokenMailRu}-{state}",
                async ct => await oAuthMailRuProvider.GetAccessTokenAsync(code, state, ct),
                options, cancellationToken: ct);

            // Не удалось получить AccessToken
            if (accessToken == null)
                return TypedResults.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            // Получаем данные пользователя по AccessToken'у
            var userInfo = await oAuthMailRuProvider.GetUserInfoAsync(accessToken, ct);

            // Не удалось получить UserInfo
            if (userInfo == null)
                return TypedResults.StatusCode((int)HttpStatusCode.ServiceUnavailable);

            try
            {
                // Регистрируем пользователя
                var result = await authManager.RegisterAsync(userInfo, oAuthCompleteRegistrationDto, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.Json(result.Value); // Возвращаем свои токены

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый создал - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithValidation<OAuthCompleteRegistrationDto>()
            .WithSummary("Завершение регистрации после авторизации в MailRu.")
            .WithDescription("Необходим авторизационный код, строка состояния, и заполненная форма завершения регистрации.");
    }
}