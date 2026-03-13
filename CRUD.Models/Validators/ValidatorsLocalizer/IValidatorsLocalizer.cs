namespace CRUD.Models.Validators.ValidatorsLocalizer;

/// <summary>
/// Сервис для локализации валидаторов.
/// </summary>
/// <remarks>
/// <para>Используй ключи из <see cref="ValidatorsLocalizerConstants"/>.</para>
/// <para>Для использования локализации, необходимо вызвать <c><see cref="WebApi.Extensions.LocalizationServiceCollectionExtensions.AddReadyLocalization(IServiceCollection)"/></c> в <c>Program.cs</c>.</para>
/// </remarks>
public interface IValidatorsLocalizer
{
    /// <summary>
    /// Получает локализированную строку по ключу с возможной заменой аргументов.
    /// </summary>
    /// <remarks>
    /// <para>Если <paramref name="args"/> = <see langword="null"/>, то исключение <see cref="ArgumentNullException"/></para>
    /// <para>Если ключ не найден, возвращается <see langword="null"/>.</para>
    /// <para>Если язык не поддерживается, возвращается <paramref name="key"/>.</para>
    /// <para>Если элементов <paramref name="args"/> больше 0, то используется локализация с заменой аргументов через метод <see cref="ReplaceParams(string, List{string})"/>.</para>
    /// <para>Язык берётся из <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Если <paramref name="args"/> = <see langword="null"/>.</exception>
    /// <param name="key">Ключ из <see cref="ValidatorsLocalizerConstants"/>.</param>
    /// <param name="args">Массив аргументов для замены.</param>
    /// <returns>Локализированная строка.</returns>
    string this[string key, params object[] args] { get; }

    /// <summary>
    /// Заменяет аргументы у ключа на элементы списка.
    /// </summary>
    /// <remarks>
    /// <para>Где <c>$A$</c> у ключа заменяется на первый элемент списка.</para>
    /// <para>Если ключ не найден, выбрасывается исключение <see cref="ArgumentException"/>.</para>
    /// <example>
    /// Как пользоваться
    /// <code>
    /// RuleFor(x => x.Firstname).Firstname().WithMessage(localizer[ValidatorsLocalizerConstants.TestParams, 10, "100"]);
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="ArgumentException">Если ключ не найден.</exception>
    /// <param name="key">Ключ из <see cref="ValidatorsLocalizerConstants"/>, который содержит "$A$, $B$...".</param>
    /// <param name="args">Список элементов для замены.</param>
    /// <returns>Локализированная строка с заменёнными аргументами.</returns>
    string ReplaceParams(string key, List<string> args);
}