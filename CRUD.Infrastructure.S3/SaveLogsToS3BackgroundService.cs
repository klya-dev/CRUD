using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRUD.Infrastructure.S3;

/// <summary>
/// Сервис для загрузки логов в облачное хранилище в фоне.
/// </summary>
public class SaveLogsToS3BackgroundService : BackgroundService
{
    private readonly ISaveLogsToS3BackgroundCore _saveLogsToS3BackgroundCore;
    private readonly SaveLogsToS3BackgroundServiceOptions _options;
    private readonly ILogger<SaveLogsToS3BackgroundService> _logger;

    public SaveLogsToS3BackgroundService(ISaveLogsToS3BackgroundCore saveLogsToS3BackgroundCore, IOptions<SaveLogsToS3BackgroundServiceOptions> options, ILogger<SaveLogsToS3BackgroundService> logger)
    {
        _saveLogsToS3BackgroundCore = saveLogsToS3BackgroundCore;
        _options = options.Value;
        _logger = logger;

        _logger.StartedBackgroundServiceLog(nameof(SaveLogsToS3BackgroundService));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _saveLogsToS3BackgroundCore.DoWorkAsync(ct); // При запуске приложения хочу выполнить итерацию (чтобы не ждать таймер)

        using PeriodicTimer timer = new(_options.Timer);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await _saveLogsToS3BackgroundCore.DoWorkAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.StopedBackgroundServiceLog(nameof(SaveLogsToS3BackgroundService));
        }
    }
}