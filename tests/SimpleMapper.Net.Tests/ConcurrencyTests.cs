using System.Collections.Concurrent;
using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public class ConcurrencyTests
{
    private record Source(
        string Name, int Age, long Score,
        Guid? ExternalId, int? OptionalRank, string InternalTag);

    private record Target(
        string Name, int Age, long Score,
        Guid? ExternalId, int? OptionalRank);

    private static Source MakeSource(int i) => new(
        Name: $"User_{i}",
        Age: 20 + (i % 50),
        Score: 100_000L + i,
        ExternalId: i % 3 == 0 ? null : Guid.NewGuid(),
        OptionalRank: i % 5 == 0 ? null : i,
        InternalTag: $"tag_{i}");

    [Fact]
    public void MapTo_ConcurrentMappingsWithDifferentConfigs_NeverCrossContaminatesConfig()
    {
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, 2000, i =>
        {
            try
            {
                var src = MakeSource(i);

                if (i % 2 == 0)
                {
                    var result = src.Map<Source>()
                        .Ignore(nameof(Target.Name))
                        .To<Target>();

                    if (result.Name != null)
                        errors.Add($"[{i}] Even: expected Name=null, got '{result.Name}'");
                }
                else
                {
                    var result = src.Map<Source>()
                        .Map(nameof(Source.InternalTag), nameof(Target.Name))
                        .To<Target>();

                    if (result.Name != src.InternalTag)
                        errors.Add($"[{i}] Odd: expected Name='{src.InternalTag}', got '{result.Name}'");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] Exception: {ex.Message}");
            }
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void Cache_ConcurrentFirstAccessToSameType_NeverThrows()
    {
        var exceptions = new ConcurrentBag<string>();

        Parallel.For(0, 200, i =>
        {
            try
            {
                var src = MakeSource(i);
                var result = src.MapTo<Target>();

                if (result.Age != src.Age)
                    exceptions.Add($"[{i}] Age mismatch: expected {src.Age}, got {result.Age}");
            }
            catch (Exception ex)
            {
                exceptions.Add($"[{i}] Exception: {ex.Message}");
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void MapTo_NullableProperties_MappedCorrectly()
    {
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, 500, i =>
        {
            try
            {
                var src = MakeSource(i);
                var result = src.MapTo<Target>();

                bool expectNullExternalId = i % 3 == 0;
                if (expectNullExternalId && result.ExternalId != null)
                    errors.Add($"[{i}] ExternalId should be null, got {result.ExternalId}");
                if (!expectNullExternalId && result.ExternalId != src.ExternalId)
                    errors.Add($"[{i}] ExternalId mismatch: expected {src.ExternalId}, got {result.ExternalId}");

                bool expectNullRank = i % 5 == 0;
                if (expectNullRank && result.OptionalRank != null)
                    errors.Add($"[{i}] OptionalRank should be null, got {result.OptionalRank}");
                if (!expectNullRank && result.OptionalRank != src.OptionalRank)
                    errors.Add($"[{i}] OptionalRank mismatch: expected {src.OptionalRank}, got {result.OptionalRank}");
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] Exception: {ex.Message}");
            }
        });

        Assert.Empty(errors);
    }

    private record SourceInt(int Value);
    private record TargetLong(long Value);

    [Fact]
    public void MapTo_NumericCoercion_IntToLong_NeverThrows()
    {
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, 500, i =>
        {
            try
            {
                var src = new SourceInt(i);
                var result = src.MapTo<TargetLong>();

                if (result.Value != (long)i)
                    errors.Add($"[{i}] Expected {(long)i}, got {result.Value}");
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] Exception: {ex.Message}");
            }
        });

        Assert.Empty(errors);
    }

    private record SourceWithNulls(string? NullableName, string NonNullName);
    private record TargetWithNulls(string? NullableName, string NonNullName);

    [Fact]
    public void MapTo_SkipIfNull_NullableReceivesNull_NonNullableSkips()
    {
        var src = new SourceWithNulls(NullableName: null, NonNullName: "set");
        var result = src.MapTo<TargetWithNulls>();

        Assert.Null(result.NullableName);
        Assert.Equal("set", result.NonNullName);
    }

    [Fact]
    public void StressTest_AllScenariosParallel_NoErrorsOrExceptions()
    {
        var errors = new ConcurrentBag<string>();

        Parallel.For(0, 5000, i =>
        {
            try
            {
                var src = MakeSource(i);

                // Scenario 1: simple MapTo
                var simple = src.MapTo<Target>();
                if (simple.Age != src.Age)
                    errors.Add($"[{i}] Simple: Age mismatch");

                // Scenario 2: Ignore
                var ignored = src.Map<Source>()
                    .Ignore(nameof(Target.Age))
                    .To<Target>();
                if (ignored.Age != default)
                    errors.Add($"[{i}] Ignored: Age should be default(0), got {ignored.Age}");

                // Scenario 3: Remap
                var remapped = src.Map<Source>()
                    .Map(nameof(Source.InternalTag), nameof(Target.Name))
                    .To<Target>();
                if (remapped.Name != src.InternalTag)
                    errors.Add($"[{i}] Remapped: Name should be '{src.InternalTag}', got '{remapped.Name}'");
            }
            catch (Exception ex)
            {
                errors.Add($"[{i}] Exception: {ex.Message}");
            }
        });

        Assert.Empty(errors);
    }
}
