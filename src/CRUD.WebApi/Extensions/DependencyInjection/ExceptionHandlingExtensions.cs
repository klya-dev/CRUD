namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения обработчиков ошибок.
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Добавляет и настраивает обработчики ошибок.
    /// </summary>
    public static IServiceCollection AddCustomExceptionHandlers(this IServiceCollection services, IHostEnvironment environment)
    {
        // Порядок регистраций обработчиков имеет значение, 1 - BadRequestExceptionHandler (он и будет обрабатывать первый), 2 - ConcurrencyConflictExceptionHandler, 3 - GlobalExceptionHandler (порядок не как в Middleware)
        services.AddExceptionHandler<BadRequestExceptionHandler>(); // Обработка BadRequest исключений
        services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>(); // Обработка конфликтов параллельности
        // Глобальный обработчик ошибок только в Production, т.к он скрывает трейс
        // P.S: К сожалению, UseDeveloperExceptionPage и UseExceptionHandler не работают в связке, поэтому в Dev не будет отрабатывать UseDeveloperExceptionPage. В Prod всё норм, все обработчики
        if (environment.IsProduction())
            services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true); // Выбрасывать исключение BadRequest в Production +у меня есть обработчик этих исключений // https://github.com/dotnet/aspnetcore/issues/48355

        return services;
    }
}