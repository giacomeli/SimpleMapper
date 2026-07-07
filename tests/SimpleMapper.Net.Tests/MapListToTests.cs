using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class MapListToTests
{
    private class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
    private class Target { public string Name { get; set; } = ""; public int Age { get; set; } }

    [Fact]
    public void MapListTo_MapsAllItems()
    {
        var sources = new List<Source>
        {
            new() { Name = "A", Age = 1 },
            new() { Name = "B", Age = 2 },
            new() { Name = "C", Age = 3 },
        };

        var results = sources.MapListTo<Target>();

        Assert.Equal(3, results.Count);
        Assert.Equal("A", results[0].Name);
        Assert.Equal("B", results[1].Name);
        Assert.Equal(3, results[2].Age);
    }

    [Fact]
    public void MapListTo_EmptyList_ReturnsEmpty()
    {
        var sources = new List<Source>();
        var results = sources.MapListTo<Target>();
        Assert.Empty(results);
    }

    [Fact]
    public void MapListTo_NullableItems_Skipped()
    {
        var sources = new List<Source?> { new() { Name = "A" }, null };
        var results = sources.MapListTo<Target>();
        Assert.Single(results);
        Assert.Equal("A", results[0].Name);
    }
}
