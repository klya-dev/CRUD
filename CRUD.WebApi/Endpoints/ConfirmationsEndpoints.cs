using Asp.Versioning.Builder;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки подтверждения.
/// </summary>
public static class ConfirmationsEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    /// <param name="apiVersionSet"><see cref="ApiVersionSet"/> версия API.</param>
    public static void Map(WebApplication app, ApiVersionSet apiVersionSet)
    {
        var confirmationsMap = app.MapGroup("/v{version:apiVersion}/confirmations")
            .WithApiVersionSet(apiVersionSet)
            .WithTags(EndpointTags.Confirmations, EndpointTags.AllEndpointsForClient);
        confirmationsMap.MapGet("/email/{token}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] string token, HttpContext httpContext, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Null в принципе не может прийти, т.к часть URL

            // Вызов сервиса
            var result = await userManager.ConfirmEmailAsync(token, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
            {
                // Информируем фронтенд о необходимости обновить JWT-токен
                // По сути, у меня как такового фронтенда нет, это просто GET-запрос, поэтому никто не считает этот заголовок
                // Изначально, плохо спроектировано, т.к я сам не знаю, что хочу, на данный момент у меня какой-то API с частицами фронтенда
                // В идеале, на фронте (отдельный домен) должна быть страничка, на эту страничку должна генерироваться ссылка с токеном
                // На этой страничке должна быть либо кнопка "Подтвердить", либо ничего, но в любом случае вызывается конечная точка API (PUT/POST),
                // которая в свою очередь подтверждает почту пользователя и возвращает заголовок, либо какой-нибудь редирект с параметром "refresh"
                // Фронт считывает эту информацию и идёт в конечную точку пересоздания токена (API)
                // А я всё намешал, выходит, что у меня эта конечная точка выполняет функции и backend'а (подтверждает почту), и frontend'а (пустая страничка - GET)
                httpContext.Response.Headers["X-REQUIRE-UPDATE-AUTH-TOKEN"] = "email_confirmed";
                // А Claim "premium" вообще зависит от вебхука, нужно тоже как-то информировать фронтенд о получении премиума и о необходимости обновления токена, например SignalR

                return TypedResults.NoContent();
            }

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .AllowAnonymous()
            .WithIdempotency()
            .CacheOutput(builder => builder.NoCache()) // Отключаем кэширование ответов для этого эндпоинта
            .WithSummary("Подтверждает электронную почту пользователя по предоставленному токену.")
            .WithDescription("Клиент должен обновить JWT-токен авторизации через Refresh-токен (если успешный ответ - 204).")
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        // Достаём опции VerificationPhoneNumberRequest, чтобы использовать LengthCode в шаблоне маршрута
        // Если будет использоваться IOptionsMonitor, то придётся проверять вручную в конечной точке
        using var scope = app.Services.CreateScope();
        var verificationPhoneNumberRequestOptions = scope.ServiceProvider.GetRequiredService<IOptions<VerificationPhoneNumberRequestOptions>>().Value;

        confirmationsMap.MapGet($"/phone/{{code:length({verificationPhoneNumberRequestOptions.LengthCode})}}", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromRoute] string code, HttpContext httpContext, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            if (!httpContext.User.Claims.GetNameIdentifierGuid(out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await userManager.VerificatePhoneNumberAsync(userId, code, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .RequireAuthorization()
            .WithIdempotency()
            .CacheOutput(builder => builder.NoCache())
            .WithSummary("Подтверждает телефонный номер пользователя по предоставленному коду.")
            .WithDescription("Клиент должен обновить JWT-токен авторизации через Refresh-токен (если успешный ответ - 204).")
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        confirmationsMap.MapGet("/password/{token}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] string token, IPasswordChanger passwordChanger, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Вызов сервиса
            var result = await passwordChanger.ChangePasswordAsync(token, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.NoContent();

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .AllowAnonymous()
            .WithIdempotency()
            .CacheOutput(builder => builder.NoCache())
            .WithSummary("Подтверждает смену пароля пользователя по предоставленному токену.")
            .WithDescription("Подтверждение единоразовое, дополнительных подтверждений не требуется.")
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);
    }
}