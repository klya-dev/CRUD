using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CRUD.Infrastructure.S3;

/// <inheritdoc cref="ISaveLogsToS3BackgroundCore"/>
public partial class SaveLogsToS3BackgroundCore : ISaveLogsToS3BackgroundCore
{
    private readonly IS3Manager _s3Manager;
    private readonly S3Options _s3Options;
    private readonly ILogger<SaveLogsToS3BackgroundCore> _logger;

    private readonly string pathLogs;

    public SaveLogsToS3BackgroundCore(IWebHostEnvironment environment, IS3Manager s3Manager, IOptions<S3Options> s3Options, ILogger<SaveLogsToS3BackgroundCore> logger)
    {
        _s3Manager = s3Manager;
        _s3Options = s3Options.Value;
        _logger = logger;

        pathLogs = Path.Combine(environment.ContentRootPath, _s3Options.LogsDirectory);
    }

    public async Task DoWorkAsync(CancellationToken ct)
    {
        // Существует ли папка /Logs
        // Учитывая, что независимо от настройки SkipLogging - SaveLogsToS3BackgroundService всегда включен, что может привезти к исключениям, если отключить логирование и папка не создастся (Serilog автоматически создаёт папку)
        if (!Directory.Exists(pathLogs))
        {
            _logger.LogWarning("Папки \"{directory}\" не существует. В ней должны находится файлы-логи.", pathLogs);
            return;
        }

        // Получаем все локальные файлы-логи
        var files = Directory.GetFiles(pathLogs);

        foreach (var path in files)
        {
            // Получаем имя файла
            var fileName = Path.GetFileName(path);

            // Проверяем подходит ли имя файла к формату
            if (!LogFileNameRegex().IsMatch(fileName))
            {
                _logger.LogWarning("Файл-лог \"{fileName}\" не подходит по формату.", fileName);
                continue;
            }

            // Проверяем несегодняшний ли это лог
            var sanitizedFileName = fileName.Substring(4, 8); // Было log-20250822.txt, стало 20250822
            var sanitizedFileDate = sanitizedFileName.Insert(4, ".").Insert(7, "."); // Было 20250822, стало 2025.08.22
            if (DateTime.TryParseExact(sanitizedFileDate, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
            {
                // Если сегодняшний или волшебным образом будущий файл-лог
                if (fileDate.Date >= DateTime.Now.Date)
                    continue;
            }

            // Создаём файл в облачном хранилище
            await using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var key = _s3Options.LogsInS3Directory + "/" + fileName; // Ключ объекта в S3
                var result = await _s3Manager.CreateObjectAsync(file, key, ct);

                // Если ошибка при загрузке в облачное хранилище
                if (result.ErrorMessage != null)
                {
                    _logger.LogError("Не удалось загрузить файл-лог \"{fileName}\" в облачное хранилище по причине: {error}.", fileName, result.ErrorMessage);
                    continue;
                }
            }

            // Удаляем локальный файл
            File.Delete(path);
        }
    }

    [GeneratedRegex(@"log-\d{4}\d{2}\d{2}\.txt$")]
    private static partial Regex LogFileNameRegex();
}