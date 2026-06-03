using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет всем эндпоинтам возможный ответ (Produce) <see cref="HttpStatusCode.TooManyRequests"/>.
/// </summary>
/// <remarks>
/// <see href="https://stackoverflow.com/questions/78539730/set-the-same-produces-response-for-all-minimal-api-endpoints"/>
/// </remarks>
public sealed class ProduceTooManyRequestsTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses ??= [];

        operation.Responses.Add(((int)HttpStatusCode.TooManyRequests).ToString(), new OpenApiResponse
        {
            Description = "Too Many Requests"
        });

        return Task.CompletedTask;
    }
}