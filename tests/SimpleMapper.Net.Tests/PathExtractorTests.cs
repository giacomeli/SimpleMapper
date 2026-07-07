using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class PathExtractorTests
{
    private class Inner { public string Name { get; set; } = ""; public int Age { get; set; } }
    private class Outer { public Inner Inner { get; set; } = new(); public List<Inner> Items { get; set; } = new(); }
    private class Root { public Outer Outer { get; set; } = new(); public string Title { get; set; } = ""; }

    [Fact]
    public void Extract_SingleProperty()
    {
        var path = PathExtractor.Extract<Root>(x => x.Title);
        Assert.Equal(new[] { "Title" }, path);
    }

    [Fact]
    public void Extract_TwoLevels()
    {
        var path = PathExtractor.Extract<Root>(x => x.Outer.Inner);
        Assert.Equal(new[] { "Outer", "Inner" }, path);
    }

    [Fact]
    public void Extract_ThreeLevels()
    {
        var path = PathExtractor.Extract<Root>(x => x.Outer.Inner.Name);
        Assert.Equal(new[] { "Outer", "Inner", "Name" }, path);
    }

    [Fact]
    public void Extract_WithEach()
    {
        var path = PathExtractor.Extract<Root>(x => x.Outer.Items.Each().Name);
        Assert.Equal(new[] { "Outer", "Items", "*", "Name" }, path);
    }

    [Fact]
    public void Extract_ValueType_UnwrapsConvert()
    {
        var path = PathExtractor.Extract<Root>(x => x.Outer.Inner.Age);
        Assert.Equal(new[] { "Outer", "Inner", "Age" }, path);
    }

    [Fact]
    public void Extract_CollectionWithoutEach()
    {
        var path = PathExtractor.Extract<Root>(x => x.Outer.Items);
        Assert.Equal(new[] { "Outer", "Items" }, path);
    }
}
