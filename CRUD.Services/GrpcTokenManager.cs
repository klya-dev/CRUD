using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace CRUD.Services;

/// <inheritdoc cref="IGrpcTokenManager"/>
public class GrpcTokenManager : IGrpcTokenManager
{
    private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();
    private string? _grpcToken;

    private readonly IOptionsMonitor<AuthOptions> _authOptions;
    private readonly IOptionsMonitor<AuthEmailSenderOptions> _authEmailSenderOptions;

    public GrpcTokenManager(IOptionsMonitor<AuthOptions> authOptions, IOptionsMonitor<AuthEmailSenderOptions> authEmailSenderOptions)
    {
        _authOptions = authOptions;
        _authEmailSenderOptions = authEmailSenderOptions;
    }

    public string GenerateAuthEmailSenderToken()
    {
        // Т.к этот метод вызывается для каждого вызова gRPC, то если токен уже вписан, то перегенерировать не нужно
        // За один HTTP запрос, может быть несколько вызовов gRPC и у них будет использоваться один и тот же токен за счёт Scoped (в моём случае, вряд ли несколько вызовов за один HTTP запрос)
        if (_grpcToken != null)
            return _grpcToken;

        // Создаем JWT-токен
        var jwt = new JwtSecurityToken(
                issuer: _authOptions.CurrentValue.Issuer,
                audience: _authEmailSenderOptions.CurrentValue.Audience,
                expires: DateTime.UtcNow.Add(_authEmailSenderOptions.CurrentValue.Expires),
                signingCredentials: new SigningCredentials(_authOptions.CurrentValue.GetPrivateKey(), SecurityAlgorithms.RsaSha256));
        jwt.Header["kid"] = _authOptions.CurrentValue.KeyId; // Вписываем KeyId в kid (заголовок токена), чтобы микросервис смог по этому заголовку сверить публичный ключ
        // Т.е ".well-known/jwks.json" возвращает список публичных ключей, и у каждого ключа есть свой kid (идентификатор), и в каждый генерируемый токен должен вписываться kid, чтобы понять к какому публичному ключу он относится, чтобы провалидировать
        // В моём случае публичный ключ всегда один, поэтому kid всегда будет один и тот же, я решил не добавлять старые и текущие ключи, дабы не усложнять
        // Если бы у меня была такая логика, то был бы один старый публичный ключ, один текущий, и у каждого был бы свой kid
        // В микросервисе я реализовал плавный переход на текущий (актуальный) публичный ключ (если нет совпадений по kid, обновляем сведения) (т.е микросервис готов к старым/новым ключам, и даже, если он один (просто замена текущего ключа))
        // Но сам в самом ".well-known/jwks.json", я отдаю только один, текущий ключ, чтобы не усложнять, а так, я бы отдавал старый и текущий
        _grpcToken = _tokenHandler.WriteToken(jwt);

        return _grpcToken;
    }
}