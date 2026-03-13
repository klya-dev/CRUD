using Microsoft.AspNetCore.Http;

namespace CRUD.Tests.TestImplementions;

/// <summary>
/// Тестовая реализация интерфейса <see cref="IHttpContextAccessor"/>.
/// </summary>
public class TestHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }

    public TestHttpContextAccessor()
    {
        HttpContext = new DefaultHttpContext();

        HttpContext.Request.Scheme = "https";
        var host = TestSettingsHelper.GetAppHost();
        this.HttpContext.Request.Host = new HostString(host);
    }
}