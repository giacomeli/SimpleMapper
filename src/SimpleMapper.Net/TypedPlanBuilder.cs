using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMapper.Net;

internal static class TypedPlanBuilder
{
    public static Action<object, object> Build(Type srcType, Type tgtType)
    {
        var srcParam = Expression.Parameter(typeof(object), "srcObj");
        var tgtParam = Expression.Parameter(typeof(object), "tgtObj");

        var srcTyped = Expression.Variable(srcType, "src");
        var tgtTyped = Expression.Variable(tgtType, "tgt");

        var body = new List<Expression>
        {
            Expression.Assign(srcTyped, Expression.Convert(srcParam, srcType)),
            Expression.Assign(tgtTyped, Expression.Convert(tgtParam, tgtType))
        };

        var srcProps = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToDictionary(p => p.Name);

        var tgtProps = tgtType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToArray();

        foreach (var tgtProp in tgtProps)
        {
            if (!srcProps.TryGetValue(tgtProp.Name, out var srcProp))
                continue;

            var assignment = BuildPropertyAssignment(srcTyped, tgtTyped, srcProp, tgtProp);
            if (assignment != null)
                body.Add(assignment);
        }

        var block = Expression.Block(
            new[] { srcTyped, tgtTyped },
            body);

        return Expression.Lambda<Action<object, object>>(block, srcParam, tgtParam).Compile();
    }

    private static Expression? BuildPropertyAssignment(
        Expression srcTyped, Expression tgtTyped,
        PropertyInfo srcProp, PropertyInfo tgtProp)
    {
        var srcAccess = Expression.Property(srcTyped, srcProp);
        var tgtAccess = Expression.Property(tgtTyped, tgtProp);
        var srcPropType = srcProp.PropertyType;
        var tgtPropType = tgtProp.PropertyType;

        // Simple types (primitives, string, decimal, DateTime, Guid, enums, etc.)
        if (MapperEngine.IsSimple(tgtPropType) && MapperEngine.IsSimple(srcPropType))
        {
            return BuildSimpleAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
        }

        // Dictionary: direct reference copy
        if (typeof(IDictionary).IsAssignableFrom(tgtPropType))
        {
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
        }

        // Collection (List<T>, IEnumerable but not string)
        if (typeof(IEnumerable).IsAssignableFrom(tgtPropType) && tgtPropType != typeof(string))
        {
            return BuildCollectionAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
        }

        // Complex nested object
        return BuildComplexAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
    }

    private static Expression BuildSimpleAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcPropType, Type tgtPropType)
    {
        // Same type: direct assignment, no boxing for value types
        if (srcPropType == tgtPropType)
        {
            return Expression.Assign(tgtAccess, srcAccess);
        }

        // Nullable<T> source -> T target or Nullable<T> target
        var srcUnderlying = Nullable.GetUnderlyingType(srcPropType);
        var tgtUnderlying = Nullable.GetUnderlyingType(tgtPropType);

        // Both are the same underlying type (e.g. int? -> int? or int -> int?)
        var srcCore = srcUnderlying ?? srcPropType;
        var tgtCore = tgtUnderlying ?? tgtPropType;

        if (srcCore == tgtCore)
        {
            // int -> int? or int? -> int etc.
            return Expression.Assign(tgtAccess, Expression.Convert(srcAccess, tgtPropType));
        }

        // Numeric coercion (int -> long, float -> double, etc.)
        return Expression.Assign(tgtAccess, Expression.Convert(srcAccess, tgtPropType));
    }

    private static Expression BuildReferenceAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcPropType, Type tgtPropType)
    {
        // For reference types, null-check the source
        if (!srcPropType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcPropType)),
                Expression.Assign(tgtAccess,
                    tgtPropType.IsAssignableFrom(srcPropType)
                        ? srcAccess
                        : Expression.Convert(srcAccess, tgtPropType)));
        }

        return Expression.Assign(tgtAccess,
            tgtPropType.IsAssignableFrom(srcPropType)
                ? srcAccess
                : Expression.Convert(srcAccess, tgtPropType));
    }

    private static Expression BuildCollectionAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcPropType, Type tgtPropType)
    {
        var srcItemType = GetCollectionItemType(srcPropType);
        var tgtItemType = GetCollectionItemType(tgtPropType);

        if (srcItemType == null || tgtItemType == null)
        {
            // Fall back to direct reference copy
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
        }

        // Build call to MapCollectionTyped<TSrc, TTgt>(srcAccess, targetIsArray, itemIsSimple)
        var helperMethod = typeof(TypedPlanBuilder)
            .GetMethod(nameof(MapCollectionTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(srcItemType, tgtItemType);

        bool targetIsArray = tgtPropType.IsArray;
        bool isSimple = MapperEngine.IsSimple(tgtItemType);

        Expression srcAsObject = srcPropType.IsValueType
            ? Expression.Convert(srcAccess, typeof(object))
            : (Expression)srcAccess;

        var callExpr = Expression.Convert(
            Expression.Call(helperMethod, srcAsObject, Expression.Constant(targetIsArray), Expression.Constant(isSimple)),
            tgtPropType);

        if (!srcPropType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcPropType)),
                Expression.Assign(tgtAccess, callExpr));
        }

        return Expression.Assign(tgtAccess, callExpr);
    }

    private static Expression BuildComplexAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcPropType, Type tgtPropType)
    {
        // If same type, just reference copy
        if (srcPropType == tgtPropType)
        {
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcPropType, tgtPropType);
        }

        var helperMethod = typeof(TypedPlanBuilder)
            .GetMethod(nameof(MapComplexObject), BindingFlags.NonPublic | BindingFlags.Static)!;

        Expression srcAsObject = srcPropType.IsValueType
            ? Expression.Convert(srcAccess, typeof(object))
            : (Expression)srcAccess;

        var callExpr = Expression.Convert(
            Expression.Call(helperMethod,
                srcAsObject,
                Expression.Constant(tgtPropType, typeof(Type))),
            tgtPropType);

        if (!srcPropType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcPropType)),
                Expression.Assign(tgtAccess, callExpr));
        }

        return Expression.Assign(tgtAccess, callExpr);
    }

    // ---- Runtime helpers (called from expression tree) ----

    private static object MapComplexObject(object src, Type tgtType)
    {
        if (src == null) return null!;
        MapperEngine.EnterMapping();
        try
        {
            var resolvedType = MapperEngine.ResolveSubtypeInternal(src, tgtType);
            var pair = TypedMapperCache.GetOrBuild(src.GetType(), resolvedType);
            var tgt = pair.CreateTarget();
            pair.Map(src, tgt);
            return tgt;
        }
        finally { MapperEngine.ExitMapping(); }
    }

    private static object MapCollectionTyped<TSrc, TTgt>(object srcCol, bool targetIsArray, bool itemIsSimple)
    {
        if (srcCol is not IEnumerable<TSrc> enumerable)
            return targetIsArray ? (object)Array.Empty<TTgt>() : new List<TTgt>();
        var result = new List<TTgt>();
        foreach (var item in enumerable)
        {
            if (item == null) { result.Add(default!); continue; }
            if (itemIsSimple || typeof(TSrc) == typeof(TTgt))
                result.Add((TTgt)(object)item!);
            else
                result.Add((TTgt)MapComplexObject(item, typeof(TTgt)));
        }
        return targetIsArray ? (object)result.ToArray() : result;
    }

    // ---- Helpers ----

    private static Type? GetCollectionItemType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments()[0];

        var enumerable = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }
}
