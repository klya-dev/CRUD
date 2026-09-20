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
        // Выполнение не как в Middleware, т.к у обработчиков нет вызова next, есть только true - обработал, false - не обработал (переход к следующему обработчику)
        // Т.е нет обратных вызовов, если первый ExceptionHandler не смог обработать, то к нему никто больше не вернётся и надежда на следующие обработчики
        // Последний обработчик, как раз, перехватывает все необработанные исключения

        // Если бы поведение было как в Middleware, то GlobalExceptionHandler стоял бы первый - т.к он оборачивал бы все следующие Middleware'ы (next) в try catch,
        // и благодаря обратному вызову все необработанные исключения возвращались к бы нему
        // А ExceptionHandler'ы, считай, самостоятельные, идут по порядку, как зарегистрировали, без обратных вызовов

        // Порядок регистраций обработчиков имеет значение, 1 - BadRequestExceptionHandler (он и будет обрабатывать первый), 2 - ConcurrencyConflictExceptionHandler, 3 - GlobalExceptionHandler
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