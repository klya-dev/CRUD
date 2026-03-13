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
        confirmationsMap.MapGet("/email/{token}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] string token, IUserManager userManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Null в принципе не может прийти, т.к часть URL

            try
            {
                // Вызов сервиса
                var result = await userManager.ConfirmEmailAsync(token, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый подтвердил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .AllowAnonymous()
            .WithIdempotency()
            .CacheOutput(builder => builder.NoCache()) // Отключаем кэширование ответов для этого эндпоинта
            .WithSummary("Подтверждает электронную почту пользователя по предоставленному токену.")
            .WithDescription("Подтверждение единоразовое, дополнительных подтверждений не требуется.")
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
            var claimUserId = httpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (claimUserId == null || !Guid.TryParse(claimUserId.Value, out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await userManager.VerificatePhoneNumberAsync(userId, code, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый подтвердил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .RequireAuthorization()
            .WithIdempotency()
            .CacheOutput(builder => builder.NoCache())
            .WithSummary("Подтверждает телефонный номер пользователя по предоставленному коду.")
            .WithDescription("Подтверждение единоразовое, дополнительных подтверждений не требуется.")
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);

        confirmationsMap.MapGet("/password/{token}", async Task<Results<ProblemHttpResult, NoContent>> ([FromRoute] string token, IPasswordChanger passwordChanger, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await passwordChanger.ChangePasswordAsync(token, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
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