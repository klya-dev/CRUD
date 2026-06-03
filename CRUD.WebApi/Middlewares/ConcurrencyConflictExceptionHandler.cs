namespace CRUD.WebApi.Middlewares;

/// <summary>
/// Обработчик исключений связанных с конфликтом параллельности.
/// </summary>
/// <remarks>
/// <para>Если исключение это <see cref="DbUpdateException"/> и исключение является конфликтом параллельности <see cref="DbExceptionHelper.IsConcurrencyConflict(DbUpdateException)"/>, то возвращается <see cref="ProblemDetails"/> с <see cref="ApiErrorConstants.ConcurrencyConflicts"/>.</para>    
/// <para>Если условие не подходит, исключение не обрабатывается - выбрасывается дальше по pipeline'у.</para>
/// </remarks>
public sealed class ConcurrencyConflictExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ConcurrencyConflictExceptionHandler> _logger;
    private readonly IResourceLocalizer _localizer;

    public ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger, IResourceLocalizer localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Кто первый обновил/удалил/создал - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
        if (exception is DbUpdateException dbUpdateException && DbExceptionHelper.IsConcurrencyConflict(dbUpdateException))
        {
            _logger.LogError(exception, "Произошёл конфликт параллельности: {Message}.", exception.Message);

            var problemDetails = TypedResults.Extensions.Problem(ApiErrorConstants.ConcurrencyConflicts, _localizer);
            await problemDetails.ExecuteAsync(httpContext);

            return true; // Исключение успешно обработано, и не надо его обрабатывать ещё кем-то
        }

        return false; // Исключение не обработано. Например, если исключение не DbUpdateException или не является конфликтом IsConcurrencyConflict
    }
}