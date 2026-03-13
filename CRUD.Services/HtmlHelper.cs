using AngleSharp.Css.Dom;
using CRUD.Services.Interfaces;
using Ganss.Xss;

namespace CRUD.Services;

/// <inheritdoc cref="IHtmlHelper"/>
public class HtmlHelper : IHtmlHelper
{
    private readonly HtmlSanitizer _htmlSanitizer;

    public HtmlHelper()
    {
        var options = new HtmlSanitizerOptions()
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "abbr", "i", "em", "b", "big", "blockquote", "strong", "br", "ul", "li", "ol", "img", "p", "small", "span", "strike" },
            AllowedSchemes = new HashSet<string>(HtmlSanitizerDefaults.AllowedSchemes, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "alt" },
            UriAttributes = new HashSet<string>(HtmlSanitizerDefaults.UriAttributes, StringComparer.OrdinalIgnoreCase),
            AllowedCssClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "" },
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "" },
            AllowedAtRules = new HashSet<CssRuleType>() { },
            AllowCssCustomProperties = false,
            AllowDataAttributes = false
        };

        _htmlSanitizer = new HtmlSanitizer(options);
    }

    public string SanitizeHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var sanitized = _htmlSanitizer.Sanitize(html);

        return sanitized;
    }
}