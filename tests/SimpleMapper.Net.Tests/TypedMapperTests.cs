using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class TypedMapperTests
{
    // ---- Test models ----

    private enum Priority { Low, Medium, High }

    private class SimpleSource
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Id { get; set; }
        public Priority Priority { get; set; }
        public bool Active { get; set; }
    }

    private class SimpleTarget
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Id { get; set; }
        public Priority Priority { get; set; }
        public bool Active { get; set; }
    }

    private class NullableSource
    {
        public int? NullableId { get; set; }
        public decimal? NullableAmount { get; set; }
        public Guid? NullableGuid { get; set; }
    }

    private class NullableTarget
    {
        public int? NullableId { get; set; }
        public decimal? NullableAmount { get; set; }
        public Guid? NullableGuid { get; set; }
    }

    private class InnerModel
    {
        public string Value { get; set; } = "";
        public int Code { get; set; }
    }

    private class InnerDto
    {
        public string Value { get; set; } = "";
        public int Code { get; set; }
    }

    private class NestedSource
    {
        public string Name { get; set; } = "";
        public InnerModel? Inner { get; set; }
    }

    private class NestedTarget
    {
        public string Name { get; set; } = "";
        public InnerDto? Inner { get; set; }
    }

    private class CollectionSource
    {
        public string Label { get; set; } = "";
        public List<InnerModel>? Items { get; set; }
        public List<string>? Tags { get; set; }
    }

    private class CollectionTarget
    {
        public string Label { get; set; } = "";
        public List<InnerDto>? Items { get; set; }
        public List<string>? Tags { get; set; }
    }

    private class DictSource
    {
        public Dictionary<string, string>? Config { get; set; }
    }

    private class DictTarget
    {
        public Dictionary<string, string>? Config { get; set; }
    }

    private class AllNullableSource
    {
        public string? Name { get; set; }
        public InnerModel? Inner { get; set; }
        public List<InnerModel>? Items { get; set; }
        public Dictionary<string, string>? Config { get; set; }
    }

    private class AllNullableTarget
    {
        public string? Name { get; set; }
        public InnerDto? Inner { get; set; }
        public List<InnerDto>? Items { get; set; }
        public Dictionary<string, string>? Config { get; set; }
    }

    // ---- Tests ----

    [Fact]
    public void SimpleProperties_MapsCorrectly()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var src = new SimpleSource
        {
            Name = "Alice",
            Age = 30,
            Total = 99.99m,
            CreatedAt = now,
            Id = id,
            Priority = Priority.High,
            Active = true
        };

        var result = src.MapTo<SimpleTarget>();

        Assert.Equal("Alice", result.Name);
        Assert.Equal(30, result.Age);
        Assert.Equal(99.99m, result.Total);
        Assert.Equal(now, result.CreatedAt);
        Assert.Equal(id, result.Id);
        Assert.Equal(Priority.High, result.Priority);
        Assert.True(result.Active);
    }

    [Fact]
    public void NullableValueTypes_WithValues_MapsCorrectly()
    {
        var guid = Guid.NewGuid();
        var src = new NullableSource
        {
            NullableId = 42,
            NullableAmount = 123.45m,
            NullableGuid = guid
        };

        var result = src.MapTo<NullableTarget>();

        Assert.Equal(42, result.NullableId);
        Assert.Equal(123.45m, result.NullableAmount);
        Assert.Equal(guid, result.NullableGuid);
    }

    [Fact]
    public void NullableValueTypes_WithNulls_MapsCorrectly()
    {
        var src = new NullableSource
        {
            NullableId = null,
            NullableAmount = null,
            NullableGuid = null
        };

        var result = src.MapTo<NullableTarget>();

        Assert.Null(result.NullableId);
        Assert.Null(result.NullableAmount);
        Assert.Null(result.NullableGuid);
    }

    [Fact]
    public void NestedComplexObject_MapsCorrectly()
    {
        var src = new NestedSource
        {
            Name = "Parent",
            Inner = new InnerModel { Value = "child", Code = 7 }
        };

        var result = src.MapTo<NestedTarget>();

        Assert.Equal("Parent", result.Name);
        Assert.NotNull(result.Inner);
        Assert.Equal("child", result.Inner!.Value);
        Assert.Equal(7, result.Inner.Code);
    }

    [Fact]
    public void NestedComplexObject_NullSource_DoesNotThrow()
    {
        var src = new NestedSource
        {
            Name = "Parent",
            Inner = null
        };

        var result = src.MapTo<NestedTarget>();

        Assert.Equal("Parent", result.Name);
        Assert.Null(result.Inner);
    }

    [Fact]
    public void Collection_ComplexItems_MapsCorrectly()
    {
        var src = new CollectionSource
        {
            Label = "batch",
            Items = new List<InnerModel>
            {
                new() { Value = "a", Code = 1 },
                new() { Value = "b", Code = 2 }
            }
        };

        var result = src.MapTo<CollectionTarget>();

        Assert.Equal("batch", result.Label);
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items!.Count);
        Assert.Equal("a", result.Items[0].Value);
        Assert.Equal(1, result.Items[0].Code);
        Assert.Equal("b", result.Items[1].Value);
        Assert.Equal(2, result.Items[1].Code);
    }

    [Fact]
    public void Collection_SimpleItems_MapsCorrectly()
    {
        var src = new CollectionSource
        {
            Label = "tags",
            Tags = new List<string> { "x", "y", "z" }
        };

        var result = src.MapTo<CollectionTarget>();

        Assert.Equal(new List<string> { "x", "y", "z" }, result.Tags);
    }

    [Fact]
    public void Collection_NullSource_DoesNotThrow()
    {
        var src = new CollectionSource
        {
            Label = "empty",
            Items = null,
            Tags = null
        };

        var result = src.MapTo<CollectionTarget>();

        Assert.Equal("empty", result.Label);
        Assert.Null(result.Items);
        Assert.Null(result.Tags);
    }

    [Fact]
    public void Dictionary_DirectReferenceCopy()
    {
        var dict = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" };
        var src = new DictSource { Config = dict };

        var result = src.MapTo<DictTarget>();

        Assert.NotNull(result.Config);
        Assert.Same(dict, result.Config);
    }

    [Fact]
    public void Dictionary_NullSource_DoesNotThrow()
    {
        var src = new DictSource { Config = null };

        var result = src.MapTo<DictTarget>();

        Assert.Null(result.Config);
    }

    [Fact]
    public void AllNullProperties_DoNotThrow()
    {
        var src = new AllNullableSource
        {
            Name = null,
            Inner = null,
            Items = null,
            Config = null
        };

        var result = src.MapTo<AllNullableTarget>();

        Assert.Null(result.Name);
        Assert.Null(result.Inner);
        Assert.Null(result.Items);
        Assert.Null(result.Config);
    }

    [Fact]
    public void ValueTypes_NotBoxed_PerformancePath()
    {
        // This test verifies the typed path works by mapping value-heavy objects
        var src = new SimpleSource
        {
            Name = "Bench",
            Age = 100,
            Total = 999.99m,
            CreatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            Priority = Priority.Medium,
            Active = false
        };

        // Map multiple times to exercise the cache
        for (int i = 0; i < 100; i++)
        {
            var result = src.MapTo<SimpleTarget>();
            Assert.Equal(100, result.Age);
            Assert.Equal(999.99m, result.Total);
        }
    }
}
