using System.Globalization;
using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Pins the fail-loud contract: unsupported mappings throw with the offending
/// member and both types in the message instead of silently producing wrong data
/// or leaking a raw expression-tree error.
/// </summary>
public sealed class DiagnosticsTests
{
    private struct PointDto { public int X { get; set; } public int Y { get; set; } }
    private class PointSource { public int X { get; set; } public int Y { get; set; } }

    [Fact]
    public void StructTarget_ThrowsNotSupported_InsteadOfZeroedData()
    {
        var src = new PointSource { X = 1, Y = 2 };

        var ex = Assert.Throws<NotSupportedException>(() => src.MapTo<PointDto>());

        Assert.Contains(nameof(PointDto), ex.Message);
    }

    private class NumSource { public string Value { get; set; } = ""; }
    private class NumTarget { public double Value { get; set; } }

    [Fact]
    public void IncompatibleSimpleTypes_ThrowWithMemberContext()
    {
        var src = new NumSource { Value = "1.5" };

        var ex = Assert.Throws<MappingException>(() => src.MapTo<NumTarget>());

        Assert.Contains("Value", ex.Message);
        Assert.Contains(nameof(NumSource), ex.Message);
        Assert.Contains(nameof(NumTarget), ex.Message);
    }

    private class IntSource { public int Id { get; set; } }
    private class StringTarget { public string? Id { get; set; } }

    [Fact]
    public void IntToString_ThrowsWithMemberContext()
    {
        var ex = Assert.Throws<MappingException>(() => new IntSource { Id = 42 }.MapTo<StringTarget>());

        Assert.Contains("Id", ex.Message);
    }

    private class SetSource { public List<string>? Items { get; set; } }
    private class SetTarget { public HashSet<string>? Items { get; set; } }

    [Fact]
    public void UnsupportedCollectionTarget_ThrowsAtPlanBuild_WithMemberContext()
    {
        var src = new SetSource { Items = new List<string> { "a" } };

        var ex = Assert.Throws<MappingException>(() => src.MapTo<SetTarget>());

        Assert.Contains("Items", ex.Message);
        Assert.Contains("collection", ex.Message);
    }

    private class NonGenericSource { public System.Collections.ArrayList? Items { get; set; } }
    private class NonGenericTarget { public System.Collections.ArrayList? Items { get; set; } }

    [Fact]
    public void NonGenericCollectionTarget_ThrowsWithMemberContext()
    {
        var src = new NonGenericSource { Items = new System.Collections.ArrayList { "a" } };

        var ex = Assert.Throws<MappingException>(() => src.MapTo<NonGenericTarget>());

        Assert.Contains("Items", ex.Message);
    }

    private struct Coordinates { public int Lat { get; set; } public int Lon { get; set; } }
    private struct OtherCoordinates { public int Lat { get; set; } public int Lon { get; set; } }

    private class StructPropSource { public string? Name { get; set; } public Coordinates Location { get; set; } }
    private class StructPropTargetSame { public string? Name { get; set; } public Coordinates Location { get; set; } }
    private class StructPropTargetOther { public string? Name { get; set; } public OtherCoordinates Location { get; set; } }

    [Fact]
    public void SameTypeStructProperty_IsValueCopied_OnBothPaths()
    {
        var src = new StructPropSource { Name = "n", Location = new Coordinates { Lat = 1, Lon = 2 } };

        var fast = src.MapTo<StructPropTargetSame>();
        var plan = src.Map().Ignore(nameof(StructPropSource.Name)).To<StructPropTargetSame>();

        Assert.Equal(1, fast.Location.Lat);
        Assert.Equal(2, plan.Location.Lon);
    }

    [Fact]
    public void DifferentTypeStructProperty_ThrowsWithMemberContext()
    {
        var src = new StructPropSource { Location = new Coordinates { Lat = 1, Lon = 2 } };

        var ex = Assert.Throws<MappingException>(() => src.MapTo<StructPropTargetOther>());

        Assert.Contains("Location", ex.Message);
        Assert.Contains("struct", ex.Message);
    }

    private class RawSource { public string? Raw { get; set; } }
    private class AmountTarget { public double Amount { get; set; } }

    [Fact]
    public void RuntimeCoercion_UsesInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            // Under pt-BR, "." is a group separator: culture-sensitive parsing would
            // turn "1.5" into 15. The dynamic path must coerce with the invariant culture.
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            var dto = new RawSource { Raw = "1.5" }.Map()
                .Map("Raw", "Amount")
                .To<AmountTarget>();

            Assert.Equal(1.5, dto.Amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
