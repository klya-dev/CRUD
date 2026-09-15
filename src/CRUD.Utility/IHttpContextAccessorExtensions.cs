using Microsoft.AspNetCore.Http;

namespace CRUD.Utility;

/// <summary>
/// Расширения для <see cref="IHttpContextAccessor"/>.
/// </summary>
public static class IHttpContextAccessorExtensions
{
    /// <summary>
    /// Возвращает базовый Url запроса. Например, "https://localhost:1234".
    /// </summary>
    /// <returns>Базовый Url запроса.</returns>
    public static string GetBaseUrl(this IHttpContextAccessor httpContextAccessor)
    {
        var request = httpContextAccessor.HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host.Value}";

        return baseUrl;
    }
}