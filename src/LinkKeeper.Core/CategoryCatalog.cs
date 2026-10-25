namespace LinkKeeper.Core;

public sealed record CategoryRule(string Name, int? DefaultDays = null, bool Permanent = false, string[]? Aliases = null);

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, CategoryRule> rules = new(StringComparer.OrdinalIgnoreCase);

    public CategoryCatalog(IEnumerable<CategoryRule>? custom = null)
    {
        CategoryRule[] defaults = [
            new("jobs", 30, Aliases: ["vaga-emprego"]),
            new("fun", Permanent: true), new("art", Permanent: true),
            new("video"), new("estudo"), new("ler-depois")
        ];
        var canonical = new Dictionary<string, CategoryRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in defaults.Concat(custom ?? []))
        {
            if (rule is null || !Valid(rule.Name) || rule.DefaultDays is <= 0 ||
                (rule.Permanent && rule.DefaultDays.HasValue))
                throw new ArgumentException("Política de categoria inválida.");
            var name = rule.Name.ToLowerInvariant();
            // Omitted aliases inherit the previous policy; an explicit array replaces them.
            canonical.TryGetValue(name, out var previous);
            var aliases = rule.Aliases ?? previous?.Aliases ?? [];
            if (aliases.Any(alias => !Valid(alias))) throw new ArgumentException("Alias inválido.");
            canonical[name] = rule with { Name = name, Aliases = [.. aliases] };
        }

        foreach (var rule in canonical.Values) rules.Add(rule.Name, rule);
        foreach (var rule in canonical.Values)
        {
            foreach (var alias in rule.Aliases!)
            {
                if (rules.TryGetValue(alias, out var existing) && existing.Name != rule.Name)
                    throw new ArgumentException("Alias em conflito com outra categoria ou alias.");
                rules[alias] = rule;
            }
        }
    }

    private static bool Valid(string? value) => !string.IsNullOrEmpty(value) &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    public CategoryRule Resolve(string value) => rules.TryGetValue(value, out var rule)
        ? rule : throw new ArgumentException("Categoria não configurada.");
}
