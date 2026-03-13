namespace CRUD.Utility.Options;

/// <summary>
/// Опции клиентов.
/// </summary>
public class ClientsOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Clients";

    /// <summary>
    /// URL-адреса веб-клиентов этого WebApi.
    /// </summary>
    public required string[] WebClientURLs { get; set; }
}