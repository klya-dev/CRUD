using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.EmailSender.Utilities;

/// <summary>
/// Донастройка <see cref="JwtBearerOptions"/>.
/// </summary>
/// <remarks>
/// <para>Полностью настраивает <see cref="JwtBearerOptions.TokenValidationParameters"/>.</para>
/// <para>Значения берутся из <see cref="IOptions{TOptions}"/>, где <see cref="TOptions"/> - <see cref="AuthOptions"/>.</para>
/// </remarks>
public class PostConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly AuthOptions _authOptions;
    private readonly ILogger<PostConfigureJwtBearerOptions> _logger;

    public PostConfigureJwtBearerOptions(IOptions<AuthOptions> authOptions, ILogger<PostConfigureJwtBearerOptions> logger)
    {
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        // Если бы у меня была конфигурация "/.well-known/openid-configuration", то там было бы поле "jwks_uri", и указав options.MetadataAddress = "https://localhost:7260/.well-known/openid-configuration", всё бы удачно спарсилось в OpenIdConnectConfiguration,
        // но приходится свой Retriever прописывать, чтобы пропарсить jwks.json
        var configurationManager = new ConfigurationManager<JsonWebKeySet>(_authOptions.JwksUrl, new JwksRetriever(), new HttpDocumentRetriever());

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Указывает, будет ли валидироваться издатель при валидации токена
            ValidIssuer = _authOptions.Issuer, // Строка, представляющая издателя
            ValidateAudience = true, // Будет ли валидироваться потребитель токена
            ValidAudience = _authOptions.Audience, // Установка потребителя токена
            ValidateLifetime = true, // Будет ли валидироваться время существования
            ValidateIssuerSigningKey = true, // Валидация ключа безопасности

            // Получаем публичные ключи из "/.well-known/jwks.json"
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                // Парсим "jwks.json", получаем неполную OpenIdConnectConfiguration с JWKS свойствами, внутри ConfigurationManager'а есть механизм кэширования
                var configuration = configurationManager.GetConfigurationAsync().GetAwaiter().GetResult();
                var keys = configuration.Keys;

                _logger.LogDebug("kid в токене: {kid}. Ключи из конфигурации: {keys}.", kid, keys);

                // Ищем ключ по kid из токена
                var matchingKeys = keys.Where(key => key.KeyId == kid).ToList();

                // Нет совпадений, вероятно, публичные ключи обновились
                if (matchingKeys.Count == 0)
                {
                    configurationManager.RequestRefresh(); // При следующем вызове GetConfigurationAsync кэш сбросится +под капотом ограничение не чаще, чем через 5 минут можно сбрасывать

                    // Получаем свежую конфигурацию, обновляем кэш
                    configuration = configurationManager.GetConfigurationAsync().GetAwaiter().GetResult();
                    keys = configuration.Keys;

                    _logger.LogDebug("kid в токене: {kid}. Ключи из обновлённой конфигурации: {keys}.", kid, keys);

                    // Ищем ключ по kid из токена
                    matchingKeys = keys.Where(key => key.KeyId == kid).ToList();

                    // Если снова нет совпадений
                    if (matchingKeys.Count == 0)
                        throw new SecurityTokenException("No matching key found");
                }

                return matchingKeys;

                // Если использовать "new HttpClient()", как многие показывают, то ключи не будут кэшироваться и при каждом запросе будет запрос в "/.well-known/jwks.json"
                // А ConfigurationManager решает эту проблему, а JwksRetriever решает проблему отсутствия "/.well-known/openid-configuration"
            }
        };
    }
}