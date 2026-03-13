using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CRUD.WebApi.Filters;

/// <summary>
/// Фильтр идемпотентности.
/// </summary>
/// <remarks>
/// <para>Ключ идемпотентности достаётся либо из заголовка <c>Idempotency-Key</c>, либо из строки запроса <c>idmkey</c> (только для <see cref="HttpMethods.Get"/> методов).</para>
/// <para>По умолчанию длительность кэшированния ключа идемпотентности (<c>Idempotency-Key</c>) равно 120 секунд.</para>
/// <para>Кэшируются только 200-299 статус коды.</para>
/// <para>Запрос будет хэшироваться и сравниваться (чтобы нельзя было переиспользовать <c>Idempotency-Key</c> с другими данными).</para>
/// <para>Желательно этот фильтр выполнять одним из первых. Например, зачем валидировать модель, если есть закэшированный ответ этого же запроса.</para>
/// <para>Полезные ссылки:</para>
/// <list type="bullet">
/// <item><seealso href="https://www.milanjovanovic.tech/blog/implementing-idempotent-rest-apis-in-aspnetcore"/>.</item>
/// <item><seealso href="https://yandex.cloud/ru/docs/api-design-guide/concepts/idempotency"/>.</item>
/// <item><seealso href="https://github.com/ikyriak/IdempotentAPI"/>.</item>
/// <item><seealso href="https://oneuptime.com/blog/post/2026-01-25-implement-idempotency-keys-dotnet/view#asp-net-core-middleware-for-idempotency"/>.</item>
/// <item><seealso href="https://www.rfc-editor.org/rfc/rfc9110#name-idempotent-methods"/>.</item>
/// </list>
/// </remarks>
public class IdempotencyFilter : IEndpointFilter
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private const string IdempotencyQueryName = "idmkey";

    private readonly TimeSpan _cacheTime;

    public IdempotencyFilter(TimeSpan? cacheTime = null)
    {
        _cacheTime = cacheTime ?? TimeSpan.FromSeconds(120);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ct = context.HttpContext.RequestAborted;

        // Включаем буфферизацию, чтобы можно было прочитать тело (весь запрос будет хэшироваться, чтобы нельзя было переиспользовать Idempotency-Key с другими данными)
        context.HttpContext.Request.EnableBuffering();

        // Достаём Idempotency-Key из заголовка или строки запроса
        Guid idempotencyKey;
        if (context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out StringValues idempotencyKeyHeader)) // Есть заголовок
        {
            // Удалось пропарсить
            if (Guid.TryParse(idempotencyKeyHeader, out Guid idempotencyKeyHeaderGuid))
                idempotencyKey = idempotencyKeyHeaderGuid;
            else
                return Results.Problem(title: $"Invalid {IdempotencyHeaderName} header.", detail: $"Invalid {IdempotencyHeaderName} header.", statusCode: StatusCodes.Status400BadRequest);
        }
        else if (context.HttpContext.Request.Query.TryGetValue(IdempotencyQueryName, out StringValues idempotencyKeyQuery)) // Есть строка запроса
        {
            // Я разрешаю использовать строку запроса только для GET запросов, т.к в html ссылку нельзя засунуть заголовок. Ссылки с Idempotency-Key используются для подтверждений (/v1/confirmations/)
            if (context.HttpContext.Request.Method != HttpMethods.Get)
                return Results.Problem(title: $"The {IdempotencyQueryName} query string can only be used in {HttpMethods.Get} methods.", detail: $"The {IdempotencyQueryName} query string can only be used in {HttpMethods.Get} methods.", statusCode: StatusCodes.Status400BadRequest);

            // Удалось пропарсить
            if (Guid.TryParse(idempotencyKeyQuery, out Guid idempotencyKeyQueryGuid))
                idempotencyKey = idempotencyKeyQueryGuid;
            else
                return Results.Problem(title: $"Invalid {IdempotencyQueryName} query string.", detail: $"Invalid {IdempotencyQueryName} query.", statusCode: StatusCodes.Status400BadRequest);
        }
        else // Нет ни заголовка, ни строки запроса
            return Results.Problem(title: $"Missing {IdempotencyHeaderName} header or {IdempotencyQueryName} query string.", detail: $"Missing {IdempotencyHeaderName} header or {IdempotencyQueryName} query.", statusCode: StatusCodes.Status400BadRequest);

        // Достаём IDistributedCache (Redis) из DI
        IDistributedCache cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        // Создаём MemoryStream и копируем в него тело запроса
        using var memoryStream = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(memoryStream, ct);

        // Создаём StringBuilder, куда мы добавляем Method, Host, Path, QueryString
        var requestDataStringBuilder = new StringBuilder();
        requestDataStringBuilder.AppendLine(context.HttpContext.Request.Method);
        requestDataStringBuilder.AppendLine(context.HttpContext.Request.Host.Value);
        requestDataStringBuilder.AppendLine(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString.Value);

        // Добавляем в StringBuilder заголовки
        foreach (var header in context.HttpContext.Request.Headers)
            requestDataStringBuilder.AppendLine($"{header.Key}:{header.Value}");

        // Добавляем к байтам StringBuilder'а байты тела запроса
        var requestDataBytes = Encoding.UTF8.GetBytes(requestDataStringBuilder.ToString()); // Байты данных из StringBuilder
        var combinedDataBytes = requestDataBytes.Concat(memoryStream.ToArray()).ToArray(); // К этим байтам добавляем байты тела запроса

        // Получаем SHA256-хэш запроса
        byte[] bodyBytes = SHA256.HashData(combinedDataBytes);

        // Получаем HEX от SHA256-хэша
        string requestHash = Convert.ToHexString(bodyBytes);

        // Получаем кэшированный результат
        string cacheKey = $"{CacheKeys.Idempotency}-{idempotencyKey}";
        string? cachedResult = await cache.GetStringAsync(cacheKey, ct);
        if (cachedResult != null)
        {
            // Десериализуем кэшированный Json
            IdempotencyCacheResult response = JsonSerializer.Deserialize<IdempotencyCacheResult>(cachedResult)!;

            // Сравниваем хэши запросов
            if (response.RequestHash != requestHash) // Этот ключ уже использовался с другими данными
                return Results.Problem(title: $"This {IdempotencyHeaderName} has already been used with another request data.", detail: $"This {IdempotencyHeaderName} has already been used with another request data.", statusCode: StatusCodes.Status400BadRequest);

            // Вписываем ответу кэшированные значения: статус, ContentType и тело
            context.HttpContext.Response.StatusCode = response.StatusCode;
            context.HttpContext.Response.ContentType = response.ContentType;
            if (response.ContentType != null) // Если ContentType = null, вероятно, это 204 статус код, и мы ничего не вписываем в тело ответа
                await context.HttpContext.Response.WriteAsJsonAsync(response.Body, ct); // Если в ответе 204 статус код, и попытаться вызвать WriteAsJsonAsync, то исключение, т.к 204 статус код не может иметь тело ответа

            // Если тела нет, то возвращаем пустой ответ с заданными выше статусом и ContentType
            return Results.Empty;
        }

        // Выполняем следующий фильтр/эндпоинт
        // Может быть Results<TResult1, TResult2...>
        object? result = await next(context);

        // Получаем только TResult
        object? actualResult = result is INestedHttpResult nested ? nested.Result : result;

        // Кэшируем только 200-299 статус коды
        if (actualResult is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 } statusCodeResult)
        {
            // Если статус код не указан - по дефолту 200
            int statusCode = statusCodeResult.StatusCode ?? StatusCodes.Status200OK;

            // Устанавливаем содержимое (тело ответа)
            ObjectResult objectResult;
            if (actualResult is IValueHttpResult valueResult) // Если у TResult есть содержимое
                objectResult = new ObjectResult(valueResult.Value); // Вписываем содержимое (тело ответа)
            else
                objectResult = new ObjectResult(null); // Нет тела ответа

            // Если содержимое есть, а ContentType не указан, ставим application/json
            // Если же содержимого нет, то оставляем ContentType = null, вероятно это 204 статус код
            string? contentType;
            if (objectResult.Value != null && context.HttpContext.Response.ContentType == null)
                contentType = "application/json; charset=utf-8";
            else
                contentType = context.HttpContext.Response.ContentType;

            // Создаём кэшируемый результат
            var idempotencyResult = new IdempotencyCacheResult()
            {
                StatusCode = statusCode,
                Body = objectResult.Value,
                RequestHash = requestHash,
                ContentType = contentType
            };

            // Сериализуем результат в Json и вписываем в кэш
            await cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(idempotencyResult),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheTime
                }, ct
            );
        }

        return result;
    }
}

/// <summary>
/// Idempotency результат. 
/// </summary>
/// <remarks>
/// Служит для кэширования результата ответа.
/// </remarks>
public class IdempotencyCacheResult
{
    /// <summary>
    /// Статус код ответа.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpResponse.StatusCode"/>.
    /// </remarks>
    public required int StatusCode { get; set; }

    /// <summary>
    /// Тело запроса.
    /// </summary>
    public required object? Body { get; set; }

    /// <summary>
    /// Хэш запроса.
    /// </summary>
    /// <remarks>
    /// Нужно для сравнивания запросов, чтобы нельзя было переиспользовать <c>Idempotency-Key</c> с другими данными.
    /// </remarks>
    public required string RequestHash { get; set; }

    /// <summary>
    /// Тип контента.
    /// </summary>
    /// <remarks>
    /// <para>Если <see cref="Body"/> не пустой, то <see cref="ContentType"/> не будет пустым.</para>
    /// <see cref="HttpResponse.ContentType"/>.
    /// </remarks>
    public required string? ContentType { get; set; }
}