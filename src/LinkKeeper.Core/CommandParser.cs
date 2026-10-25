using System.Globalization;
using System.Text.RegularExpressions;

namespace LinkKeeper.Core;

public sealed record ParsedLink(string OriginalUrl, string Category, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);

// This increment validates a command only. It never stores or fetches the URL.
public sealed partial class CommandParser(TimeProvider clock, CategoryCatalog? categories = null)
{
    public ParsedLink Parse(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || command.Length > 4096)
            throw new ArgumentException("Informe um comando com até 4096 caracteres.");
        var parts = command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3)
            throw new ArgumentException("Use exatamente uma URL, --categoria e /validade opcional.");
        var original = parts[0];
        LinkValidation.RequireHttpUrl(original);
        if (!parts[1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("Informe uma categoria usando --categoria.");
        var policy = (categories ?? new CategoryCatalog()).Resolve(parts[1][2..]);
        var category = policy.Name;
        int? days = policy.Permanent ? null : policy.DefaultDays;
        if (parts.Length == 2 && !policy.Permanent && policy.DefaultDays is null)
            throw new ArgumentException("Esta categoria exige validade explícita.");
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
        try { return new(original, category, days is null ? null : now.AddDays(days.Value), now); }
        catch (ArgumentOutOfRangeException) { throw new ArgumentException("Validade excede o intervalo suportado."); }
    }

    [GeneratedRegex(@"^/([0-9]+)(?:d|dias)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Duration();
}
