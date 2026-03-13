using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CRUD.WebApi.SwaggerUI;

/// <summary>
/// Добавляет поле <c>Accept-Language</c> для заголовока запроса в Swagger UI.
/// </summary>
public class AcceptLanguageHeaderParameterTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "Код языка (ru, en)."
        });

        await Task.CompletedTask; // Подавить предупреждение компилятора CS1998, т.к async есть, а ожидания нет
    }
}