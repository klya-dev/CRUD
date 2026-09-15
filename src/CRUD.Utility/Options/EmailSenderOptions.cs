namespace CRUD.Utility.Options;

/// <summary>
/// Опции EmailSender'а.
/// </summary>
public sealed class EmailSenderOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "EmailSender";

    /// <summary>
    /// URL сервиса.
    /// </summary>
    public required string ServiceURL { get; init; }

    /// <summary>
    /// Название конечной точки для проверки состояния микросервиса (без "/").
    /// </summary>
    public required string HealthzEndpoint { get; init; }

    /// <summary>
    /// Взаимодействовать ли через Unix Domain Socket вместо TCP.
    /// </summary>
    /// <remarks>
    /// Используется в основном, если EmailSender и WebApi на одном ПК.
    /// </remarks>
    public required bool UseUnixDomainSocketGRPC { get; init; }

    /// <summary>
    /// Имя файла с расширением.
    /// </summary>
    /// <remarks>
    /// <para>Используется в паре с <see cref="UseUnixDomainSocketGRPC"/>.</para>
    /// <para>Файл создаётся в <c>Temp</c> папке.</para>
    /// </remarks>
    public required string FileNameInTempFolder { get; init; }
}