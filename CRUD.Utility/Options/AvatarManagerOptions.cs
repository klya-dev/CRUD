namespace CRUD.Utility.Options;

/// <summary>
/// Опции AvatarManager'а.
/// </summary>
public class AvatarManagerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "AvatarManager";

    /// <summary>
    /// Директория для аватарок в S3.
    /// </summary>
    public required string AvatarsInS3Directory { get; set; }

    /// <summary>
    /// Путь до дефолтной аватарки в S3.
    /// </summary>
    public required string DefaultAvatarPath { get; set; }

    /// <summary>
    /// Максимальный размер аватарки в байтах.
    /// </summary>
    public required int MaxFileSize { get; set; }

    /// <summary>
    /// Максимальный размер аватарки в мегабайтах словом, без "МБ".
    /// </summary>
    /// <remarks>
    /// <para>Например, "10". "MB/МБ" дорисуется в локализации.</para>
    /// <para>Да, возможно, лучше прописать "10 MB", но пусть локализация тоже отыграет эту ситуацию.</para>
    /// </remarks>
    public required string MaxFileSizeString { get; set; }
}