using AngleSharp.Css.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace CRUD.Services;

/// <inheritdoc cref="IHtmlHelper"/>
public sealed class HtmlHelper : IHtmlHelper
{
    private readonly HtmlSanitizer _htmlSanitizer;

    public HtmlHelper()
    {
        var options = new HtmlSanitizerOptions()
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "abbr", "i", "em", "b", "big", "blockquote", "strong", "br", "ul", "li", "ol", "img", "p", "small", "span", "strike" },
            AllowedSchemes = new HashSet<string>(HtmlSanitizerDefaults.AllowedSchemes, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "alt", "src" },
            UriAttributes = new HashSet<string>(HtmlSanitizerDefaults.UriAttributes, StringComparer.OrdinalIgnoreCase),
            AllowedCssClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowedAtRules = new HashSet<CssRuleType>() { },
            AllowCssCustomProperties = false,
            AllowDataAttributes = false
        };

        _htmlSanitizer = new HtmlSanitizer(options);
        _htmlSanitizer.PostProcessNode += OnPostProcessNode;
    }

    private void OnPostProcessNode(object? sender, PostProcessNodeEventArgs e)
    {
        // Защита <a> от Tabnabbing и фишинга
        if (e.Node is IHtmlAnchorElement anchor)
        {
            // Безопасный rel
            anchor.SetAttribute("rel", "noopener noreferrer nofollow");

            // Ссылки будут открываться в новом окне
            anchor.SetAttribute("target", "_blank");
        }

        // Защита <img> от внешних источников (src)
        if (e.Node is IHtmlImageElement image)
        {
            // Включаем ленивую загрузку (снижает нагрузку и защищает от мгновенного срабатывания)
            image.SetAttribute("loading", "lazy");

            // Удаляем атрибут src, если он не ведёт на безопасный домен (imgur.com)
            var src = image.GetAttribute("src");
            if (!string.IsNullOrWhiteSpace(src) && Uri.TryCreate(src, UriKind.Absolute, out var uri)) // Если источник не пустой и ссылка корректна и абсолютна
            {
                if (uri.Host != "imgur.com")
                    e.ReplacementNodes.Clear(); // Удаляем src
            }
        }
    }

    public string SanitizeHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var sanitized = _htmlSanitizer.Sanitize(html);

        return sanitized;
    }
}