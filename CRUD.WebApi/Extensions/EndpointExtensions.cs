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

    /// <summary>
    /// Добавляет фильтр <see cref="CheckSafeListIpEndpointFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>Сверяет IP-адрес с указанным белым списком.</para>
    /// <para>Если IP-адреса нет в белом списке, то возвращается <see cref="StatusCodes.Status403Forbidden"/>.</para>
    /// </remarks>
    /// <param name="safeListIp">Белый список с IP-адресами / диапазонами.</param>
    public static RouteHandlerBuilder WithSafeListIp(this RouteHandlerBuilder builder, IEnumerable<string> safeListIp)
    {
        return builder.AddEndpointFilterFactory((routeHandlerContext, next) =>
        {
            // Получаем DI контейнер
            var services = routeHandlerContext.ApplicationServices;

            // ActivatorUtilities сам подтянет ILogger из DI
            var filter = ActivatorUtilities.CreateInstance<CheckSafeListIpEndpointFilter>(
                services,
                [safeListIp] // Передаём оставшиеся аргументы через object[]
            );

            // Можно явно
            //var filter = new PaymentWebHookIpCheckEndpointFilter(
            //    services.GetRequiredService<ILogger<PaymentWebHookIpCheckEndpointFilter>>(),
            //    cidrs
            //);

            // Возвращаем контекст для выполнения фильтра
            return async (invocationContext) => await filter.InvokeAsync(invocationContext, next);
        });
    }

    /// <summary>
    /// Добавляет фильтр <see cref="CheckSafeListIpEndpointFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>Сверяет IP-адрес с указанной строкой, состоящей из безопасных IP-адресов / диапазонов.</para>
    /// <para>Если IP-адреса нет в белом списке, то возвращается <see cref="StatusCodes.Status403Forbidden"/>.</para>
    /// </remarks>
    /// <param name="safeListIpString">Строка из безопасных IP-адресов / диапазонов.</param>
    /// <param name="separator">Разделитель строки (IP-адресов / диапазонов).</param>
    public static RouteHandlerBuilder WithSafeListIp(this RouteHandlerBuilder builder, string safeListIpString, char separator)
    {
        // Получаем список IP-адресов из строки
        var safeListIp = safeListIpString.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return builder.WithSafeListIp(safeListIp);
    }
}