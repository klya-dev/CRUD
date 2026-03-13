using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет всем эндпоинтам возможный ответ (Produce) <see cref="HttpStatusCode.TooManyRequests"/>.
/// </summary>
/// <remarks>
/// <see href="https://stackoverflow.com/questions/78539730/set-the-same-produces-response-for-all-minimal-api-endpoints"/>
/// </remarks>
public class ProduceTooManyRequestsTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Responses.Add(((int)HttpStatusCode.TooManyRequests).ToString(), new OpenApiResponse
        {
            Description = "Too Many Requests"
        });

        await Task.CompletedTask; // Подавить предупреждение компилятора CS1998, т.к async есть, а ожидания нет
    }
}