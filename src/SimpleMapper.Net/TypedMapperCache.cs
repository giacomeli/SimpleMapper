using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleMapper.Net;

internal sealed record CompiledPair(
    Func<object> CreateTarget,
    Action<object, object> Map);

internal static class TypedMapperCache
{
    private static readonly ConcurrentDictionary<(Type, Type), CompiledPair> Cache = new();

    public static CompiledPair GetOrBuild(Type srcType, Type tgtType)
    {
        return Cache.GetOrAdd((srcType, tgtType), key =>
        {
            var factory = BuildFactory(key.Item2);
            var mapper = TypedPlanBuilder.Build(key.Item1, key.Item2);
            return new CompiledPair(factory, mapper);
        });
    }

    // Shared by the typed and the plan-based engines so both resolve constructors
    // identically (public or non-public parameterless ctor, otherwise uninitialized
    // instance) and neither invokes a constructor as a side effect of plan building.
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
        // No parameterless constructor: create the instance without invoking any
        // constructor and let the mapper populate the members directly.
        return () => RuntimeHelpers.GetUninitializedObject(t);
    }
}
