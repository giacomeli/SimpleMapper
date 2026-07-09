using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace SimpleMapper.Net;

internal sealed record CompiledPair(
    Func<object> CreateTarget,
    Action<object, object> Map);

internal static class TypedMapperCache
{
    private static readonly ConcurrentDictionary<(Type, Type), CompiledPair> Cache = new();

    [RequiresDynamicCode(SimpleMapperExtensions.AotWarning)]
    [RequiresUnreferencedCode(SimpleMapperExtensions.TrimWarning)]
    public static CompiledPair GetOrBuild(Type srcType, Type tgtType)
    {
        return Cache.GetOrAdd((srcType, tgtType), key =>
        {
            var factory = BuildFactory(key.Item2);
            var mapper = TypedPlanBuilder.Build(key.Item1, key.Item2);
            return new CompiledPair(factory, mapper);
        });
    }

    // Test/benchmark hook: drops the compiled pairs so cold-start cost can be
    // measured and tests can toggle global options without stale factories.
    internal static void Clear() => Cache.Clear();

    // Shared by the typed and the plan-based engines so both resolve constructors
    // identically (public or non-public parameterless ctor, otherwise a factory
    // gated by ObjectConstructionMode) and neither invokes a constructor as a side
    // effect of plan building.
    [RequiresDynamicCode(SimpleMapperExtensions.AotWarning)]
    [RequiresUnreferencedCode(SimpleMapperExtensions.TrimWarning)]
    internal static Func<object> BuildFactory(Type t)
    {
        var ctor = t.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, Type.EmptyTypes, null);
        if (ctor != null)
        {
            var body = Expression.Convert(Expression.New(ctor), typeof(object));
            return Expression.Lambda<Func<object>>(body).Compile();
        }
        // No parameterless constructor. Whether the instance may be created without
        // running a constructor is decided at invocation time — not at build time —
        // because the factory is cached per type pair while the permission can come
        // from the global option or from a per-call opt-in.
        return () =>
        {
            if (SimpleMapperOptions.ObjectConstruction == ObjectConstructionMode.AllowUninitializedObjects
                || MapperEngine.AllowUninitializedObjectsAmbient)
                return RuntimeHelpers.GetUninitializedObject(t);

            throw new MappingException(
                $"Cannot create an instance of '{t.Name}': it has no parameterless " +
                "constructor (public or non-public), and SimpleMapper.Net does not bypass " +
                "constructors by default because uninitialized instances skip constructor " +
                "logic and field initializers. Either add a parameterless constructor (it " +
                "can be private or protected), or opt in explicitly with " +
                "SimpleMapperOptions.ObjectConstruction = ObjectConstructionMode.AllowUninitializedObjects " +
                "(global) or source.Map().AllowUninitializedObjects() (this call only).");
        };
    }
}
