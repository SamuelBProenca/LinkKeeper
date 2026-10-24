using System.Globalization;
using System.Text.RegularExpressions;

namespace LinkKeeper.Core;

public sealed record ParsedLink(string OriginalUrl, string Category, DateTimeOffset? ExpiresAt);

// This increment validates a command only. It never stores or fetches the URL.
public sealed partial class CommandParser(TimeProvider clock)
{
    public ParsedLink Parse(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > 4096)
            throw new ArgumentException("Informe um comando com até 4096 caracteres.");
        var parts = command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3)
            throw new ArgumentException("Use exatamente uma URL, --categoria e /validade opcional.");
        var original = parts[0];
        if (!Uri.TryCreate(original, UriKind.Absolute, out var url) ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(url.Host) || !string.IsNullOrEmpty(url.UserInfo) ||
            original.Any(char.IsControl))
            throw new ArgumentException("URL HTTP/HTTPS inválida; credenciais na URL não são aceitas.");
        if (!parts[1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("Informe uma categoria usando --categoria.");
        var category = parts[1][2..].ToLowerInvariant();
        category = category == "vaga-emprego" ? "jobs" : category;
        if (category is not ("jobs" or "fun" or "art"))
            throw new ArgumentException("Categoria não configurada. Disponíveis: jobs, fun, art.");
        int? days = category == "jobs" ? 30 : null;
        if (parts.Length == 3)
        {
            if (parts[2].Equals("/permanente", StringComparison.OrdinalIgnoreCase)) days = null;
            else
            {
                var match = Duration().Match(parts[2]);
                if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var value) || value <= 0)
                    throw new ArgumentException("Validade inválida. Use /7d, /7dias ou /permanente.");
                days = value;
            }
        }
        var now = clock.GetUtcNow();
        try { return new(original, category, days is null ? null : now.AddDays(days.Value)); }
        catch (ArgumentOutOfRangeException) { throw new ArgumentException("Validade excede o intervalo suportado."); }
    }

    [GeneratedRegex(@"^/([0-9]+)(?:d|dias)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Duration();
}
