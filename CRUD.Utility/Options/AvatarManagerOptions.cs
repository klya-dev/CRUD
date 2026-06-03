namespace CRUD.Utility.Options;

/// <summary>
/// Опции AvatarManager'а.
/// </summary>
public sealed class AvatarManagerOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "AvatarManager";

    /// <summary>
    /// Директория для аватарок в S3.
    /// </summary>
    public required string AvatarsInS3Directory { get; init; }

    /// <summary>
    /// Путь до дефолтной аватарки в S3.
    /// </summary>
    public required string DefaultAvatarPath { get; init; }

    /// <summary>
    /// Максимальный размер аватарки в байтах.
    /// </summary>
    public required int MaxFileSize { get; init; }

    /// <summary>
    /// Максимальный размер аватарки в мегабайтах словом, без "МБ".
    /// </summary>
    /// <remarks>
    /// <para>Например, "10". "MB/МБ" дорисуется в локализации.</para>
    /// <para>Да, возможно, лучше прописать "10 MB", но пусть локализация тоже отыграет эту ситуацию.</para>
    /// </remarks>
    public required string MaxFileSizeString { get; init; }
}