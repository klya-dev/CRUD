#nullable disable
using CRUD.Services;

namespace CRUD.Tests.IntegrationTests;

public class HtmlHelperIntegrationTest
{
    // #nullable disable

    private readonly HtmlHelper _htmlHelper;

    // Примеры html
    private readonly string[] htmls =
    [
        "<a href='https://example.com'>Подпишись на мои соцсети</a>",
        "<script>alert('XSS');</script>",
        "<img src='x' onerror='alert(1)'/>",
        "<a href='javascript:alert(1)'>Click me</a>",
        "<div style='background-image:url(java\\0script:alert(1))'>Hello</div>",
        "<p>Testing <b>bold</b> content</p>",
        "<a href='https://example.com' onclick='doEvil()'>Link</a>",
        "<iframe src='http://evil.com'></iframe>",
        "<style>body { background: url('http://evil.com'); }</style>",
        "<p><b>Safe content</b></p>",
        "<div>ВРЕДОНОСНЫЙ КОД</div>"
    ];

    // Их очищенные результаты
    private readonly string[] sanitizedHtmls =
    [
        "<a href=\"https://example.com\">Подпишись на мои соцсети</a>",
        "",
        "<img>",
        "<a>Click me</a>",
        "",
        "<p>Testing <b>bold</b> content</p>",
        "<a href=\"https://example.com\">Link</a>",
        "",
        "",
        "<p><b>Safe content</b></p>",
        "",
    ];

    public HtmlHelperIntegrationTest()
    {
        _htmlHelper = new HtmlHelper();
    }

    private static HtmlHelper GenerateNewHtmlHelper()
    {
        return new HtmlHelper();
    }

    [Fact]
    public void SanitizeHtml_CorrectData_ReturnsString()
    {
        // Arrange

        // Act
        for (int i = 0; i < htmls.Length; i++)
        {
            var html = htmls[i];
            var sanitizedHtml = sanitizedHtmls[i];

            var result = _htmlHelper.SanitizeHtml(html);

            // Assert
            Assert.NotNull(result);
            Assert.Equivalent(sanitizedHtml, result);
        }
    }

    [Fact]
    public void SanitizeHtml_NullObject_ThrowsArgumentNullException()
    {
        // Arrange
        string html = null;

        // Act
        Action a = () =>
        {
            _htmlHelper.SanitizeHtml(html);
        };

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(a);
        Assert.Contains(nameof(html), ex.ParamName);
    }


    // Конфликть параллельности


    [Fact]
    public async Task SanitizeHtml_ConcurrencyConflict_CorrectData_ReturnsString()
    {
        // Arrange
        var htmlHelper = GenerateNewHtmlHelper();
        var htmlHelper2 = GenerateNewHtmlHelper();

        // Act
        for (int i = 0; i < htmls.Length; i++)
        {
            var html = htmls[i];
            var sanitizedHtml = sanitizedHtmls[i];

            var task = Task.Run(() => htmlHelper.SanitizeHtml(html));
            var task2 = Task.Run(() => htmlHelper2.SanitizeHtml(html));

            var results = await Task.WhenAll(task, task2);
            var result = results[0];
            var result2 = results[1];

            // Assert
            Assert.NotNull(result);
            Assert.Equivalent(sanitizedHtml, result);

            Assert.Equivalent(result, result2);
        }
    }
}