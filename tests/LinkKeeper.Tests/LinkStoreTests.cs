using LinkKeeper.Core;
using LinkKeeper.Storage;
using Xunit;

namespace LinkKeeper.Tests;
public sealed class LinkStoreTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"linkkeeper-{Guid.NewGuid():N}.db");
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private readonly Clock clock = new();
    private LinkStore Store => new(path, clock);
    private ParsedLink Parse(string value = "https://example.com --jobs /7d") => new CommandParser(clock).Parse(value);
    [Fact]
    public void SurvivesReopeningAndDoesNotExtendDuplicateExpiry()
    {
        var first = Store.Add("one", Parse(), "Minha referência");
        clock.Now = clock.Now.AddDays(1);
        var duplicate = Store.Add("one", Parse());
        Assert.True(first.Created);
        Assert.Equal(TimeSpan.FromDays(7), first.Item.ExpiresAt - first.Item.CreatedAt);
        Assert.False(duplicate.Created);
        Assert.Equal(first.Item.Id, duplicate.Item.Id);
        Assert.Equal(first.Item.ExpiresAt, duplicate.Item.ExpiresAt);
        Assert.Single(Store.List("one", search: "referência"));
    }
    [Fact]
    public void ExpirationPreservesHistoryAndRenewalRestoresActive()
    {
        var first = Store.Add("one", Parse()).Item;
        clock.Now = clock.Now.AddDays(7);
        Assert.Equal(1, Store.Expire());
        Assert.Equal(0, Store.Expire());
        Assert.Empty(Store.List("one"));
        Assert.Equal(first.OriginalUrl, Assert.Single(Store.List("one", "Expired")).OriginalUrl);
        Assert.True(Store.Renew("one", first.Id, 3));
        Assert.Equal(clock.Now.AddDays(3), Assert.Single(Store.List("one")).ExpiresAt);
    }
    [Fact]
    public void EveryOperationScopesOwnerAndArchiveCannotBeRenewed()
    {
        var first = Store.Add("one", Parse()).Item;
        Assert.Null(Store.Get("two", first.Id));
        Assert.Empty(Store.List("two"));
        Assert.False(Store.Archive("two", first.Id));
        clock.Now = clock.Now.AddDays(8);
        Assert.False(Store.Renew("two", first.Id, 7));
        Assert.True(Store.Archive("one", first.Id));
        Assert.False(Store.Renew("one", first.Id, 7));
        Assert.Single(Store.List("one", "Archived"));
    }
    [Fact]
    public async Task ConcurrentWorkersOnlyTransitionOnce()
    {
        var store = Store;
        store.Add("one", Parse());
        clock.Now = clock.Now.AddDays(8);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => store.Expire())));
        Assert.Equal(1, results.Sum());
        Assert.Single(store.List("one", "Expired"));
    }
    [Fact]
    public void SameUrlMayExistInDifferentCategoriesAndOwners()
    {
        Store.Add("one", Parse());
        Store.Add("one", Parse("https://example.com --fun"));
        Store.Add("two", Parse());
        Assert.Equal(2, Store.List("one").Count);
        Assert.Single(Store.List("one", category: "fun"));
        Assert.Single(Store.List("two"));
    }
    public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); foreach(var suffix in new[]{"", "-shm", "-wal"}) File.Delete(path + suffix); }
}
