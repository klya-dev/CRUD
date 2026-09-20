using System.Threading.RateLimiting;

namespace CRUD.WebApi.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> расширения RateLimiter.
/// </summary>
public static class RateLimiterExtensions
{
    /// <summary>
    /// Добавляет и настраивает RateLimiter.
    /// </summary>
    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        RateLimiterOptions rateLimiterOptions = configuration.GetSection(RateLimiterOptions.SectionName).Get<RateLimiterOptions>()!;

        services.AddRateLimiter(options =>
        {
            // Глобальный лимитер
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userRole = httpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

                // Если пользователь админ, то не ограничиваем доступ
                if (userRole == UserRoles.Admin)
                    return RateLimitPartition.GetNoLimiter(userRole);

                // Лимитер для "/publications... GET"
                string path = httpContext.Request.Path.ToString();
                var apiVersion = httpContext.RequestedApiVersion;
                if (path.StartsWith($"/v{apiVersion}/publications")
                    && httpContext.Request.Method == "GET")
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"{httpContext.Connection.RemoteIpAddress}-public",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimiterOptions.PublicationsGet.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimiterOptions.PublicationsGet.Window),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = rateLimiterOptions.PublicationsGet.QueueLimit
                        });

                // Лимитер для "/metrics..."
                if (path.StartsWith("/metrics"))
                    return RateLimitPartition.GetNoLimiter(path);

                // Основной лимитер
                return RateLimitPartition.GetFixedWindowLimiter(
                   partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                   factory: partition => new FixedWindowRateLimiterOptions
                   {
                       PermitLimit = rateLimiterOptions.Global.PermitLimit,
                       QueueLimit = rateLimiterOptions.Global.QueueLimit,
                       Window = TimeSpan.FromSeconds(rateLimiterOptions.Global.Window)
                   });
            });

            // При достижении лимита
            options.OnRejected = async (context, ct) =>
            {
                var localizer = context.HttpContext.RequestServices.GetRequiredService<IResourceLocalizer>();

                var problem = TypedResults.Extensions.Problem(ApiErrorConstants.RateLimitExceeded, localizer);

                // Устанавливаем заголовок RetryAfter
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

                await problem.ExecuteAsync(context.HttpContext);
            };
        });

        return services;
    }
}