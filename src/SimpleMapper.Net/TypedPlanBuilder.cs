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

        var srcMembers = GetReadableMembers(srcType);
        var tgtMembers = GetWritableMembers(tgtType);

        foreach (var (tgtMember, tgtMemberType) in tgtMembers)
        {
            if (!srcMembers.TryGetValue(tgtMember.Name, out var src))
                continue;
            var (srcMember, srcMemberType) = src;

            Expression? assignment;
            try
            {
                assignment = BuildMemberAssignment(
                    srcTyped, tgtTyped, srcMember, srcMemberType, tgtMember, tgtMemberType);
            }
            catch (MappingException)
            {
                throw;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                throw MappingException.ForMember(
                    srcType, srcMember.Name, srcMemberType,
                    tgtType, tgtMember.Name, tgtMemberType,
                    "the types are not compatible and no conversion is supported. " +
                    "Rename or Ignore the property, or convert the value in your own code.",
                    ex);
            }

            if (assignment != null)
                body.Add(assignment);
        }

        var block = Expression.Block(
            new[] { srcTyped, tgtTyped },
            body);

        return Expression.Lambda<Action<object, object>>(block, srcParam, tgtParam).Compile();
    }

    // Public instance properties (readable, non-indexed) and public instance fields.
    private static Dictionary<string, (MemberInfo Member, Type Type)> GetReadableMembers(Type t)
    {
        var d = new Dictionary<string, (MemberInfo, Type)>();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.GetIndexParameters().Length == 0)
                d[p.Name] = (p, p.PropertyType);
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            d[f.Name] = (f, f.FieldType);
        return d;
    }

    private static List<(MemberInfo Member, Type Type)> GetWritableMembers(Type t)
    {
        var list = new List<(MemberInfo, Type)>();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanWrite && p.GetIndexParameters().Length == 0)
                list.Add((p, p.PropertyType));
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            if (!f.IsInitOnly && !f.IsLiteral)
                list.Add((f, f.FieldType));
        return list;
    }

    private static Expression? BuildMemberAssignment(
        Expression srcTyped, Expression tgtTyped,
        MemberInfo srcMember, Type srcMemberType,
        MemberInfo tgtMember, Type tgtMemberType)
    {
        var srcAccess = Expression.MakeMemberAccess(srcTyped, srcMember);
        var tgtAccess = Expression.MakeMemberAccess(tgtTyped, tgtMember);
        var srcType = srcMember.DeclaringType!;
        var tgtType = tgtMember.DeclaringType!;

        // Simple types (primitives, string, decimal, DateTime, Guid, enums, etc.)
        if (MapperEngine.IsSimple(tgtMemberType) && MapperEngine.IsSimple(srcMemberType))
        {
            return BuildSimpleAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);
        }

        // Dictionary: direct reference copy (documented behavior)
        if (typeof(IDictionary).IsAssignableFrom(tgtMemberType))
        {
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);
        }

        // Collection (List<T>, T[], interfaces assignable from List<T>; not string)
        if (typeof(IEnumerable).IsAssignableFrom(tgtMemberType) && tgtMemberType != typeof(string))
        {
            return BuildCollectionAssignment(
                srcAccess, tgtAccess, srcMember, srcMemberType, tgtMember, tgtMemberType);
        }

        // Untyped target: the target shape is unknowable, keep the reference.
        if (tgtMemberType == typeof(object))
        {
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);
        }

        // Delegates cannot be instantiated by the mapper: keep the reference.
        if (typeof(Delegate).IsAssignableFrom(tgtMemberType))
        {
            return BuildReferenceAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);
        }

        // Value-type (struct) targets: identical types are a plain value copy;
        // anything else cannot be safely populated member-by-member — fail loudly
        // instead of producing a silently zeroed struct.
        if (tgtMemberType.IsValueType)
        {
            if (srcMemberType == tgtMemberType)
                return BuildReferenceAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);

            throw MappingException.ForMember(
                srcType, srcMember.Name, srcMemberType,
                tgtType, tgtMember.Name, tgtMemberType,
                "the target is a value type (struct) and structs are only copied when " +
                "source and target types are identical.");
        }

        // Complex nested object: always deep-map, including when source and target
        // types are identical, so a mapped DTO never aliases the source graph.
        return BuildComplexAssignment(srcAccess, tgtAccess, srcMemberType, tgtMemberType);
    }

    private static Expression BuildSimpleAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcMemberType, Type tgtMemberType)
    {
        // Same type: direct assignment, no boxing for value types
        if (srcMemberType == tgtMemberType)
        {
            return Expression.Assign(tgtAccess, srcAccess);
        }

        // Nullable<T> source -> T target or Nullable<T> target
        var srcUnderlying = Nullable.GetUnderlyingType(srcMemberType);
        var tgtUnderlying = Nullable.GetUnderlyingType(tgtMemberType);

        // Both are the same underlying type (e.g. int? -> int? or int -> int?)
        var srcCore = srcUnderlying ?? srcMemberType;
        var tgtCore = tgtUnderlying ?? tgtMemberType;

        if (srcCore == tgtCore)
        {
            // int -> int? or int? -> int etc.
            return Expression.Assign(tgtAccess, Expression.Convert(srcAccess, tgtMemberType));
        }

        // Numeric coercion (int -> long, float -> double, etc.). Incompatible pairs
        // (string -> double, int -> string, ...) throw here and are wrapped with the
        // member context by Build().
        return Expression.Assign(tgtAccess, Expression.Convert(srcAccess, tgtMemberType));
    }

    private static Expression BuildReferenceAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcMemberType, Type tgtMemberType)
    {
        // For reference types, null-check the source
        if (!srcMemberType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcMemberType)),
                Expression.Assign(tgtAccess,
                    tgtMemberType.IsAssignableFrom(srcMemberType)
                        ? srcAccess
                        : Expression.Convert(srcAccess, tgtMemberType)));
        }

        return Expression.Assign(tgtAccess,
            tgtMemberType.IsAssignableFrom(srcMemberType)
                ? srcAccess
                : Expression.Convert(srcAccess, tgtMemberType));
    }

    private static Expression BuildCollectionAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        MemberInfo srcMember, Type srcMemberType,
        MemberInfo tgtMember, Type tgtMemberType)
    {
        var srcItemType = GetCollectionItemType(srcMemberType);
        var tgtItemType = GetCollectionItemType(tgtMemberType);

        if (srcItemType == null || tgtItemType == null)
        {
            throw MappingException.ForMember(
                srcMember.DeclaringType!, srcMember.Name, srcMemberType,
                tgtMember.DeclaringType!, tgtMember.Name, tgtMemberType,
                "the collection item type could not be resolved. Non-generic collections " +
                "(e.g. ArrayList) are not supported; use a typed collection.");
        }

        // The mapper materializes List<T> (or T[]): the target member must be able
        // to hold one. Reject unsupported shapes (HashSet<T>, immutable collections,
        // custom collections) at plan build instead of a runtime InvalidCastException.
        if (!tgtMemberType.IsArray &&
            !tgtMemberType.IsAssignableFrom(typeof(List<>).MakeGenericType(tgtItemType)))
        {
            throw MappingException.ForMember(
                srcMember.DeclaringType!, srcMember.Name, srcMemberType,
                tgtMember.DeclaringType!, tgtMember.Name, tgtMemberType,
                "the target collection type is not supported. Supported targets: T[], " +
                "List<T> and interfaces a List<T> satisfies (IEnumerable<T>, IList<T>, " +
                "ICollection<T>, IReadOnlyList<T>, IReadOnlyCollection<T>).");
        }

        // Build call to MapCollectionTyped<TSrc, TTgt>(srcAccess, targetIsArray, itemIsSimple)
        var helperMethod = typeof(TypedPlanBuilder)
            .GetMethod(nameof(MapCollectionTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(srcItemType, tgtItemType);

        bool targetIsArray = tgtMemberType.IsArray;
        bool isSimple = MapperEngine.IsSimple(tgtItemType);

        Expression srcAsObject = srcMemberType.IsValueType
            ? Expression.Convert(srcAccess, typeof(object))
            : (Expression)srcAccess;

        var callExpr = Expression.Convert(
            Expression.Call(helperMethod, srcAsObject, Expression.Constant(targetIsArray), Expression.Constant(isSimple)),
            tgtMemberType);

        if (!srcMemberType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcMemberType)),
                Expression.Assign(tgtAccess, callExpr));
        }

        return Expression.Assign(tgtAccess, callExpr);
    }

    private static Expression BuildComplexAssignment(
        Expression srcAccess, MemberExpression tgtAccess,
        Type srcMemberType, Type tgtMemberType)
    {
        var helperMethod = typeof(TypedPlanBuilder)
            .GetMethod(nameof(MapComplexObject), BindingFlags.NonPublic | BindingFlags.Static)!;

        Expression srcAsObject = srcMemberType.IsValueType
            ? Expression.Convert(srcAccess, typeof(object))
            : (Expression)srcAccess;

        var callExpr = Expression.Convert(
            Expression.Call(helperMethod,
                srcAsObject,
                Expression.Constant(tgtMemberType, typeof(Type))),
            tgtMemberType);

        if (!srcMemberType.IsValueType)
        {
            return Expression.IfThen(
                Expression.NotEqual(srcAccess, Expression.Constant(null, srcMemberType)),
                Expression.Assign(tgtAccess, callExpr));
        }

        return Expression.Assign(tgtAccess, callExpr);
    }

    // ---- Runtime helpers (called from expression tree) ----

    private static object MapComplexObject(object src, Type tgtType)
    {
        if (src == null) return null!;
        if (tgtType == typeof(object) || typeof(Delegate).IsAssignableFrom(tgtType))
            return src;
        if (tgtType.IsValueType)
        {
            if (src.GetType() == tgtType) return src;
            throw new MappingException(
                $"Cannot map {src.GetType().Name} -> {tgtType.Name}: the target is a value " +
                "type (struct) and structs are only copied when source and target types " +
                "are identical.");
        }

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
            // Simple items and identical value types are plain copies; identical
            // reference types are deep-mapped like any other complex object so the
            // mapped collection never aliases the source items.
            if (itemIsSimple || (typeof(TSrc) == typeof(TTgt) && typeof(TSrc).IsValueType))
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
