using System.Globalization;

namespace CRUD.Models.Validators.Localization;

/// <inheritdoc cref="IValidatorLocalizer"/>
public sealed class ValidatorLocalizer : IValidatorLocalizer
{
    // Не хочу, чтобы зависило от ресурсов, поэтому захардкодил переводы, их не так много
    // По мне это даже получше ресурсов, полный контроль над процессом. Прикол в том, что при изменении ресурсов, всё равно нужно пересобирать проект, т.к ресурсы в DLL'ке хранятся, лучше уж базу данных использовать

    public string this[string key, params object[] args]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(args);

            var currentCulture = CultureInfo.CurrentUICulture;
            if (args.Length > 0)
            {
                var paramsStrings = args.Select(x => x.ToString()).ToList();
                return currentCulture.Name switch
                {
                    "ru" => ReplaceParams(key, paramsStrings!),
                    "en" => ReplaceParams(key, paramsStrings!),
                    _ => key,
                };
            }

            return currentCulture.Name switch
            {
                "ru" => RussianValidatorLanguage.GetTranslation(key),
                "en" => EnglishValidatorLanguage.GetTranslation(key),
                _ => key,
            };
        }
    }

    public string ReplaceParams(string key, IEnumerable<string> args)
    {
        var localizeOriginal = this[key] ?? throw new ArgumentException(ErrorMessages.KeyNotFound, nameof(key));

        for (int i = 0; i < SD.Alphabet.Length; i++)
        {
            var replaced = $"${SD.Alphabet[i]}$";
            if (localizeOriginal.Contains(replaced) && args.Skip(i).Any()) // MA0031. Быстрее скипнуть несколько элементов, чем считать всю коллекцию (args.Count() > i)
                localizeOriginal = localizeOriginal.Replace(replaced, args.ElementAt(i));
            else
                return localizeOriginal;
        }

        return localizeOriginal;
    }
}