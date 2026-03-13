namespace CRUD.Utility.Options;

/// <summary>
/// Опции S3Initializer'а.
/// </summary>
public class S3InitializerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "S3Initializer";

    /// <summary>
    /// Директория архива в S3.
    /// </summary>
    /// <remarks>
    /// Из архива копируются все необходимые файлы для инициализации приложения.
    /// </remarks>
    public required string ArchiveInS3Directory { get; set; }
}