using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;

namespace SimpleMapper.Net;

/// <summary>
/// Fluent builder for per-call mapping configuration: flat and deep property
/// ignores/renames, plus debug logging of the mapping tree.
/// </summary>
public sealed class MapperBuilder<TSource>
{
    private readonly TSource _source;
    private readonly Dictionary<string, string> _mappings = new();
    private readonly HashSet<string> _ignored = new();
    private readonly List<string[]> _ignoredPaths = new();
    private readonly List<(string[] Source, string[] Target)> _mappingPaths = new();
    private bool _debug;
    private TextWriter? _debugWriter;

    internal MapperBuilder(TSource source) => _source = source;

    /// <summary>
    /// Maps a source property to a target property with a different name (root level).
    /// </summary>
    public MapperBuilder<TSource> Map(string sourceProp, string targetProp)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceProp);
        ArgumentException.ThrowIfNullOrEmpty(targetProp);
        _mappings[targetProp] = sourceProp;
        return this;
    }

    /// <summary>
    /// Maps a deep source property to a deep target property with a different name.
    /// Both paths must have the same depth — only the leaf may differ.
    /// </summary>
    public MapperBuilder<TSource> Map(
        Expression<Func<TSource, object>> sourcePath,
        Expression<Func<TSource, object>> targetPath)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(targetPath);

        var src = PathExtractor.Extract(sourcePath);
        var tgt = PathExtractor.Extract(targetPath);

        if (src.Length != tgt.Length)
            throw new ArgumentException(
                "Source and target paths must have the same depth for deep property mapping.");

        if (src.Length == 1 && tgt.Length == 1)
            _mappings[tgt[0]] = src[0];
        else
            _mappingPaths.Add((src, tgt));

        return this;
    }

    /// <summary>Ignores a target property during mapping (root level).</summary>
    public MapperBuilder<TSource> Ignore(string targetProp)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetProp);
        _ignored.Add(targetProp);
        return this;
    }

    /// <summary>
    /// Ignores a deep target property during mapping.
    /// Use Each() to navigate into collection items.
    /// </summary>
    public MapperBuilder<TSource> Ignore(Expression<Func<TSource, object>> path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var segments = PathExtractor.Extract(path);

        if (segments.Length == 1)
            _ignored.Add(segments[0]);
        else
            _ignoredPaths.Add(segments);

        return this;
    }

    /// <summary>Prints the mapping tree to the console. Diagnostic use only (slow path).</summary>
    public MapperBuilder<TSource> WithDebugLogging()
    {
        _debug = true;
        return this;
    }

    /// <summary>
    /// Writes the mapping tree to the given writer (plain text, no colors).
    /// Diagnostic use only (slow path); useful in tests and server logs.
    /// </summary>
    public MapperBuilder<TSource> WithDebugLogging(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _debug = true;
        _debugWriter = writer;
        return this;
    }

    /// <summary>Executes the configured mapping and returns the target instance.</summary>
    [RequiresDynamicCode(SimpleMapperExtensions.AotWarning)]
    [RequiresUnreferencedCode(SimpleMapperExtensions.TrimWarning)]
    public TTarget To<TTarget>()
    {
        if (_source is null)
            throw new InvalidOperationException("Source cannot be null.");

        return MapperEngine.Execute<TTarget>(_source, BuildConfig());
    }

    /// <summary>Executes the configured mapping to a runtime-resolved target type.</summary>
    [RequiresDynamicCode(SimpleMapperExtensions.AotWarning)]
    [RequiresUnreferencedCode(SimpleMapperExtensions.TrimWarning)]
    public object To(Type targetType)
    {
        if (_source is null)
            throw new InvalidOperationException("Source cannot be null.");

        return MapperEngine.Execute(_source, targetType, BuildConfig());
    }

    /// <summary>
    /// Executes the configured mapping onto an existing instance and returns it.
    /// Subtype rules do not apply — the target already exists.
    /// </summary>
    [RequiresDynamicCode(SimpleMapperExtensions.AotWarning)]
    [RequiresUnreferencedCode(SimpleMapperExtensions.TrimWarning)]
    public TTarget To<TTarget>(TTarget destination) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (_source is null)
            throw new InvalidOperationException("Source cannot be null.");

        MapperEngine.ExecuteInto(_source, destination, BuildConfig());
        return destination;
    }

    private MappingConfig BuildConfig()
    {
        var root = new ConfigNode();

        foreach (var prop in _ignored)
            root.Ignored.Add(prop);

        foreach (var (tgt, src) in _mappings)
            root.Mappings[tgt] = src;

        foreach (var path in _ignoredPaths)
        {
            var node = root;
            for (int i = 0; i < path.Length - 1; i++)
                node = node.GetOrAddChild(path[i]);
            node.Ignored.Add(path[^1]);
        }

        foreach (var (srcPath, tgtPath) in _mappingPaths)
        {
            var node = root;
            for (int i = 0; i < tgtPath.Length - 1; i++)
                node = node.GetOrAddChild(tgtPath[i]);
            node.Mappings[tgtPath[^1]] = srcPath[^1];
        }

        return root.ToConfig(_debug) with { DebugWriter = _debugWriter };
    }

    private sealed class ConfigNode
    {
        public HashSet<string> Ignored { get; } = new();
        public Dictionary<string, string> Mappings { get; } = new();
        public Dictionary<string, ConfigNode> Children { get; } = new();

        public ConfigNode GetOrAddChild(string key)
        {
            if (!Children.TryGetValue(key, out var child))
            {
                child = new ConfigNode();
                Children[key] = child;
            }
            return child;
        }

        public MappingConfig ToConfig(bool debug)
        {
            var childConfigs = new Dictionary<string, MappingConfig>();
            foreach (var (k, v) in Children)
                childConfigs[k] = v.ToConfig(false);

            return new MappingConfig
            {
                IgnoredProperties = Ignored,
                PropertyMappings = Mappings,
                ChildConfigs = childConfigs,
                DebugLogging = debug,
            };
        }
    }
}
