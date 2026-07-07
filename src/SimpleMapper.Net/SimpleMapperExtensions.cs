using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleMapper.Net;

/// <summary>
/// Entry point of SimpleMapper.Net: extension methods for direct, convention-based
/// object-to-object mapping and for starting a configurable fluent mapping.
/// </summary>
public static class SimpleMapperExtensions
{
    internal const string AotWarning =
        "SimpleMapper.Net builds mapping code at runtime with reflection and compiled " +
        "expression trees, which is incompatible with NativeAOT.";

    internal const string TrimWarning =
        "SimpleMapper.Net discovers members via reflection at runtime; trimming may " +
        "remove mapped properties or constructors.";

    /// <summary>Maps by name convention. Zero configuration.</summary>
    [RequiresDynamicCode(AotWarning)]
    [RequiresUnreferencedCode(TrimWarning)]
    [return: NotNullIfNotNull(nameof(source))]
    public static TTarget? MapTo<TTarget>(this object? source)
    {
        if (source is null) return default;
        return MapperEngine.Execute<TTarget>(source, MappingConfig.Default)!;
    }

    /// <summary>Maps to a runtime-resolved target type. Zero configuration.</summary>
    [RequiresDynamicCode(AotWarning)]
    [RequiresUnreferencedCode(TrimWarning)]
    [return: NotNullIfNotNull(nameof(source))]
    public static object? MapTo(this object? source, Type targetType)
    {
        if (source is null) return null;
        return MapperEngine.Execute(source, targetType, MappingConfig.Default);
    }

    /// <summary>
    /// Maps by name convention onto an existing instance and returns it. Target
    /// properties without a matching source member keep their current values.
    /// A null source leaves the destination untouched.
    /// </summary>
    [RequiresDynamicCode(AotWarning)]
    [RequiresUnreferencedCode(TrimWarning)]
    public static TTarget MapTo<TTarget>(this object? source, TTarget destination)
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (source is null) return destination;
        MapperEngine.ExecuteInto(source, destination, MappingConfig.Default);
        return destination;
    }

    /// <summary>Starts a configurable mapping via the fluent builder.</summary>
    public static MapperBuilder<TSource> Map<TSource>(this TSource source)
        => new(source);

    /// <summary>
    /// Registers a subtype rule: when the source satisfies the discriminator,
    /// the mapping creates <paramref name="targetType"/> instead of the declared type.
    /// </summary>
    /// <remarks>
    /// WIP / experimental: rules live in a global static registry and must be
    /// registered before the first mapping of the affected types. See the
    /// "Polymorphic mapping" section of the documentation for details.
    /// </remarks>
    [Experimental("SMEXP001")]
    public static void RegisterSubtype<TSource>(Func<object, bool> discriminator, Type targetType)
        => MapperEngine.RegisterSubtype<TSource>(discriminator, targetType);

    /// <summary>Maps every item of a collection to the target type.</summary>
    [RequiresDynamicCode(AotWarning)]
    [RequiresUnreferencedCode(TrimWarning)]
    public static List<TTarget> MapListTo<TTarget>(this IEnumerable source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var result = new List<TTarget>();
        foreach (var item in source)
        {
            if (item is null) continue;
            result.Add(MapperEngine.Execute<TTarget>(item, MappingConfig.Default));
        }
        return result;
    }
}
