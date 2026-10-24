namespace LinkKeeper.Core;

public sealed record CategoryRule(string Name, int? DefaultDays = null, bool Permanent = false, string[]? Aliases = null);
public sealed class CategoryCatalog
{
    private readonly Dictionary<string, CategoryRule> rules = new(StringComparer.OrdinalIgnoreCase);
    public CategoryCatalog(IEnumerable<CategoryRule>? custom = null)
    {
        foreach (var rule in new[] {
            new CategoryRule("jobs", 30, Aliases: ["vaga-emprego"]),
            new CategoryRule("fun", Permanent: true), new CategoryRule("art", Permanent: true),
            new CategoryRule("video"), new CategoryRule("estudo"), new CategoryRule("ler-depois")
        }) Add(rule);
        if (custom is not null) foreach (var rule in custom) Add(rule);
    }
    private void Add(CategoryRule rule)
    {
        if (!Valid(rule.Name) || rule.DefaultDays is <= 0 || (rule.Permanent && rule.DefaultDays.HasValue))
            throw new ArgumentException("Política de categoria inválida.");
        rules[rule.Name] = rule with { Name = rule.Name.ToLowerInvariant() };
        foreach (var alias in rule.Aliases ?? [])
        {
            if (!Valid(alias)) throw new ArgumentException("Alias inválido.");
            rules[alias] = rules[rule.Name];
        }
    }
    private static bool Valid(string value) => !string.IsNullOrEmpty(value) && value.All(c => char.IsAsciiLetterOrDigit(c) || c=='-');
    public CategoryRule Resolve(string value) => rules.TryGetValue(value, out var rule)
        ? rule : throw new ArgumentException("Categoria não configurada.");
}
