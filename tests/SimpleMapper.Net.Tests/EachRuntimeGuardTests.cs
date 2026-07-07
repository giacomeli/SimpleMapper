using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class EachRuntimeGuardTests
{
    private class Node { public string Name { get; set; } = ""; }
    private class Root { public List<Node> Items { get; set; } = new(); public string Title { get; set; } = ""; }

    [Fact]
    public void Each_CalledAtRuntime_Throws()
    {
        var list = new List<string> { "a" };

        Assert.Throws<InvalidOperationException>(() => list.Each());
    }

    [Fact]
    public void PathExtractor_UnsupportedExpression_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PathExtractor.Extract<Root>(x => x.Title.Length + 1));
    }
}
