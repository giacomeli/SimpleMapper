using System.Collections.Concurrent;
using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Covers the ObjectConstructionMode contract: the strict default refuses to create
/// targets without a parameterless constructor (naming the type and both ways out),
/// while the global option and the per-call builder opt-in restore the uninitialized
/// fallback — the per-call form without leaking to other mappings or threads.
/// Shares a serialized collection with every test class that depends on the global
/// construction mode, because the option is process-wide state.
/// </summary>
[Collection("ObjectConstruction")]
public sealed class ObjectConstructionModeTests
{
    private class PlainSource
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    private record RecordTarget(string Name, int Value);

    private class NestedSource
    {
        public string Label { get; set; } = "";
        public PlainSource Child { get; set; } = new();
        public List<PlainSource> Items { get; set; } = new();
    }

    private class NestedTargetWithCtorlessMembers
    {
        public string Label { get; set; } = "";
        public RecordTarget? Child { get; set; }
        public List<RecordTarget> Items { get; set; } = new();
    }

    [Fact]
    public void StrictDefault_CtorlessTarget_Throws_NamingTypeAndBothRemedies()
    {
        var src = new PlainSource { Name = "x" };

        var ex = Assert.Throws<MappingException>(() => src.MapTo<RecordTarget>());

        Assert.Contains(nameof(RecordTarget), ex.Message);
        Assert.Contains("SimpleMapperOptions.ObjectConstruction", ex.Message);
        Assert.Contains("AllowUninitializedObjects()", ex.Message);
    }

    [Fact]
    public void StrictDefault_NestedCtorlessMember_Throws()
    {
        var src = new NestedSource { Child = new PlainSource { Name = "c" } };

        Assert.Throws<MappingException>(() => src.MapTo<NestedTargetWithCtorlessMembers>());
    }

    [Fact]
    public void GlobalOptIn_RestoresUninitializedFallback()
    {
        var original = SimpleMapperOptions.ObjectConstruction;
        try
        {
            SimpleMapperOptions.ObjectConstruction = ObjectConstructionMode.AllowUninitializedObjects;

            var result = new PlainSource { Name = "g", Value = 9 }.MapTo<RecordTarget>();

            Assert.Equal("g", result.Name);
            Assert.Equal(9, result.Value);
        }
        finally
        {
            SimpleMapperOptions.ObjectConstruction = original;
        }
    }

    [Fact]
    public void PerCallOptIn_CoversRoot_NestedObject_AndCollectionItems()
    {
        var src = new NestedSource
        {
            Label = "root",
            Child = new PlainSource { Name = "child", Value = 1 },
            Items =
            {
                new PlainSource { Name = "i0", Value = 10 },
                new PlainSource { Name = "i1", Value = 11 },
            },
        };

        var result = src.Map()
            .AllowUninitializedObjects()
            .To<NestedTargetWithCtorlessMembers>();

        Assert.Equal("root", result.Label);
        Assert.Equal(new RecordTarget("child", 1), result.Child);
        Assert.Equal(new RecordTarget("i0", 10), result.Items[0]);
        Assert.Equal(new RecordTarget("i1", 11), result.Items[1]);
    }

    [Fact]
    public void PerCallOptIn_DoesNotLeak_ToTheNextMapping()
    {
        var src = new PlainSource { Name = "x" };

        _ = src.Map().AllowUninitializedObjects().To<RecordTarget>();

        Assert.Throws<MappingException>(() => src.MapTo<RecordTarget>());
    }

    [Fact]
    public void PerCallOptIn_MapIntoExistingInstance_CreatesCtorlessMembers()
    {
        var src = new NestedSource
        {
            Label = "into",
            Child = new PlainSource { Name = "child", Value = 2 },
        };
        var destination = new NestedTargetWithCtorlessMembers { Label = "old" };

        src.Map()
            .AllowUninitializedObjects()
            .To(destination);

        Assert.Equal("into", destination.Label);
        Assert.Equal(new RecordTarget("child", 2), destination.Child);
    }

    [Fact]
    public void PerCallOptIn_IsThreadIsolated_UnderParallelLoad()
    {
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, 500, i =>
        {
            try
            {
                var src = new PlainSource { Name = $"n{i}", Value = i };

                if (i % 2 == 0)
                {
                    var ok = src.Map().AllowUninitializedObjects().To<RecordTarget>();
                    if (ok.Value != i)
                        errors.Add($"[{i}] opted-in mapping produced Value={ok.Value}");
                }
                else
                {
                    // No opt-in on this thread at this moment: the strict default
                    // must hold even while other threads are opted in.
                    try
                    {
                        _ = src.MapTo<RecordTarget>();
                        errors.Add($"[{i}] strict mapping unexpectedly succeeded");
                    }
                    catch (MappingException) { /* expected */ }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] Unexpected: {ex.Message}");
            }
        });

        Assert.Empty(errors);
    }
}
