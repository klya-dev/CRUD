using Asp.Versioning.Builder;

namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Конечные точки публикаций.
/// </summary>
public static class PublicationsEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    /// <param name="apiVersionSet"><see cref="ApiVersionSet"/> версия API.</param>
    public static void Map(WebApplication app, ApiVersionSet apiVersionSet)
    {
        var publicationsMap = app.MapGroup("/v{version:apiVersion}/publications")
            .WithApiVersionSet(apiVersionSet)
            .RequireAuthorization()
            .WithTags(EndpointTags.Publications, EndpointTags.AllEndpointsForClient);
        publicationsMap.MapGet("/", async Task<Results<ValidationProblem, JsonHttpResult<IEnumerable<PublicationDto>>>> ([FromQuery] int count, IPublicationManager publicationManager, IValidator<GetPublicationsDto> validator, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Сейчас у меня спецификация OAS 3.0.1, можно посмотреть в "/openapi/v1.json"
            // OAS соблюдает стандарты RFC, а там сказано, что GET запросы не могут иметь тела запроса, т.е класс GetPublicationsDto | https://httpwg.org/specs/rfc7231.html#GET
            // Поэтому, я использую поле Count в строке запроса. В RFC, кстати, верно сказано, что можно и нужно кэшировать ответы GET запросов
            // А GetPublicationsDto, я использую для валидации, чтобы не городить свои ошибки, когда можно сразу воспользоваться Fluent Validation
            // С другой стороны в методе DELETE у "/users", я использую класс DeleteUserDto, так в версии OAS 3.0.0 нельзя было делать, сейчас разрешили

            // Кажется тупость, создавать класс, чтобы отвалидировать один int, но если вспомнить про локализацию и правила, их всё равно, где-то нужно писать
            // Можно не создавать класс валидатора, а воспользоваться InlineValidator<int>, но мы столкнёмся с теми же проблемами
            // Можно создать свою ошибку, написать её в ресурсах, через аргументы вставлять в неё значения, но, одно большое но, придёться создавать уже свой валидатор, без Fluent Validation, т.к нужно же мои валидации, как-то вызывать, а это уже "код ради кода"
            // За валидацию отвечает Fluent Validation, так пусть и отвечает без моих велосипедов, всё уже написано
            var getPublicationsDto = new GetPublicationsDto()
            {
                Count = count
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getPublicationsDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationsDtoAsync(count, ct);

            return TypedResults.Json(result);
        })
            .AllowAnonymous()
            //.CacheOutput() - необязательно писать, т.к дефолтная политика кэша автоматически определит
            .WithSummary("Возвращает указанное количество публикаций.")
            .WithDescription("Возвратимые данные каждой публикации: Дата, Заголовок, Содержимое, Id автора (пользователя), Имя автора.")
            .Produces<IEnumerable<PublicationDto>>((int)HttpStatusCode.OK);

        publicationsMap.MapGet("/paginated", async Task<Results<ValidationProblem, JsonHttpResult<PaginatedListDto<PublicationDto>>>> ([FromQuery] int? pageIndex, [FromQuery] int pageSize, [FromQuery] string? searchString, [FromQuery] string? sortBy, IPublicationManager publicationManager, IValidator<GetPaginatedListDto> validator, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            var getPaginatedListDto = new GetPaginatedListDto()
            {
                PageIndex = pageIndex ?? 1,
                PageSize = pageSize
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getPaginatedListDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationsDtoAsync(pageIndex ?? 1, pageSize, searchString, sortBy ?? SortByVariables.date, ct);

            return TypedResults.Json(result);
        })
            .AllowAnonymous()
            .WithSummary("Возвращает постраничный список публикаций.")
            .WithDescription("Возвратимые данные: Публикации, Номер страницы, Количество страниц, Есть ли предыдущая страница, Есть ли следующая страница.")
            .Produces<PaginatedListDto<PublicationDto>>((int)HttpStatusCode.OK);

        // v2
        publicationsMap.MapGet("/", ([FromQuery] int count) =>
        {
            return TypedResults.Ok("some");
        })
            .AllowAnonymous()
            .MapToApiVersion(2.0)
            .WithSummary("Просто отправляет OK.")
            .WithDescription("Да, вот так вот.");

        publicationsMap.MapGet("/authors", async Task<Results<ValidationProblem, JsonHttpResult<IEnumerable<AuthorDto>>>> ([FromQuery] int count, IPublicationManager publicationManager, IValidator<GetAuthorsDto> validator, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            var getAuthorsDto = new GetAuthorsDto()
            {
                Count = count
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getAuthorsDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetAuthorsDtoAsync(count, ct);

            return TypedResults.Json(result);
        })
            .AllowAnonymous()
            .WithSummary("Возвращает указанное количество авторов.")
            .WithDescription("Возвратимые данные каждой публикации: Имя автора, Username автора, Код языка автора, количество публикаций этого автора.")
            .Produces<IEnumerable<PublicationDto>>((int)HttpStatusCode.OK);

        publicationsMap.MapGet("/authors/{authorId:guid}", async Task<Results<ValidationProblem, ProblemHttpResult, JsonHttpResult<IEnumerable<PublicationDto>>>> ([FromRoute] Guid authorId, [FromQuery] int count, IPublicationManager publicationManager, IValidator<GetPublicationsDto> validator, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (authorId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            var getPublicationsDto = new GetPublicationsDto()
            {
                Count = count
            };

            // Валидация модели
            var validationResult = await validator.ValidateAsync(getPublicationsDto, ct);
            if (!validationResult.IsValid)
                return TypedResults.Extensions.ValidationProblem(validationResult, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationsDtoAsync(count, authorId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);

        })
            .AllowAnonymous()
            .CacheOutput("Expire20")
            .WithSummary("Возвращает указанное количество публикаций указанного автора.")
            .WithDescription("Возвратимые данные: [Имя, Username, Код языка].")
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<IEnumerable<PublicationDto>>((int)HttpStatusCode.OK);

        publicationsMap.MapGet("/{publicationId:guid}", async Task<Results<ProblemHttpResult, JsonHttpResult<PublicationDto>>> ([FromRoute] Guid publicationId, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Пустой GUID
            if (publicationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            // Вызов сервиса
            var result = await publicationManager.GetPublicationDtoAsync(publicationId, ct);

            // Нет ошибки
            if (result.ErrorMessage == null)
                return TypedResults.Json(result.Value);

            // Сопоставление ошибки
            return TypedResults.Extensions.Problem(result, localizer);
        })
            .AllowAnonymous()
            //.CacheOutput() - необязательно писать
            .WithName(EndpointNames.PublicationsGetById)
            .WithSummary("Возвращает минимальные данные об указанной публикации.")
            .WithDescription("Возвратимые данные: Id, Дата, Заголовок, Содержимое, Id автора (пользователя), Имя автора.")
            .Produces((int)HttpStatusCode.NotFound)
            .Produces<PublicationDto>((int)HttpStatusCode.OK);

        publicationsMap.MapPatch("/", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromBody] UpdatePublicationDto updatePublicationDto, HttpContext httpContext, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
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
                var result = await publicationManager.UpdatePublicationAsync(userId, updatePublicationDto, ct);

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
            .WithValidation<UpdatePublicationDto>()
            .WithSummary("Частично или полностью обновляет данные публикации по указанной модели.")
            .WithDescription("Обновляемые данные: Заголовок, Содержимое.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);

        publicationsMap.MapPost("/", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, CreatedAtRoute<PublicationDto>>> ([FromBody] CreatePublicationDto createPublicationDto, HttpContext httpContext, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
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
                var result = await publicationManager.CreatePublicationAsync(userId, createPublicationDto, ct);

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
            .WithValidation<CreatePublicationDto>()
            .WithSummary("Создаёт публикацию по указанной модели.")
            .WithDescription("Задаваемые данные: Заголовок, Содержимое.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);

        publicationsMap.MapDelete("/{publicationId:guid}", async Task<Results<UnauthorizedHttpResult, ProblemHttpResult, NoContent>> ([FromRoute] Guid publicationId, HttpContext httpContext, IPublicationManager publicationManager, IResourceLocalizer localizer, CancellationToken ct) =>
        {
            // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
            var claimUserId = httpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (claimUserId == null || !Guid.TryParse(claimUserId.Value, out Guid userId))
                return TypedResults.Unauthorized();

            // Пустой GUID
            if (userId == Guid.Empty || publicationId == Guid.Empty)
                return TypedResults.Extensions.Problem(ApiErrorConstants.EmptyUniqueIdentifier, localizer);

            try
            {
                // Вызов сервиса
                var result = await publicationManager.DeletePublicationAsync(userId, publicationId, ct);

                // Нет ошибки
                if (result.ErrorMessage == null)
                    return TypedResults.NoContent();

                // Сопоставление ошибки
                return TypedResults.Extensions.Problem(result, localizer);
            }
            catch (DbUpdateException ex)
            {
                // Кто первый удалил - тот и удалил в базе. Второму сообщение о конфликте и предложение попробовать позже
                if (DbExceptionHelper.IsConcurrencyConflict(ex))
                    return TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, localizer);

                throw;
            }
        })
            .WithIdempotency()
            .WithSummary("Удаляет указанную публикацию из базы данных.")
            .WithDescription("Удаление безвозвратно.")
            .Produces((int)HttpStatusCode.Unauthorized)
            .Produces((int)HttpStatusCode.NotFound)
            .Produces((int)HttpStatusCode.Forbidden)
            .Produces((int)HttpStatusCode.Conflict);
    }
}