using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Documents the instantiation contract: targets with a parameterless constructor
/// (any visibility) are created through it; targets without one require an explicit
/// opt-in (global option or per-call builder) to be created uninitialized — no
/// constructor runs and the mapper populates the members directly. The strict
/// default is covered by <see cref="ObjectConstructionModeTests"/>.
/// </summary>
[Collection("ObjectConstruction")]
public sealed class UninitializedFallbackTests
{
    private class PlainSource
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    private class TargetWithoutParameterlessCtor
    {
        public TargetWithoutParameterlessCtor(string name)
        {
            Name = name;
            ConstructorRan = true;
        }

        public string Name { get; set; } = "";
        public int Value { get; set; }

        // Read-only: never touched by the mapper, only by the constructor.
        public bool ConstructorRan { get; }
    }

    private class TargetWithProtectedCtor
    {
        protected TargetWithProtectedCtor()
        {
            ConstructorRan = true;
        }

        public string Name { get; set; } = "";
        public bool ConstructorRan { get; }
    }

    [Fact]
    public void TargetWithoutParameterlessCtor_IsMapped_ViaUninitializedOptIn()
    {
        var src = new PlainSource { Name = "mapped", Value = 7 };

        var result = src.Map()
            .AllowUninitializedObjects()
            .To<TargetWithoutParameterlessCtor>();

        Assert.Equal("mapped", result.Name);
        Assert.Equal(7, result.Value);
        Assert.False(result.ConstructorRan); // constructor was bypassed
    }

    [Fact]
    public void TargetWithProtectedCtor_UsesConstructor()
    {
        var src = new PlainSource { Name = "mapped" };

        var result = src.MapTo<TargetWithProtectedCtor>();

        Assert.Equal("mapped", result.Name);
        Assert.True(result.ConstructorRan); // protected parameterless ctor was invoked
    }

    [Fact]
    public void PositionalRecord_IsMapped_UnderOptIn()
    {
        var src = new PlainSource { Name = "rec", Value = 3 };

        var result = src.Map()
            .AllowUninitializedObjects()
            .To<RecordTarget>();

        Assert.Equal("rec", result.Name);
        Assert.Equal(3, result.Value);
    }

    private record RecordTarget(string Name, int Value);
}
