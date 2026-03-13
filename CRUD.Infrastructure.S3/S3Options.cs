namespace CRUD.Infrastructure.S3;

/// <summary>
/// Опции S3.
/// </summary>
public class S3Options
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "S3";

    /// <summary>
    /// Ключ доступа.
    /// </summary>
    public required string AccessKey { get; set; }

    /// <summary>
    /// Секретный ключ.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Имя бакета.
    /// </summary>
    public required string BucketName { get; set; }

    /// <summary>
    /// URL сервиса.
    /// </summary>
    public required string ServiceURL { get; set; }

    /// <summary>
    /// Директория для логов относительно <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.WebRootPath"/>.
    /// </summary>
    public required string LogsDirectory { get; set; }

    /// <summary>
    /// Директория для логов в S3.
    /// </summary>
    public required string LogsInS3Directory { get; set; }
}