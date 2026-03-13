using Microsoft.AspNetCore.SignalR.Client;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет подключение к хабам.
/// </summary>
public class HubsConnectionHealthCheck : IHealthCheck
{
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<HubsConnectionHealthCheck> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HubsConnectionHealthCheck(ITokenManager tokenManager, ILogger<HubsConnectionHealthCheck> logger, IHttpContextAccessor httpContextAccessor)
    {
        _tokenManager = tokenManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Токен для аутентификации
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "username"),
            new Claim(ClaimTypes.Role, UserRoles.Admin),
            new Claim("language_code", "ru"),
            new Claim("premium", "true")
        ];
        var token = _tokenManager.GenerateAuthResponse(claims, "username").AccessToken;

        // Коллекция неподключённых хабов
        var notConnectedHubs = await GetNotConnectedHubsAsync(token, cancellationToken);

        if (!notConnectedHubs.Any())
            return HealthCheckResult.Healthy();
        else
        {
            var hubsNames = notConnectedHubs.Select(key => $"'{key}'");
            var names = string.Join(", ", hubsNames);

            return HealthCheckResult.Unhealthy($"Failed to connect to the hubs: {names} ");
        }
    }

    private async Task<IEnumerable<string>> GetNotConnectedHubsAsync(string accessToken, CancellationToken ct = default)
    {
        // Все хабы
        IEnumerable<string> hubs =
        [
            "notificationHub"
        ];

        var notConnectedHubs = new List<string>();
        foreach (var hub in hubs)
        {
            HubConnection? hubConnection = null;
            try
            {
                // Подключаемся к хабу
                hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{_httpContextAccessor.GetBaseUrl()}/{hub}", options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken)!; // Аутентификация
                    })
                    .AddMessagePackProtocol()
                    .Build();
                await hubConnection.StartAsync(ct);

                bool isConnected = hubConnection.State == HubConnectionState.Connected;
                if (!isConnected)
                    notConnectedHubs.Add(hub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось подключится к хабу \"{hub}\" по причине: {message}.", hub, ex.Message);
                notConnectedHubs.Add(hub);
            }
            finally
            {
                if (hubConnection != null)
                    await hubConnection.DisposeAsync();
            }
        }

        return notConnectedHubs;
    }
}