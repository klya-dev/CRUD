using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Microservice.EmailSender.Utilities;

/// <summary>
/// Retriever позволяющий пропарсить JWKS без "/.well-known/openid-configuration".
/// </summary>
/// <remarks>
/// <para>Нужен для получения публичных ключей из "/.well-known/jwks.json" в делегате <see cref="TokenValidationParameters.IssuerSigningKeyResolver"/>.</para>
/// <para><see cref="GetConfigurationAsync(string, IDocumentRetriever, CancellationToken)"/> возвращает <see cref="JsonWebKeySet"/>.</para>
/// </remarks>
public class JwksRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public async Task<JsonWebKeySet> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        // Можно зайти в OpenIdConnectConfigurationRetriever, там почти такая же реализация

        // Получаем сырую строку JSON (JWKS)
        string doc = await retriever.GetDocumentAsync(address, cancel);

        // Десериализуем ключи
        var jwks = new JsonWebKeySet(doc);

        return jwks;
    }
}