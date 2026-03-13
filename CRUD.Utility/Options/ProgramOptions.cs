namespace CRUD.Utility.Options;

/// <summary>
/// Опции <c>Program.cs</c>.
/// </summary>
public class ProgramOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Program";

    /// <summary>
    /// Пропустить ли логирование.
    /// </summary>
    public required bool SkipLogging { get; set; }

    /// <summary>
    /// Пропустить ли инициализаторы (база данных, S3).
    /// </summary>
    public required bool SkipInitializers { get; set; }
}