using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CRUD.Services;

/// <inheritdoc cref="ITokenManager"/>
public class TokenManager : ITokenManager
{
    private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

    private readonly IOptionsMonitor<AuthOptions> _authOptions; // Обязательно IOptionsMonitor, а не просто класс опции
    private readonly IOptionsMonitor<AuthWebApiOptions> _authWebApiOptions;

    public TokenManager(IOptionsMonitor<AuthOptions> authOptions, IOptionsMonitor<AuthWebApiOptions> authWebApiOptions)
    {
        // TokenManager - Singleton, а значит, если прописать "_authOptions = authOptions.CurrentValue" (если _authOptions это AuthOptions), то свежие данные так и НЕ придут
        // Такой синтаксис можно использовать в Scoped сервисах, но нахуй надо, да, данные обновятся, но лучше везде пробрасывать в IOptionsMonitor напрямую, и будет 100% везде обновлённая конфигурация
        // +далеко не факт, что Scoped сервис завтра не станет Singleton, и тогда изменения "на лету", не прокатят

        _authOptions = authOptions;
        _authWebApiOptions = authWebApiOptions;
    }

    public AuthJwtResponse GenerateAuthResponse(IEnumerable<Claim> claims, string username)
    {
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));

        // Нет ни одного элемента в коллекции
        if (!claims.Any())
            throw new InvalidOperationException("Sequence contains no elements");

        // Срок истечения токена
        var expires = DateTime.UtcNow.Add(_authWebApiOptions.CurrentValue.Expires);

        // Создаем JWT-токен
        var jwt = new JwtSecurityToken(
                issuer: _authOptions.CurrentValue.Issuer,
                audience: _authWebApiOptions.CurrentValue.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(_authOptions.CurrentValue.GetPrivateKey(), SecurityAlgorithms.RsaSha256));
        var encodedJwt = _tokenHandler.WriteToken(jwt);

        // Формируем ответ
        var response = new AuthJwtResponse
        {
            AccessToken = encodedJwt,
            Expires = expires,
            RefreshToken = GenerateRefreshToken(),
            Username = username
        };

        return response;
    }

    public string GenerateRefreshToken()
    {
        var base64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        return base64;
    }

    public string GenerateUniqueToken()
    {
        var guid = Guid.NewGuid();
        var base64 = Base64UrlEncoder.Encode(guid.ToByteArray());

        return base64;
    }

    public string GenerateCode(int length = 6)
    {
        string code = "";
        for (int i = 0; i < length; i++)
            code += Random.Shared.Next(0, 10);

        return code;
    }
}