using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CRUD.WebApi.HealthChecks;

/// <summary>
/// Проверяет консистенцию S3 на наличие всех объектов.
/// </summary>
public class S3ConsistencyHealthCheck : IHealthCheck
{
    private readonly IS3Manager _s3Manager;
    private readonly AvatarManagerOptions _avatarManagerOptions;

    public S3ConsistencyHealthCheck(IS3Manager s3Manager, IOptions<AvatarManagerOptions> avatarManagerOptions)
    {
        _s3Manager = s3Manager;
        _avatarManagerOptions = avatarManagerOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Коллекция несуществующих объектов, которые должны были существовать
        var notExistsObjects = await GetNotExistsObjectsAsync(cancellationToken);

        // В коллекции нет элементов
        if (!notExistsObjects.Any())
            return HealthCheckResult.Healthy();
        else
        {
            var objectNames = notExistsObjects.Select(key => $"'{key}'");
            var names = string.Join(", ", objectNames);

            return HealthCheckResult.Unhealthy($"Objects: {names} does not exist.");
        }
    }

    private async Task<IEnumerable<string>> GetNotExistsObjectsAsync(CancellationToken ct = default)
    {
        // Все объекты, которые должны существовать
        IEnumerable<string> keys =
        [
            _avatarManagerOptions.AvatarsInS3Directory,
            _avatarManagerOptions.DefaultAvatarPath
        ];

        var notExistsObjects = new List<string>();
        foreach (var key in keys)
        {
            bool isExists = await _s3Manager.IsObjectExistsAsync(key, ct);
            if (!isExists)
                notExistsObjects.Add(key);
        }

        return notExistsObjects;
    }
}