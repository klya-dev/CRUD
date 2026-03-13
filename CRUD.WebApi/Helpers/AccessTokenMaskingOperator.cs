using Serilog.Enrichers.Sensitive;
using System.Text.RegularExpressions;

namespace CRUD.WebApi.Helpers;

/// <summary>
/// Оператор маскировки <c>access_token</c>'а в логах.
/// </summary>
/// <remarks>
/// Например, было "<c>HTTP GET /notificationHub?id=7It...&amp;access_token=eyJhbGci...</c>", стало "<c>HTTP GET /notificationHub?id=7It...&amp;access_token=***MASKED***</c>".
/// </remarks>
public class AccessTokenMaskingOperator : RegexMaskingOperator
{
    private const string pattern = "access_token=[A-z0-9\\-_]+\\.[A-z0-9\\-_]+\\.[A-z0-9\\-_]+";

    public AccessTokenMaskingOperator() : base(pattern) { }

    protected override string PreprocessMask(string mask, Match match) => "access_token=" + mask;
}