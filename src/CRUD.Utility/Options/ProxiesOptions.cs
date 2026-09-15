namespace CRUD.Utility.Options;

/// <summary>
/// Опции прокси серверов.
/// </summary>
public sealed class ProxiesOptions
{
    /// <summary>
    /// Название секции.
    /// </summary>
    public const string SectionName = "Proxies";

    /// <summary>
    /// Список доверенных удалённых прокси IP-адресов / сетей (диапазонов).
    /// </summary>
    /// <remarks>
    /// Диапазон локальных IP-адресов уже включен по умолчанию.
    /// </remarks>
    public required string[] RemoteProxyIps { get; init; }

    /// <summary>
    /// Лимит обрабатываемых элементов цепочки X-Forwarded-For.
    /// </summary>
    /// <remarks>
    /// Например, 2. Где первый элемент - это платёжный шлюз (реальный IP-адрес), а второй элемент - это локальный прокси.
    /// </remarks>
    public required int ForwardLimit { get; init; }
}