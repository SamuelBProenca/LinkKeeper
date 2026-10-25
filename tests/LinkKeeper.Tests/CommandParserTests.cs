using LinkKeeper.Core;
using Xunit;

namespace LinkKeeper.Tests;

public sealed class CommandParserTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private readonly CommandParser parser = new(new Clock());

    [Theory]
    [InlineData("jobs", "jobs", 30)]
    [InlineData("VAGA-EMPREGO", "jobs", 30)]
    [InlineData("fun", "fun", 0)]
    [InlineData("art", "art", 0)]
    public void AppliesHistoricalPolicies(string input, string expected, int days)
    {
        var result = parser.Parse($"https://example.com --{input}");
        Assert.Equal(expected, result.Category);
        Assert.Equal(days == 0 ? (DateTimeOffset?)null : Now.AddDays(days), result.ExpiresAt);
    }
    [Theory]
    [InlineData("/7d")]
    [InlineData("/7dias")]
    [InlineData("/7DIAS")]
    public void ExplicitDurationOverridesDefaultAndUsesExactUtcPeriods(string duration)
    {
        var result = parser.Parse($"  https://example.com/a?q=X#f  --FUN  {duration}  ");
        Assert.Equal(Now.AddHours(168), result.ExpiresAt);
        Assert.Equal("https://example.com/a?q=X#f", result.OriginalUrl);
    }
    [Fact]
    public void ExplicitPermanentOverridesJobs() =>
        Assert.Null(parser.Parse("https://example.com --jobs /permanente").ExpiresAt);

    [Theory]
    [InlineData("")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com jobs")]
    [InlineData("https://example.com --unknown")]
    [InlineData("javascript:alert(1) --fun")]
    [InlineData("not-a-url --fun")]
    [InlineData("https://user:password@example.com --fun")]
    [InlineData("https://example.com --jobs /0d")]
    [InlineData("https://example.com --jobs /-1d")]
    [InlineData("https://example.com --jobs /1.5d")]
    [InlineData("https://example.com --jobs /999999999999d")]
    [InlineData("https://example.com --jobs /2147483647d")]
    [InlineData("https://example.com --jobs https://second.example")]
    [InlineData("https://example.com https://second.example --fun /7d")]
    public void RejectsInvalidCommandsWithoutPartialResults(string command) =>
        Assert.Throws<ArgumentException>(() => parser.Parse(command));
    [Fact]
    public void CategoriesWithoutDefaultRequireExplicitDuration()
    {
        Assert.Throws<ArgumentException>(() => parser.Parse("https://example.com --video"));
        Assert.Equal(Now.AddDays(30), parser.Parse("https://example.com --video /30d").ExpiresAt);
    }
    [Fact]
    public void CustomCategoryPoliciesAndAliasesAreConfigurable()
    {
        var custom = new CommandParser(new Clock(), new CategoryCatalog([
            new CategoryRule("pesquisa", 14, Aliases: ["research"])]));
        var item = custom.Parse("https://example.com --RESEARCH");
        Assert.Equal("pesquisa", item.Category);
        Assert.Equal(Now.AddDays(14), item.ExpiresAt);
    }
    [Fact]
    public void RejectsOversizedMessages() =>
        Assert.Throws<ArgumentException>(() => parser.Parse(new string('x', 4097)));
}
