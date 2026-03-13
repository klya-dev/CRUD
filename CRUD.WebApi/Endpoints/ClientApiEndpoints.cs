using Asp.Versioning.Builder;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки клиентского API (пользовательского).
/// </summary>
public static class ClientApiEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    /// <param name="apiVersionSet"><see cref="ApiVersionSet"/> версия API.</param>
    public static void Map(WebApplication app, ApiVersionSet apiVersionSet)
    {
        var clientApiMap = app.MapGroup("/v{version:apiVersion}/client-api")
            .WithApiVersionSet(apiVersionSet)
            .WithTags(EndpointTags.ClientApi, EndpointTags.AllEndpointsForClient);
        clientApiMap.MapPost("/publications", async Task<Results<ProblemHttpResult, CreatedAtRoute<PublicationDto>>> ([FromBody] ClientApiCreatePublicationDto clientApiCreatePublication, HttpContext httpContext, IClientApiManager clientApiManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            try
            {
                // Вызов сервиса
                var result = await clientApiManager.CreatePublicationAsync(clientApiCreatePublication, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.CreatedAtRoute(result.Value, routeName: EndpointNames.PublicationsGetById, routeValues: new { publicationId = result.Value!.Id });

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
            .WithValidation<ClientApiCreatePublicationDto>()
            .AllowAnonymous()
            .WithSummary("Создаёт публикацию по предоставленной модели, используя клиентский API-ключ.")
            .WithDescription("Задаваемые данные: Заголовок, Содержимое, Постоянный или одноразовый API-ключ.\nПодробнее можно прочитать на сайте https://localhost:7217/api.html")
            .Produces((int)HttpStatusCode.Unauthorized) // Неверный API-ключ
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);
    }
}