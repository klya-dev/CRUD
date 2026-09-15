namespace CRUD.WebApi.Endpoints;

/// <summary>
/// Общеизвестные конечные точки "/.well-known".
/// </summary>
public static class WellKnownEndpoints
{
    /// <summary>
    /// Регистрирует конечные точки.
    /// </summary>
    public static void Map(WebApplication app)
    {
        var wellKnownMap = app.MapGroup("/.well-known")
            .AllowAnonymous()
            .WithTags(EndpointTags.WellKnown, EndpointTags.AllEndpointsForBusiness);
        wellKnownMap.MapGet("/jwks.json", (IOptionsMonitor<AuthOptions> options) =>
        {
            var authOptions = options.CurrentValue;

            // Вписываем публичному ключу KeyId (kid)
            var rsaKey = authOptions.GetPublicKey();
            rsaKey.KeyId = authOptions.KeyId;

            // Конвертируем ключ в JsonWebKey
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);

            // Возвращаем ключи (в моём случае, всегда один)
            return Results.Json(new { keys = new[] { jwk } });
        })
            .CacheOutput(builder => builder.NoCache()) // Без кэширования ответа, GetPublicKey сам кэширует. Поэтому для замены ключей нужно изменить конфигурацию, а IOptionsMonitor сам подтянет изменения на лету
            .WithSummary("Возвращает все публичные ключи для валидации JWT-токенов.")
            .WithDescription("Микросервисы должны брать публичные ключи из этой конечной точки.");
    }
}