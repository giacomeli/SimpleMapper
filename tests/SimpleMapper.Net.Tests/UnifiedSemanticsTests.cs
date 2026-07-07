using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Pins the unified semantics between the typed fast path (empty config) and the
/// plan-based path (Ignore/rename configs): both must deep-map, include public
/// fields, and resolve constructors the same way.
/// </summary>
public sealed class UnifiedSemanticsTests
{
    private class Media { public string Url { get; set; } = ""; }

    private class Source
    {
        public string Name { get; set; } = "";
        public Media? Media { get; set; }
        public List<Media>? Tags { get; set; }
    }

    private class Target
    {
        public string Name { get; set; } = "";
        public Media? Media { get; set; }
        public List<Media>? Tags { get; set; }
    }

    [Fact]
    public void SameTypeComplexProperty_IsDeepCopied_OnFastPath()
    {
        var src = new Source { Media = new Media { Url = "u" } };

        var dto = src.MapTo<Target>();

        Assert.NotSame(src.Media, dto.Media);
        Assert.Equal("u", dto.Media!.Url);
    }

    [Fact]
    public void SameTypeComplexProperty_IsDeepCopied_OnPlanPath()
    {
        var src = new Source { Name = "n", Media = new Media { Url = "u" } };

        var dto = src.Map().Ignore(nameof(Source.Name)).To<Target>();

        Assert.NotSame(src.Media, dto.Media);
        Assert.Equal("u", dto.Media!.Url);
        Assert.Equal("", dto.Name);
    }

    [Fact]
    public void SameTypeCollectionItems_AreDeepCopied_OnBothPaths()
    {
        var src = new Source { Name = "n", Tags = new List<Media> { new() { Url = "t" } } };

        var fast = src.MapTo<Target>();
        var plan = src.Map().Ignore(nameof(Source.Name)).To<Target>();

        Assert.NotSame(src.Tags![0], fast.Tags![0]);
        Assert.NotSame(src.Tags[0], plan.Tags![0]);
        Assert.Equal("t", fast.Tags[0].Url);
        Assert.Equal("t", plan.Tags[0].Url);
    }

    private class FieldSource
    {
        public string Name = "";
        public int Age { get; set; }
    }

    private class FieldTarget
    {
        public string Name = "";
        public int Age { get; set; }
    }

    [Fact]
    public void PublicFields_AreMapped_OnFastPath()
    {
        var src = new FieldSource { Name = "field", Age = 7 };

        var dto = src.MapTo<FieldTarget>();

        Assert.Equal("field", dto.Name);
        Assert.Equal(7, dto.Age);
    }

    [Fact]
    public void PublicFields_AreMapped_OnPlanPath()
    {
        var src = new FieldSource { Name = "field", Age = 7 };

        var dto = src.Map().Ignore(nameof(FieldSource.Age)).To<FieldTarget>();

        Assert.Equal("field", dto.Name);
        Assert.Equal(0, dto.Age);
    }

    private class ObjectHolderSource { public string? Name { get; set; } public object? Payload { get; set; } }
    private class ObjectHolderTarget { public string? Name { get; set; } public object? Payload { get; set; } }

    [Fact]
    public void ObjectTypedProperty_KeepsReference_OnBothPaths()
    {
        var payload = new Media { Url = "p" };
        var src = new ObjectHolderSource { Name = "n", Payload = payload };

        var fast = src.MapTo<ObjectHolderTarget>();
        var plan = src.Map().Ignore(nameof(ObjectHolderSource.Name)).To<ObjectHolderTarget>();

        Assert.Same(payload, fast.Payload);
        Assert.Same(payload, plan.Payload);
    }

    private class DelegateSource { public string? Name { get; set; } public Action? Callback { get; set; } }
    private class DelegateTarget { public string? Name { get; set; } public Action? Callback { get; set; } }

    [Fact]
    public void DelegateProperty_KeepsReference_OnBothPaths()
    {
        Action callback = () => { };
        var src = new DelegateSource { Name = "n", Callback = callback };

        var fast = src.MapTo<DelegateTarget>();
        var plan = src.Map().Ignore(nameof(DelegateSource.Name)).To<DelegateTarget>();

        Assert.Same(callback, fast.Callback);
        Assert.Same(callback, plan.Callback);
    }

    private class CountingCtorSource { public string? Name { get; set; } }

    private class CountingCtorTarget
    {
        public static int Instances;
        public CountingCtorTarget() => Instances++;
        public string? Name { get; set; }
    }

    [Fact]
    public void PlanBuild_DoesNotInvokeConstructorAsSideEffect()
    {
        CountingCtorTarget.Instances = 0;
        var src = new CountingCtorSource { Name = "n" };

        // Plan path (Ignore config): plan building must not test-instantiate the target.
        var dto = src.Map().Ignore("Missing").To<CountingCtorTarget>();

        Assert.Equal("n", dto.Name);
        Assert.Equal(1, CountingCtorTarget.Instances);
    }

    private class ProtectedCtorSource { public string? Name { get; set; } }

    private class ProtectedCtorTarget
    {
        protected ProtectedCtorTarget() => Marker = "ctor-ran";
        public string? Name { get; set; }
        public string? Marker { get; set; }
    }

    [Fact]
    public void ProtectedParameterlessCtor_IsUsed_OnPlanPath()
    {
        var src = new ProtectedCtorSource { Name = "n" };

        var dto = src.Map().Ignore("Missing").To<ProtectedCtorTarget>();

        Assert.Equal("n", dto.Name);
        Assert.Equal("ctor-ran", dto.Marker);
    }

    private class ArraySource { public string? Name { get; set; } public Media[]? Items { get; set; } }
    private class ArrayTarget { public string? Name { get; set; } public Media[]? Items { get; set; } }

    [Fact]
    public void ArrayProperty_IsMapped_OnBothPaths()
    {
        var src = new ArraySource { Name = "n", Items = new[] { new Media { Url = "a" } } };

        var fast = src.MapTo<ArrayTarget>();
        var plan = src.Map().Ignore(nameof(ArraySource.Name)).To<ArrayTarget>();

        Assert.Single(fast.Items!);
        Assert.Single(plan.Items!);
        Assert.Equal("a", fast.Items![0].Url);
        Assert.Equal("a", plan.Items![0].Url);
        Assert.NotSame(src.Items[0], fast.Items[0]);
        Assert.NotSame(src.Items[0], plan.Items[0]);
    }

    private class RenameSource { public string? Handle { get; set; } }
    private class RenameTarget { public string? DisplayName { get; set; } }

    [Fact]
    public void Rename_Works_WhenTargetNameDoesNotExistOnSource()
    {
        var src = new RenameSource { Handle = "julian" };

        var dto = src.Map().Map("Handle", "DisplayName").To<RenameTarget>();

        Assert.Equal("julian", dto.DisplayName);
    }
}
