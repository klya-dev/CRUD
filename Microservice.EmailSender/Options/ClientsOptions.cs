namespace Microservice.EmailSender.Options;

/// <summary>
/// Опции клиентов.
/// </summary>
public sealed class ClientsOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Clients";

    /// <summary>
    /// URL-адреса веб-клиентов этого WebApi.
    /// </summary>
    public required string[] WebClientURLs { get; init; }
}