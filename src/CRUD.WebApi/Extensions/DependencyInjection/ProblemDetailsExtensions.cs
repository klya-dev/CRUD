using System.Text.Json.Serialization;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения ProblemDetails.
/// </summary>
public static class ProblemDetailsExtensions
{
    // Можно разделить на два файла при масштабировании
    // ProblemDetailsExtensions и ConvertersExtensions

    /// <summary>
    /// Добавляет и настраивает ProblemDetails и форматирование.
    /// </summary>
    public static IServiceCollection AddProblemDetailsAndConverters(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            };
        });

        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new DateTimeConverter()); // Изменить формат записи даты
            options.SerializerOptions.Converters.Add(new TrimStringConverter()); // Обрезать все лишние пробелы в начале и конце строки
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Парсить enum, как строку, а не int (при привязке параметров)
        });

        return services;
    }
}