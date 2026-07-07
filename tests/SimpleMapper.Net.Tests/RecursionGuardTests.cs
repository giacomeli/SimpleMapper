using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Guards against CWE-674 (uncontrolled recursion): a cyclic or extremely deep
/// object graph must fail with a catchable exception instead of terminating the
/// process with a StackOverflowException.
/// </summary>
public sealed class RecursionGuardTests
{
    private class Node
    {
        public string Name { get; set; } = "";
        public Node? Next { get; set; }
    }

    private class NodeDto
    {
        public string Name { get; set; } = "";
        public NodeDto? Next { get; set; }
    }

    private static Node BuildChain(int depth)
    {
        var head = new Node { Name = "0" };
        var cur = head;
        for (int i = 1; i < depth; i++)
        {
            cur.Next = new Node { Name = i.ToString() };
            cur = cur.Next;
        }
        return head;
    }

    [Fact]
    public void CyclicGraph_ThrowsMappingDepthExceeded_NotStackOverflow()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b", Next = a };
        a.Next = b; // cycle: a -> b -> a

        Assert.Throws<MappingDepthExceededException>(() => a.MapTo<NodeDto>());
    }

    [Fact]
    public void DeepGraph_BeyondMaxDepth_ThrowsMappingDepthExceeded()
    {
        var head = BuildChain(SimpleMapperOptions.MaxDepth + 50);

        Assert.Throws<MappingDepthExceededException>(() => head.MapTo<NodeDto>());
    }

    [Fact]
    public void ShallowGraph_WithinMaxDepth_MapsSuccessfully()
    {
        var head = BuildChain(5);

        var dto = head.MapTo<NodeDto>();

        Assert.Equal("0", dto.Name);
        Assert.Equal("4", dto.Next!.Next!.Next!.Next!.Name);
    }

    [Fact]
    public void AfterDepthExceeded_ThreadCounterResets_SubsequentMappingWorks()
    {
        var a = new Node { Name = "a" };
        a.Next = a; // self-cycle

        Assert.Throws<MappingDepthExceededException>(() => a.MapTo<NodeDto>());

        // The per-thread depth counter must be restored so the thread stays usable.
        var ok = BuildChain(3).MapTo<NodeDto>();
        Assert.Equal("0", ok.Name);
        Assert.Equal("2", ok.Next!.Next!.Name);
    }

    [Fact]
    public void MaxDepth_IsConfigurable()
    {
        var original = SimpleMapperOptions.MaxDepth;
        try
        {
            SimpleMapperOptions.MaxDepth = 10;
            var head = BuildChain(20);

            Assert.Throws<MappingDepthExceededException>(() => head.MapTo<NodeDto>());
        }
        finally
        {
            SimpleMapperOptions.MaxDepth = original;
        }
    }
}
