using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CRUD.Services.BackgroundServices.RevokeExpiredRefreshTokensBackground;

/// <summary>
/// Сервис для отзыва/удаления истёкших Refresh-токенов в фоне.
/// </summary>
public class RevokeExpiredRefreshTokensBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RevokeExpiredRefreshTokensBackgroundServiceOptions _options;
    private readonly ILogger<RevokeExpiredRefreshTokensBackgroundService> _logger;

    public RevokeExpiredRefreshTokensBackgroundService(IServiceProvider serviceProvider, IOptions<RevokeExpiredRefreshTokensBackgroundServiceOptions> options, ILogger<RevokeExpiredRefreshTokensBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;

        _logger.StartedBackgroundServiceLog(nameof(RevokeExpiredRefreshTokensBackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Получаем сервис
        using var scope = _serviceProvider.CreateScope();
        var revokeExpiredRefreshTokensBackgroundCore = scope.ServiceProvider.GetRequiredService<IRevokeExpiredRefreshTokensBackgroundCore>();

        await revokeExpiredRefreshTokensBackgroundCore.DoWorkAsync(ct); // При запуске приложения хочу выполнить итерацию (чтобы не ждать таймер)

        using PeriodicTimer timer = new(_options.Timer);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await revokeExpiredRefreshTokensBackgroundCore.DoWorkAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.StopedBackgroundServiceLog(nameof(RevokeExpiredRefreshTokensBackgroundService));
        }
    }
}