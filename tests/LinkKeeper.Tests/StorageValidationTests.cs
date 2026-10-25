using LinkKeeper.Core;
using LinkKeeper.Storage;
using Microsoft.Data.Sqlite;
using Xunit;

namespace LinkKeeper.Tests;

public sealed class StorageValidationTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"linkkeeper-validation-{Guid.NewGuid():N}.db");
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 0, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private readonly Clock clock = new();
    private LinkStore Store => new(path, clock);
    private static ParsedLink Valid => new("https://example.com", "jobs", Now.AddDays(7), Now);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://example.com/with space")]
    [InlineData("https://example.com/\u0001")]
    public void DirectStorageWritesRejectUnsafeUrls(string url) =>
        Assert.Throws<ArgumentException>(() => Store.Add("one", Valid with { OriginalUrl = url }));

    [Fact]
    public void DirectStorageWritesRejectUnknownCategories() =>
        Assert.Throws<ArgumentException>(() => Store.Add("one", Valid with { Category = "unknown" }));

    [Fact]
    public void DirectStorageWritesCanonicalizeAliasesBeforeDeduplication()
    {
        var first = Store.Add("one", Valid with { Category = "VAGA-EMPREGO" });
        var duplicate = Store.Add("one", Valid);
        Assert.Equal("jobs", first.Item.Category);
        Assert.False(duplicate.Created);
        Assert.Equal(first.Item.Id, duplicate.Item.Id);
    }

    [Fact]
    public void DirectStorageWritesRequireCreationTime() =>
        Assert.Throws<ArgumentException>(() => Store.Add("one", Valid with { CreatedAt = default }));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExpiryMustFollowCreation(int days) =>
        Assert.Throws<ArgumentException>(() => Store.Add("one", Valid with { ExpiresAt = Now.AddDays(days) }));

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("Active, Active")]
    [InlineData(" Active ")]
    [InlineData("active")]
    public void StatusQueriesRequireTheDocumentedTextualValues(string status) =>
        Assert.Throws<ArgumentException>(() => Store.List("one", status));

    [Fact]
    public void RejectedWriteDoesNotLeavePartialData()
    {
        Assert.Throws<ArgumentException>(() => Store.Add("one", Valid with { Category = "unknown" }));
        Assert.Empty(Store.List("one"));
    }

    [Fact]
    public void StorageAcceptsConfiguredCategoriesAndCanonicalizesTheirAliases()
    {
        var catalog = new CategoryCatalog([new CategoryRule("pesquisa", 14, Aliases: ["research"])]);
        var store = new LinkStore(path, clock, catalog);
        var result = store.Add("one", Valid with { Category = "RESEARCH" });
        Assert.Equal("pesquisa", result.Item.Category);
        Assert.Single(store.List("one", category: "pesquisa"));
    }

    [Fact]
    public void NormalizationPreservesQueryAndFragmentAndOriginalUrl()
    {
        const string original = "https://EXAMPLE.com:443/path?q=Value#section";
        var result = Store.Add("one", Valid with { OriginalUrl = original });
        Assert.Equal(original, result.Item.OriginalUrl);
        Assert.Equal("https://example.com/path?q=Value#section", result.Item.NormalizedUrl);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" }) File.Delete(path + suffix);
    }
}
