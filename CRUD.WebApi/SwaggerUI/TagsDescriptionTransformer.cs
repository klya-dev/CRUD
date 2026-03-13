using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет описание к тегам.
/// </summary>
public class TagsDescriptionTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Найти все теги можно в файле "/openapi/v1.json" в самом низу

        document.Tags.First(x => x.Name == EndpointTags.Admin).Description = "Админ-панель.";
        document.Tags.First(x => x.Name == EndpointTags.Users).Description = "Пользователи (public).";
        document.Tags.First(x => x.Name == EndpointTags.AllEndpointsForClient).Description = "Все конечные точки для клиента.";
        document.Tags.First(x => x.Name == EndpointTags.User).Description = "Авторизированный (текущий) пользователь.";
        document.Tags.First(x => x.Name == EndpointTags.Confirmations).Description = "Подтверждения.";
        document.Tags.First(x => x.Name == EndpointTags.Publications).Description = "Публикации.";
        document.Tags.First(x => x.Name == EndpointTags.ClientApi).Description = "Клиентский API.";
        document.Tags.First(x => x.Name == EndpointTags.Auth).Description = "Авторизация/регистрация, генерация токенов.";
        document.Tags.First(x => x.Name == EndpointTags.WebHooks).Description = "Вебхуки.";
        document.Tags.First(x => x.Name == EndpointTags.WellKnown).Description = "Общеизвестные конечные точки.";
        document.Tags.First(x => x.Name == EndpointTags.AllEndpointsForBusiness).Description = "Все конечные точки для бизнеса.";

        await Task.CompletedTask;
    }
}