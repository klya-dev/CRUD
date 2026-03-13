using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace CRUD.Services;

/// <inheritdoc cref="IS3Initializer"/>
public class S3Initializer : IS3Initializer
{
    // Чтобы заюзать IWebHostEnvironment тут
    // Нужно в .csproj написать <ItemGroup> <FrameworkReference Include="Microsoft.AspNetCore.App" /> </ItemGroup>

    private readonly IS3Manager _s3Manager;
    private readonly IWebHostEnvironment _environment;
    private readonly S3InitializerOptions _s3InitializerOptions;
    private readonly AvatarManagerOptions _avatarManagerOptions;
    private readonly ILogger<S3Initializer> _logger;

    public S3Initializer(IS3Manager s3Manager, IWebHostEnvironment environment, IOptions<S3InitializerOptions> s3InitializerOptions, IOptions<AvatarManagerOptions> avatarManagerOptions, ILogger<S3Initializer> logger)
    {
        _s3Manager = s3Manager;
        _environment = environment;
        _s3InitializerOptions = s3InitializerOptions.Value;
        _avatarManagerOptions = avatarManagerOptions.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // S3 сам создаст папку по пути дефотной аватарки
        //await _s3Manager.CreateObjectAsync(_avatarManagerOptions.AvatarsInS3Directory); // Создаём папку avatars

        // Для инициализации S3 экосистемы приложения, ОБЯЗАТЕЛЬНО должен существовать архив, из которого всё копируется в рабочие папки
        // Архив нужен, чтобы не держать необходимые файлы в самом приложении, и чтобы провести инициализацию

        // Существует ли архив. Чтобы оттуда всё копировать
        if (!await _s3Manager.IsObjectExistsAsync(_s3InitializerOptions.ArchiveInS3Directory, ct))
        {
            _logger.LogError("Инициализация рабочих папок и файлов была пропущена, т.к архива по ключу \"{key}\" несуществует.", _s3InitializerOptions.ArchiveInS3Directory);
            return;
        }
        
        // Копируем из архива дефолтную аватарку в папку для работы с аватарками (archive/default.png to avatars/default.png)
        // Существует ли дефолтная аватарка в архиве
        var archiveDefaultAvatarKey = _s3InitializerOptions.ArchiveInS3Directory + "/default.png";
        if (!await _s3Manager.IsObjectExistsAsync(archiveDefaultAvatarKey, ct))
        {
            _logger.LogError("Объект \"{key}\" не найден. Инициализация остановлена.", archiveDefaultAvatarKey);
            return;
        }

        // Копируем объект из архива
        var copyResult = await _s3Manager.CopyObjectAsync(archiveDefaultAvatarKey, _avatarManagerOptions.DefaultAvatarPath, ct);
        if (copyResult.ErrorMessage != null)
        {
            _logger.LogError("Не удалось скопировать объект \"{sourceKey}\" в \"{destinationKey}\" по причине: \"{error}\". Инициализация остановлена.", archiveDefaultAvatarKey, _avatarManagerOptions.DefaultAvatarPath, copyResult.ErrorMessage);
            return;
        }
    }
}