using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Pins the write-accessor contract: public properties are written regardless of
/// setter visibility (private, protected or init-only). This is intentional — the
/// same mechanism that fills init-only members of records also fills private
/// setters — and is documented in the README; changing it silently would break
/// immutable DTOs.
/// </summary>
public sealed class NonPublicSetterTests
{
    private class Source
    {
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
        public int Code { get; set; }
    }

    private class TargetWithPrivateSetter
    {
        public string Name { get; set; } = "";
        public decimal Amount { get; private set; }
        public int Code { get; protected set; }
    }

    private class TargetWithInitOnly
    {
        public string Name { get; init; } = "";
        public decimal Amount { get; init; }
    }

    [Fact]
    public void PrivateAndProtectedSetters_AreWritten()
    {
        var src = new Source { Name = "n", Amount = 99.9m, Code = 42 };

        var result = src.MapTo<TargetWithPrivateSetter>();

        Assert.Equal("n", result.Name);
        Assert.Equal(99.9m, result.Amount);
        Assert.Equal(42, result.Code);
    }

    [Fact]
    public void InitOnlySetters_AreWritten()
    {
        var src = new Source { Name = "n", Amount = 1.5m };

        var result = src.MapTo<TargetWithInitOnly>();

        Assert.Equal("n", result.Name);
        Assert.Equal(1.5m, result.Amount);
    }
}
