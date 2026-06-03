namespace CRUD.WebApi.Middlewares;

/// <summary>
/// Обработчик <see cref="BadHttpRequestException"/> исключений.
/// </summary>
/// <remarks>
/// <para>Если исключение это <see cref="BadHttpRequestException"/>, то возвращается <see cref="ProblemDetails"/> с <see cref="ApiErrorConstants.IncorrectRequest"/>.</para>    
/// <para>Если условие не подходит, исключение не обрабатывается - выбрасывается дальше по pipeline'у.</para>
/// <para>Для <c>Production</c> работает, если включить <c>builder.Services.Configure&lt;RouteHandlerOptions&gt;(options => options.ThrowOnBadRequest = true);</c></para>
/// <para><see href="https://github.com/dotnet/aspnetcore/issues/48355"/>.</para>
/// </remarks>
public sealed class BadRequestExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BadRequestExceptionHandler> _logger;
    private readonly IResourceLocalizer _localizer;

    public BadRequestExceptionHandler(ILogger<BadRequestExceptionHandler> logger, IResourceLocalizer localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException)
        {
            _logger.LogDebug(exception, "BadRequest исключение: " + exception.Message);

            var problemDetails = TypedResults.Extensions.Problem(ApiErrorConstants.IncorrectRequest, _localizer);
            await problemDetails.ExecuteAsync(httpContext);

            return true; // Исключение успешно обработано
        }

        return false; // Исключение не обработано
    }
}