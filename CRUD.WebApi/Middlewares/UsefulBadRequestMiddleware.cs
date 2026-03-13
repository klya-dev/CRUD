namespace CRUD.WebApi.Middlewares;

/// <summary>
/// Middleware для отправки вместо "голого" <c>Bad Request</c>'а, полезный, хотя бы немного наводящий на решение, <c>Bad Request</c>.
/// </summary>
/// <remarks>
/// Для <c>Production</c> работает, если включить <c>builder.Services.Configure&lt;RouteHandlerOptions&gt;(options => options.ThrowOnBadRequest = true);</c>
/// <para><see href="https://github.com/dotnet/aspnetcore/issues/48355"/></para>
/// </remarks>
public class UsefulBadRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsefulBadRequestMiddleware> _logger;

    public UsefulBadRequestMiddleware(RequestDelegate next, ILogger<UsefulBadRequestMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex)
        {
            _logger.LogDebug(ex, "BadRequest исключение: " + ex.Message);

            var localizer = context.RequestServices.GetRequiredService<IResourceLocalizer>();

            var problem = TypedResults.Extensions.Problem(ApiErrorConstants.IncorrectRequest, localizer);
            await problem.ExecuteAsync(context);
        }
    }
}