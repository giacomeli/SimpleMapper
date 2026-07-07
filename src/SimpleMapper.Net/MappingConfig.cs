using System.Collections.Generic;

namespace SimpleMapper.Net;

/// <summary>
/// Immutable mapping configuration produced by <see cref="MapperBuilder{TSource}"/>.
/// An empty config selects the compiled fast path; ignores, renames and nested
/// (child) configs select the plan-based paths.
/// </summary>
public sealed record MappingConfig
{
    /// <summary>The shared empty configuration used by zero-config mappings.</summary>
    public static readonly MappingConfig Default = new();

    /// <summary>Property renames, keyed by target property name.</summary>
    public IReadOnlyDictionary<string, string> PropertyMappings { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Target property names skipped during mapping.</summary>
    public IReadOnlySet<string> IgnoredProperties { get; init; }
        = new HashSet<string>();

    /// <summary>Sub-configs for nested properties; the "*" key targets collection items.</summary>
    public IReadOnlyDictionary<string, MappingConfig> ChildConfigs { get; init; }
        = new Dictionary<string, MappingConfig>();

    /// <summary>Prints the mapping tree to the console (diagnostic slow path).</summary>
    public bool DebugLogging { get; init; }

    /// <summary>True when the config carries no overrides and the fast path can be used.</summary>
    public bool IsEmpty => IgnoredProperties.Count == 0
        && PropertyMappings.Count == 0
        && ChildConfigs.Count == 0;

    /// <summary>
    /// Returns the sub-config for a nested complex property.
    /// Falls back to <see cref="Default"/> (empty) when no specific config exists.
    /// </summary>
    public MappingConfig ForChild(string propertyName)
    {
        if (ChildConfigs.Count == 0) return Default;
        return ChildConfigs.TryGetValue(propertyName, out var child) ? child : Default;
    }

    /// <summary>
    /// Returns the sub-config for collection items (the "*" marker produced by Each()).
    /// Falls back to <see cref="Default"/> (empty) when no specific config exists.
    /// </summary>
    public MappingConfig ForCollectionItem()
    {
        if (ChildConfigs.Count == 0) return Default;
        return ChildConfigs.TryGetValue("*", out var child) ? child : Default;
    }
}
