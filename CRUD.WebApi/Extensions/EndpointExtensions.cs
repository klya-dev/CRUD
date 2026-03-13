namespace CRUD.WebApi.Extensions;

/// <summary>
/// Расширения для эндпоинтов.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Добавляет фильтр <see cref="ValidationFilter{T}"/> и метадату ответа <see cref="OpenApiRouteHandlerBuilderExtensions.ProducesValidationProblem(RouteHandlerBuilder, int, string?)"/>.
    /// </summary>
    /// <typeparam name="T">Модель, которую нужно провалидировать.</typeparam>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class
    {
        return builder.AddEndpointFilter<ValidationFilter<T>>()
            .ProducesValidationProblem();
    }

    /// <summary>
    /// Добавляет фильтр <see cref="IdempotencyFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>Ключ идемпотентности достаётся либо из заголовка <c>Idempotency-Key</c>, либо из строки запроса <c>idmkey</c> (только для <see cref="HttpMethods.Get"/> методов).</para>
    /// <para>По умолчанию длительность кэшированния ключа идемпотентности (<c>Idempotency-Key</c>) равно 120 секунд.</para>
    /// <para>Кэшируются только 200-299 статус коды.</para>
    /// <para>Желательно этот фильтр выполнять одним из первых. Например, зачем валидировать модель, если есть закэшированный ответ этого же запроса.</para>
    /// </remarks>
    /// <param name="cacheTime">Длительность кэшированния ключа идемпотентности (<c>Idempotency-Key</c>).</param>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder, TimeSpan? cacheTime = null)
    {
        return builder.AddEndpointFilter(new IdempotencyFilter(cacheTime));
    }
}