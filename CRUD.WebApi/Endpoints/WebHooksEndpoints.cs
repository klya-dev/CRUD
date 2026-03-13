namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки WebHook'ов.
/// </summary>
public static class WebHooksEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    public static void Map(WebApplication app)
    {
        var webHooksMap = app.MapGroup("/webhooks")
            .WithTags(EndpointTags.WebHooks, EndpointTags.AllEndpointsForBusiness);
        // По документации нужно отправить Ok (200 статус), NoContent тут не подойдёт
        webHooksMap.MapPost("/payment", async Task<Results<ProblemHttpResult, Ok>> ([FromBody] PaymentWebHook paymentWebHook, IOrderUpdater orderUpdater, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await orderUpdater.UpdateOrderInfoAsync(paymentWebHook, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.Ok();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый обновил заказ - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .AllowAnonymous()
            .AddEndpointFilter<PaymentWebHookIpCheckEndpointFilter>()
            .WithSummary("Вебхук оплаты.")
            .WithDescription("Допустимые события: payment.succeeded.")
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Conflict);
    }
}