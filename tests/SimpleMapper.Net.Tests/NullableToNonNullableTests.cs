using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Pins skip-if-null for Nullable&lt;T&gt; sources mapped onto non-nullable value targets
/// (int? -> int, int? -> long): a null source is skipped and the target keeps its default,
/// on every execution path and inside collection items. A value is copied as usual.
/// </summary>
public sealed class NullableToNonNullableTests
{
    private class Source
    {
        public string Name { get; set; } = "";
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? Count { get; set; }
    }

    private class Target
    {
        public string Name { get; set; } = "";
        public int Month { get; set; }
        public int Year { get; set; } = 2000;
        public long Count { get; set; }
    }

    private class Parent
    {
        public string Name { get; set; } = "";
        public List<Source> Items { get; set; } = new();
        public Source? Single { get; set; }
    }

    private class ParentTarget
    {
        public string Name { get; set; } = "";
        public List<Target> Items { get; set; } = new();
        public Target? Single { get; set; }
    }

    [Fact]
    public void NullSource_IsSkipped_OnFastPath()
    {
        var result = new Source { Name = "n" }.MapTo<Target>();

        Assert.Equal(0, result.Month);
        Assert.Equal(2000, result.Year);
        Assert.Equal(0L, result.Count);
    }

    [Fact]
    public void NullSource_IsSkipped_OnPlanPath()
    {
        var result = new Source { Name = "n" }.Map().Ignore(nameof(Source.Name)).To<Target>();

        Assert.Equal(0, result.Month);
        Assert.Equal(2000, result.Year);
        Assert.Equal(0L, result.Count);
    }

    [Fact]
    public void NullSource_IsSkipped_InCollectionItemsAndNestedObjects()
    {
        var src = new Parent
        {
            Items = new List<Source> { new() { Name = "a" }, new() { Name = "b", Month = 3, Year = 2031, Count = 9 } },
            Single = new Source { Name = "s" },
        };

        var fast = src.MapTo<ParentTarget>();
        var plan = src.Map().Ignore(nameof(Parent.Name)).To<ParentTarget>();

        foreach (var result in new[] { fast, plan })
        {
            Assert.Equal(0, result.Items[0].Month);
            Assert.Equal(2000, result.Items[0].Year);
            Assert.Equal(3, result.Items[1].Month);
            Assert.Equal(2031, result.Items[1].Year);
            Assert.Equal(9L, result.Items[1].Count);
            Assert.Equal(0, result.Single!.Month);
        }
    }

    [Fact]
    public void Value_IsCopied_IncludingNumericWidening()
    {
        var result = new Source { Month = 12, Year = 2030, Count = 42 }.MapTo<Target>();

        Assert.Equal(12, result.Month);
        Assert.Equal(2030, result.Year);
        Assert.Equal(42L, result.Count);
    }
}
