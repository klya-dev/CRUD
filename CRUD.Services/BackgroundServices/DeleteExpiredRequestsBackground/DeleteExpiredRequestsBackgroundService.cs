using CRUD.Services.BackgroundServices.RevokeExpiredRefreshTokensBackground;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CRUD.Services.BackgroundServices.DeleteExpiredRequestsBackground;

/// <summary>
/// Сервис для удаления истёкших запросов (<see cref="Request"/>) в фоне.
/// </summary>
public class DeleteExpiredRequestsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DeleteExpiredRequestsBackgroundServiceOptions _options;
    private readonly ILogger<DeleteExpiredRequestsBackgroundService> _logger;

    public DeleteExpiredRequestsBackgroundService(IServiceProvider serviceProvider, IOptions<DeleteExpiredRequestsBackgroundServiceOptions> options, ILogger<DeleteExpiredRequestsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;

        _logger.StartedBackgroundServiceLog(nameof(DeleteExpiredRequestsBackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Получаем сервис
        using var scope = _serviceProvider.CreateScope();
        var deleteExpiredRequestsBackgroundCore = scope.ServiceProvider.GetRequiredService<IDeleteExpiredRequestsBackgroundCore>();

        await deleteExpiredRequestsBackgroundCore.DoWorkAsync(ct); // При запуске приложения хочу выполнить итерацию (чтобы не ждать таймер)

        using PeriodicTimer timer = new(_options.Timer);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await deleteExpiredRequestsBackgroundCore.DoWorkAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.StopedBackgroundServiceLog(nameof(RevokeExpiredRefreshTokensBackgroundService));
        }
    }
}