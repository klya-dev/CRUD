using Microsoft.Extensions.Localization;

namespace CRUD.WebApi.ResourceLocalizer;

/// <summary>
/// Сервис для локализации из ресурсов.
/// </summary>
/// <remarks>
/// <para>Используй ключи из <see cref="ResourceLocalizerConstants"/>.</para>
/// <para>Для использования локализации из ресурсов, необходимо вызвать <c><see cref="CRUD.WebApi.Extensions.LocalizationServiceCollectionExtensions.AddReadyLocalization(IServiceCollection)"/></c> в <c>Program.cs</c>.</para>
/// </remarks>
public interface IResourceLocalizer
{
    /// <summary>
    /// Получить локализированную строку по ключу.
    /// </summary>
    /// <param name="key">Ключ из <see cref="ResourceLocalizerConstants"/>.</param>
    LocalizedString this[string key] { get; }

    /// <summary>
    /// Заменяет аргументы у ключа на элементы списка.
    /// </summary>
    /// <remarks>
    /// Где <c>$A$</c> у ключа заменяется на первый элемент списка.
    /// </remarks>
    /// <param name="key">Ключ из <see cref="ResourceLocalizerConstants"/>, который содержит "$A$, $B$...".</param>
    /// <param name="args">Список элементов для замены.</param>
    /// <returns>Локализированная строка с заменёнными аргументами.</returns>
    string ReplaceParams(string key, List<string> args);
}