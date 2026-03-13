namespace Microservice.EmailSender.Options;

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
    /// Взаимодействовать ли через Unix Domain Socket вместо TCP.
    /// </summary>
    /// <remarks>
    /// Используется в основном, если микросервис и клиент на одном ПК.
    /// </remarks>
    public required bool UseUnixDomainSocketGRPC { get; set; }

    /// <summary>
    /// Имя файла с расширением.
    /// </summary>
    /// <remarks>
    /// <para>Используется в паре с <see cref="UseUnixDomainSocketGRPC"/>.</para>
    /// <para>Файл создаётся в <c>Temp</c> папке.</para>
    /// </remarks>
    public required string FileNameInTempFolder { get; set; }

    /// <summary>
    /// Пропустить ли логирование.
    /// </summary>
    public required bool SkipLogging { get; set; }
}