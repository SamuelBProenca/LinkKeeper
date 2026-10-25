using LinkKeeper.Core;
using Xunit;

namespace LinkKeeper.Tests;

public sealed class CategoryCatalogTests
{
    [Fact]
    public void UpdatingPolicyRetargetsInheritedAliases()
    {
        var catalog = new CategoryCatalog([new CategoryRule("JOBS", 7)]);
        Assert.Equal(7, catalog.Resolve("vaga-emprego").DefaultDays);
        Assert.Equal("jobs", catalog.Resolve("VAGA-EMPREGO").Name);
    }

    [Fact]
    public void ExplicitAliasReplacementRemovesPreviousAliases()
    {
        var catalog = new CategoryCatalog([new CategoryRule("jobs", 7, Aliases: ["carreira"])]);
        Assert.Equal("jobs", catalog.Resolve("carreira").Name);
        Assert.Throws<ArgumentException>(() => catalog.Resolve("vaga-emprego"));
    }

    [Fact]
    public void EmptyAliasListCanDisableInheritedAliases()
    {
        var catalog = new CategoryCatalog([new CategoryRule("jobs", 7, Aliases: [])]);
        Assert.Throws<ArgumentException>(() => catalog.Resolve("vaga-emprego"));
        Assert.Equal(7, catalog.Resolve("jobs").DefaultDays);
    }

    [Fact]
    public void AliasesCannotShadowAnotherCanonicalCategory() =>
        Assert.Throws<ArgumentException>(() => new CategoryCatalog([
            new CategoryRule("pesquisa", 14, Aliases: ["JOBS"])]));

    [Fact]
    public void CanonicalCategoryCannotShadowAnotherAlias() =>
        Assert.Throws<ArgumentException>(() => new CategoryCatalog([
            new CategoryRule("vaga-emprego", 7)]));

    [Fact]
    public void DifferentCategoriesCannotShareAnAlias() =>
        Assert.Throws<ArgumentException>(() => new CategoryCatalog([
            new CategoryRule("pesquisa", 14, Aliases: ["ref"]),
            new CategoryRule("leituras", 30, Aliases: ["REF"])]));

    [Fact]
    public void RepeatedAliasesForTheSameCategoryRemainUnambiguous()
    {
        var catalog = new CategoryCatalog([
            new CategoryRule("pesquisa", 14, Aliases: ["ref", "REF", "pesquisa"])]);
        Assert.Equal("pesquisa", catalog.Resolve("REF").Name);
    }
}
