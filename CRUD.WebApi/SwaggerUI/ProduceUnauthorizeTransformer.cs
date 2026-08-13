using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет всем конечным точкам с метадатой <see cref="IAuthorizeData"/> (RequireAuthorization) возможный ответ (Produce) <see cref="HttpStatusCode.Unauthorized"/>.
/// </summary>
/// <remarks>
/// <para><see cref="AuthorizationEndpointConventionBuilderExtensions.RequireAuthorization{TBuilder}(TBuilder)"/> не добавляет метаданные для Swagger UI.</para>
/// <see href="https://stackoverflow.com/questions/78539730/set-the-same-produces-response-for-all-minimal-api-endpoints"/>
/// </remarks>
public sealed class ProduceUnauthorizeTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses ??= [];

        // Поиск IAuthorizeData метадаты
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = metadata.Any(m => m is IAuthorizeData);

        // Если авторизация требуется, и ответа 401 еще нет в схеме
        if (hasAuthorize && !operation.Responses.ContainsKey(StatusCodes.Status401Unauthorized.ToString()))
        {
            operation.Responses.Add(((int)HttpStatusCode.Unauthorized).ToString(), new OpenApiResponse
            {
                Description = "Unauthorize"
            });
        }

        return Task.CompletedTask;
    }
}