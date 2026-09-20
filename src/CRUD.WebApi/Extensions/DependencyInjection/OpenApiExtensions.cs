namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения Open API.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Добавляет и настраивает OpenApi.
    /// </summary>
    public static IServiceCollection AddCustomOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); // Кнопка Authorize и применение к запросам
            options.AddOperationTransformer<AcceptLanguageHeaderParameterTransformer>(); // Поле Accept-Language
            options.AddOperationTransformer<IdempotencyKeyHeaderParameterTransformer>(); // Поле Idempotency-Key
            options.AddDocumentTransformer<InfoTransformer>(); // Информация об API, контакты
            options.AddOperationTransformer<ProduceTooManyRequestsTransformer>(); // Добавить всем конечным точкам Produce TooManyRequests
            options.AddDocumentTransformer<HealthzInfoTransformer>(); // Добавляет "/healthz" в Swagger UI
            options.AddDocumentTransformer<MetricsInfoTransformer>(); // Добавляет "/metrics" в Swagger UI
            options.AddDocumentTransformer<TagsDescriptionTransformer>(); // Добавляет описание к тегам
            options.AddOperationTransformer<ProduceUnauthorizeTransformer>(); // Добавить всем конечным точкам требующим авторизацию Produce Unauthorize

            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0; // Оставляю прошлую версию (в .NET 10 по умолчанию версия 3.1)
            // В новой версии достаточно серьёзный Breaking Change связанный с nullable типами, к примеру, если сейчас поставить новую версию, то NSwag UI даже не даст вписать count в конечную точку ниже
            // И чтобы это исправить нужно в "/v1/publications?count=1" будет указать, что count может быть null, внутри конечной точки определить, что если count = null, то возвращаем BadRequest
        });

        services.AddOpenApi("v2", options =>
        {
            // Чтобы в /openapi/v2.json была только указанная конечная точка, а остальные даже не сгенерировались, нужно:
            //options.ShouldInclude = (apiDescription) => apiDescription.HttpMethod == "GET" && apiDescription.RelativePath == "v2/publications/";
            // И плюсом удалить некоторые трансформеры, у которых есть упоминания скрытых конечных точек (будет исключение, т.к нет тега, например в TagsDescriptionTransformer)

            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); // Кнопка Authorize и применение к запросам
            options.AddOperationTransformer<AcceptLanguageHeaderParameterTransformer>(); // Поле Accept-Language
            options.AddOperationTransformer<IdempotencyKeyHeaderParameterTransformer>(); // Поле Idempotency-Key
            options.AddDocumentTransformer<InfoTransformer>(); // Информация об API, контакты
            options.AddOperationTransformer<ProduceTooManyRequestsTransformer>(); // Добавить всем конечным точкам Produce TooManyRequests
            options.AddDocumentTransformer<HealthzInfoTransformer>(); // Добавляет "/healthz" в Swagger UI
            options.AddDocumentTransformer<MetricsInfoTransformer>(); // Добавляет "/metrics" в Swagger UI
            options.AddDocumentTransformer<TagsDescriptionTransformer>(); // Добавляет описание к тегам
            options.AddOperationTransformer<ProduceUnauthorizeTransformer>(); // Добавить всем конечным точкам требующим авторизацию Produce Unauthorize

            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
        });

        return services;
    }
}