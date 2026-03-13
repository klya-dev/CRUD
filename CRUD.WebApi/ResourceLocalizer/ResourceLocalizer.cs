using Microsoft.Extensions.Localization;
using System.Reflection;

namespace CRUD.WebApi.ResourceLocalizer;

/// <inheritdoc cref="IResourceLocalizer"/>
public class ResourceLocalizer : IResourceLocalizer
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

    public string ReplaceParams(string key, List<string> args)
    {
        var localizeOriginal = _localizer[key].ToString();

        for (int i = 0; i < SD.Alphabet.Length; i++)
        {
            var replaced = $"${SD.Alphabet[i]}$";
            if (localizeOriginal.Contains(replaced) && args.Count > i)
                localizeOriginal = localizeOriginal.Replace(replaced, args[i]);
            else
                return localizeOriginal;
        }

        return localizeOriginal;
    }
}