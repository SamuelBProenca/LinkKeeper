namespace LinkKeeper.Core;

// Shared invariants apply even when a caller constructs ParsedLink without the command parser.
public static class LinkValidation
{
    public static Uri RequireHttpUrl(string? original)
    {
        if (string.IsNullOrWhiteSpace(original) || original.Length > 4096 ||
            original.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) ||
            !Uri.TryCreate(original, UriKind.Absolute, out var url) ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(url.Host) || !string.IsNullOrEmpty(url.UserInfo))
            throw new ArgumentException("URL HTTP/HTTPS inválida; credenciais na URL não são aceitas.");
        return url;
    }

    public static ParsedLink Canonicalize(ParsedLink parsed, CategoryCatalog categories)
    {
        ArgumentNullException.ThrowIfNull(parsed);
        RequireHttpUrl(parsed.OriginalUrl);
        var category = categories.Resolve(parsed.Category).Name;
        if (parsed.CreatedAt == default)
            throw new ArgumentException("Data de criação obrigatória.");
        if (parsed.ExpiresAt is { } expiry && expiry <= parsed.CreatedAt)
            throw new ArgumentException("A validade deve ser posterior à criação.");
        return parsed with {
            Category = category,
            CreatedAt = parsed.CreatedAt.ToUniversalTime(),
            ExpiresAt = parsed.ExpiresAt?.ToUniversalTime()
        };
    }
}
