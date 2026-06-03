using Microsoft.Extensions.Localization;
using System.Reflection;

namespace CRUD.WebApi.Localization;

/// <inheritdoc cref="IResourceLocalizer"/>
public sealed class ResourceLocalizer : IResourceLocalizer
{
    private readonly IStringLocalizer _localizer;

    public ResourceLocalizer(IStringLocalizerFactory factory)
    {
        var type = typeof(SharedResource);
        var assemblyName = new AssemblyName(type.Assembly.FullName!);
        _localizer = factory.Create("Messages", assemblyName.Name!); // У меня файл ресурсов называется "Messages.resx", значит baseName: "Messages"
    }

    public LocalizedString this[string name]
    {
        get => _localizer[name];
    }

    public string ReplaceParams(string key, IEnumerable<string> args)
    {
        var localizeOriginal = _localizer[key].ToString();

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