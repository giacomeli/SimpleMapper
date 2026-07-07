using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SimpleMapper.Net;

internal static class MapperEngine
{
    internal sealed record SetterInfo(Action<object, object?> Set, Type TargetType, bool SkipIfNull);

    private static readonly ConcurrentDictionary<Type, Dictionary<string, Func<object, object?>>> GettersCache = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, SetterInfo>> SettersCache = new();
    private static readonly ConcurrentDictionary<Type, List<SubtypeRule>> SubtypeRules = new();

    internal sealed record SubtypeRule(Func<object, bool> Discriminator, Type TargetType);

    // ---- Execution plan cache ----

    internal enum PropertyKind : byte { Simple, Dictionary, Collection, Complex }

    internal sealed record PropertyPlan(
        string Name,
        Func<object, object?> Get,
        Action<object, object?> Set,
        bool SkipIfNull,
        PropertyKind Kind,
        Type? CollectionItemType,
        Func<IList>? CreateList,
        bool ItemIsSimple);

    internal sealed record TypePlan(
        Func<object> CreateTarget,
        PropertyPlan[] Properties);

    private static readonly ConcurrentDictionary<(Type src, Type tgt), TypePlan> PlanCache = new();

    internal static TypePlan GetOrBuildPlan(Type srcType, Type tgtType)
    {
        return PlanCache.GetOrAdd((srcType, tgtType), key => BuildPlan(key.src, key.tgt));
    }

    internal static Type ResolveSubtypeInternal(object source, Type requestedTargetType)
        => ResolveSubtype(source, requestedTargetType);

    // ---- Recursion depth guard (CWE-674) ----

    [ThreadStatic] private static int _depth;

    /// <summary>
    /// Enters one nesting level. Throws before recursing past the configured limit
    /// so a cyclic or extremely deep graph fails with a catchable exception instead
    /// of a StackOverflowException. Every call must be paired with <see cref="ExitMapping"/>
    /// in a finally block so the per-thread counter is restored on both return and throw.
    /// </summary>
    internal static void EnterMapping()
    {
        if (_depth >= SimpleMapperOptions.MaxDepth)
            throw new MappingDepthExceededException(SimpleMapperOptions.MaxDepth);
        _depth++;
    }

    internal static void ExitMapping() => _depth--;

    // ---- Public API ----

    public static void RegisterSubtype<TSource>(Func<object, bool> discriminator, Type targetType)
        => RegisterSubtype(typeof(TSource), discriminator, targetType);

    public static void RegisterSubtype(Type sourceBaseType, Func<object, bool> discriminator, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(sourceBaseType);
        ArgumentNullException.ThrowIfNull(discriminator);
        ArgumentNullException.ThrowIfNull(targetType);
        var rules = SubtypeRules.GetOrAdd(sourceBaseType, _ => new List<SubtypeRule>());
        lock (rules)
        {
            rules.Add(new SubtypeRule(discriminator, targetType));
        }
    }

    public static TTarget Execute<TTarget>(object source, MappingConfig cfg)
    {
        var resolvedType = ResolveSubtype(source, typeof(TTarget));
        var useFast = !cfg.DebugLogging && cfg.PropertyMappings.Count == 0
            && cfg.ChildConfigs.Count == 0;

        if (useFast)
        {
            var pair = TypedMapperCache.GetOrBuild(source.GetType(), resolvedType);
            var target = pair.CreateTarget();
            MapPropertiesFast(source, target, cfg);
            return (TTarget)target;
        }

        var plan = PlanCache.GetOrAdd((source.GetType(), resolvedType),
            key => BuildPlan(key.src, key.tgt));
        var tgt = plan.CreateTarget();

        if (cfg.DebugLogging)
            MapPropertiesDebug(source, tgt, cfg, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        else
            MapPropertiesDynamic(source, tgt, cfg, plan);

        return (TTarget)tgt;
    }

    public static object Execute(object source, Type targetType, MappingConfig cfg)
    {
        var resolvedType = ResolveSubtype(source, targetType);
        var useFast = !cfg.DebugLogging && cfg.PropertyMappings.Count == 0
            && cfg.ChildConfigs.Count == 0;

        if (useFast)
        {
            var pair = TypedMapperCache.GetOrBuild(source.GetType(), resolvedType);
            var target = pair.CreateTarget();
            MapPropertiesFast(source, target, cfg);
            return target;
        }

        var plan = PlanCache.GetOrAdd((source.GetType(), resolvedType),
            key => BuildPlan(key.src, key.tgt));
        var tgt = plan.CreateTarget();

        if (cfg.DebugLogging)
            MapPropertiesDebug(source, tgt, cfg, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        else
            MapPropertiesDynamic(source, tgt, cfg, plan);

        return tgt;
    }

    // ---- Fast path (no debug, no allocations) ----

    private static void MapPropertiesFast(object src, object tgt, MappingConfig cfg, TypePlan? plan = null)
    {
        EnterMapping();
        try
        {
        if (cfg == MappingConfig.Default || cfg.IsEmpty)
        {
            // Hot path: fully typed, zero boxing
            var pair = TypedMapperCache.GetOrBuild(src.GetType(), tgt.GetType());
            pair.Map(src, tgt);
            return;
        }

        // Fallback for configs with Ignore/PropertyMappings (plan-based path)
        plan ??= PlanCache.GetOrAdd((src.GetType(), tgt.GetType()),
            key => BuildPlan(key.src, key.tgt));

        var props = plan.Properties;

        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];

            if (cfg.IgnoredProperties.Contains(p.Name))
                continue;

            var val = p.Get(src);

            if (val is null)
            {
                if (!p.SkipIfNull) p.Set(tgt, null);
                continue;
            }

            switch (p.Kind)
            {
                case PropertyKind.Simple:
                case PropertyKind.Dictionary:
                    p.Set(tgt, val);
                    break;

                case PropertyKind.Collection:
                    if (p.CreateList != null)
                    {
                        var itemCfg = cfg.ForChild(p.Name).ForCollectionItem();
                        p.Set(tgt, MapCollectionFast((IEnumerable)val, p.CollectionItemType!,
                            p.CreateList, p.ItemIsSimple, itemCfg));
                    }
                    break;

                case PropertyKind.Complex:
                    var childCfg = cfg.ForChild(p.Name);
                    var resolvedType = ResolveSubtype(val, p.CollectionItemType ?? val.GetType());
                    var innerPlan = PlanCache.GetOrAdd((val.GetType(), resolvedType),
                        key => BuildPlan(key.src, key.tgt));
                    var inner = innerPlan.CreateTarget();
                    MapPropertiesFast(val, inner, childCfg, innerPlan);
                    p.Set(tgt, inner);
                    break;
            }
        }
        }
        finally { ExitMapping(); }
    }

    private static object MapCollectionFast(IEnumerable srcCol, Type itemType,
        Func<IList> createList, bool itemIsSimple, MappingConfig cfg)
    {
        var list = createList();

        foreach (var it in srcCol)
        {
            if (it == null || itemIsSimple)
            {
                list.Add(it);
            }
            else
            {
                var resolvedType = ResolveSubtype(it, itemType);
                var innerPlan = PlanCache.GetOrAdd((it.GetType(), resolvedType),
                    key => BuildPlan(key.src, key.tgt));
                var inner = innerPlan.CreateTarget();
                MapPropertiesFast(it, inner, cfg, innerPlan);
                list.Add(inner);
            }
        }

        return list;
    }

    // ---- Dynamic path (PropertyMappings support, no debug) ----

    private static void MapPropertiesDynamic(object src, object tgt, MappingConfig cfg, TypePlan? plan = null)
    {
        EnterMapping();
        try
        {
        plan ??= PlanCache.GetOrAdd((src.GetType(), tgt.GetType()),
            key => BuildPlan(key.src, key.tgt));

        var srcGetters = GettersCache.GetOrAdd(src.GetType(), BuildGetters);
        var props = plan.Properties;

        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];

            if (cfg.IgnoredProperties.Contains(p.Name))
                continue;

            // Resolve the source name via PropertyMappings
            var srcName = cfg.PropertyMappings.TryGetValue(p.Name, out var mapped) ? mapped : p.Name;
            if (!srcGetters.TryGetValue(srcName, out var getter))
                continue;

            var val = getter(src);

            if (val is null)
            {
                if (!p.SkipIfNull) p.Set(tgt, null);
                continue;
            }

            switch (p.Kind)
            {
                case PropertyKind.Simple:
                case PropertyKind.Dictionary:
                    p.Set(tgt, val);
                    break;

                case PropertyKind.Collection:
                    if (p.CreateList != null)
                    {
                        var itemCfg = cfg.ForChild(p.Name).ForCollectionItem();
                        p.Set(tgt, MapCollectionFast((IEnumerable)val, p.CollectionItemType!,
                            p.CreateList, p.ItemIsSimple, itemCfg));
                    }
                    break;

                case PropertyKind.Complex:
                    var childCfg = cfg.ForChild(p.Name);
                    var resolvedType = ResolveSubtype(val, p.CollectionItemType ?? val.GetType());
                    var innerPlan = PlanCache.GetOrAdd((val.GetType(), resolvedType),
                        key => BuildPlan(key.src, key.tgt));
                    var inner = innerPlan.CreateTarget();
                    MapPropertiesDynamic(val, inner, childCfg, innerPlan);
                    p.Set(tgt, inner);
                    break;
            }
        }
        }
        finally { ExitMapping(); }
    }

    // ---- Debug path (preserves TreeConsole output) ----

    private static void MapPropertiesDebug(object src, object tgt, MappingConfig cfg,
        int depth, HashSet<object>? visited)
    {
        EnterMapping();
        try
        {
        var srcGet = GettersCache.GetOrAdd(src.GetType(), BuildGetters);
        var tgtSet = SettersCache.GetOrAdd(tgt.GetType(), BuildSetters);
        var entries = new List<KeyValuePair<string, SetterInfo>>(tgtSet);

        for (int i = 0; i < entries.Count; i++)
        {
            var (tgtName, info) = entries[i];
            bool last = i == entries.Count - 1;

            if (cfg.IgnoredProperties.Contains(tgtName))
            {
                TreeConsole.WriteNode("(ign) " + Path(tgtName, tgt), depth, last, ConsoleColor.DarkGray);
                continue;
            }

            var srcName = cfg.PropertyMappings.TryGetValue(tgtName, out var map) ? map : tgtName;
            if (!srcGet.TryGetValue(srcName, out var getter))
            {
                TreeConsole.WriteNode("(miss) " + Path(srcName, src) + " -> " + Path(tgtName, tgt), depth, last, ConsoleColor.DarkRed);
                continue;
            }

            var val = getter(src);

            if (val is null && info.SkipIfNull)
            {
                TreeConsole.WriteNode("(warn) " + Path(srcName, src) + " -> " + Path(tgtName, tgt) + "  null on non-nullable -> skipped", depth, last, ConsoleColor.DarkYellow);
                continue;
            }

            var label = Path(srcName, src) + " -> " + Path(tgtName, tgt) + "  (" + TreeConsole.PrettyValue(val) + ")";
            TreeConsole.WriteNode(label, depth, last, ConsoleColor.Green);

            if (val == null || IsSimple(val.GetType()))
            {
                info.Set(tgt, val);
            }
            else if (val is IDictionary)
            {
                info.Set(tgt, val);
            }
            else if (val is IEnumerable en && val.GetType() != typeof(string))
            {
                var collChildCfg = cfg.ForChild(tgtName).ForCollectionItem();
                var mapped = MapCollectionDebug(en, info.TargetType, collChildCfg, depth + 1, visited);
                info.Set(tgt, mapped);
            }
            else
            {
                var complexChildCfg = cfg.ForChild(tgtName);
                var mapped = MapComplexDebug(val, info.TargetType, complexChildCfg, depth + 1, visited);
                info.Set(tgt, mapped);
            }
        }
        }
        finally { ExitMapping(); }
    }

    private static object? MapComplexDebug(object srcObj, Type tgtType, MappingConfig cfg,
        int depth, HashSet<object>? visited)
    {
        if (tgtType == typeof(object) || tgtType.IsAssignableFrom(srcObj.GetType()))
        {
            DumpObject(srcObj, depth, visited);
            return srcObj;
        }

        if (IsSimple(tgtType)) return srcObj;

        var plan = PlanCache.GetOrAdd((srcObj.GetType(), tgtType),
            key => BuildPlan(key.src, key.tgt));
        var tgtObj = plan.CreateTarget();
        MapPropertiesDebug(srcObj, tgtObj, cfg, depth, visited);
        return tgtObj;
    }

    private static object? MapCollectionDebug(IEnumerable srcCol, Type tgtColType,
        MappingConfig cfg, int depth, HashSet<object>? visited)
    {
        var itemType = tgtColType.IsGenericType
            ? tgtColType.GetGenericArguments()[0]
            : srcCol.GetType().GetGenericArguments().FirstOrDefault();

        if (itemType == null) return null;

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
        var items = srcCol.Cast<object?>().ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            bool last = i == items.Count - 1;

            TreeConsole.WriteNode("[" + i + "] = " + TreeConsole.PrettyValue(it), depth, last, ConsoleColor.Yellow);

            list.Add(it == null || IsSimple(it.GetType())
                ? it
                : MapComplexDebug(it, itemType, cfg, depth + 1, visited));
        }

        return list;
    }

    // ---- Subtype resolution ----

    private static readonly ConcurrentDictionary<Type, bool> HasSubtypeRules = new();

    private static Type ResolveSubtype(object source, Type requestedTargetType)
    {
        var srcType = source.GetType();

        if (HasSubtypeRules.TryGetValue(srcType, out var has) && !has)
            return requestedTargetType;

        var type = srcType;
        bool foundAny = false;
        while (type != null)
        {
            if (SubtypeRules.TryGetValue(type, out var rules))
            {
                foundAny = true;
                lock (rules)
                {
                    foreach (var rule in rules)
                    {
                        if (rule.Discriminator(source))
                        {
                            HasSubtypeRules.TryAdd(srcType, true);
                            return rule.TargetType;
                        }
                    }
                }
            }
            type = type.BaseType;
        }

        HasSubtypeRules.TryAdd(srcType, foundAny);
        return requestedTargetType;
    }

    // ---- Plan building ----

    private static TypePlan BuildPlan(Type srcType, Type tgtType)
    {
        var getters = GettersCache.GetOrAdd(srcType, BuildGetters);
        var setters = SettersCache.GetOrAdd(tgtType, BuildSetters);

        var factory = ResolveFactory(tgtType);

        var props = new List<PropertyPlan>();
        foreach (var (tgtName, info) in setters)
        {
            if (!getters.TryGetValue(tgtName, out var getter)) continue;

            var kind = ClassifyProperty(info.TargetType);
            Type? itemType = null;
            Func<IList>? createList = null;
            bool itemIsSimple = false;

            if (kind == PropertyKind.Collection)
            {
                itemType = info.TargetType.IsGenericType
                    ? info.TargetType.GetGenericArguments()[0]
                    : null;
                if (itemType != null)
                {
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    createList = () => (IList)Activator.CreateInstance(listType)!;
                    itemIsSimple = IsSimple(itemType);
                }
            }
            else if (kind == PropertyKind.Complex)
            {
                // Store the target type for complex properties
                itemType = info.TargetType;
            }

            props.Add(new PropertyPlan(tgtName, getter, info.Set, info.SkipIfNull,
                kind, itemType, createList, itemIsSimple));
        }

        return new TypePlan(factory, props.ToArray());
    }

    private static Func<object> ResolveFactory(Type t)
    {
        try
        {
            var test = Activator.CreateInstance(t);
            return () => Activator.CreateInstance(t)!;
        }
        catch
        {
            // No usable constructor: create the instance without invoking any
            // constructor and let the mapper populate the members directly.
            return () => RuntimeHelpers.GetUninitializedObject(t);
        }
    }

    private static PropertyKind ClassifyProperty(Type targetType)
    {
        if (IsSimple(targetType)) return PropertyKind.Simple;
        if (typeof(IDictionary).IsAssignableFrom(targetType)) return PropertyKind.Dictionary;
        if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
            return PropertyKind.Collection;
        return PropertyKind.Complex;
    }

    // ---- Dump helpers (debug only) ----

    private static void DumpObject(object obj, int depth, HashSet<object>? visited)
    {
        if (visited == null || !visited.Add(obj)) return;

        var props = obj.GetType()
                       .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                       .ToList();

        for (int i = 0; i < props.Count; i++)
        {
            var p = props[i];
            bool last = i == props.Count - 1;

            object? val;
            try { val = p.GetValue(obj); } catch { continue; }

            TreeConsole.WriteNode(obj.GetType().Name + "." + p.Name + " = " + TreeConsole.PrettyValue(val), depth, last, IsSimple(p.PropertyType) ? ConsoleColor.Cyan : null);

            if (val == null || IsSimple(val.GetType())) continue;

            if (val is IEnumerable en && val.GetType() != typeof(string))
                DumpCollection(en, depth + 1, visited);
            else
                DumpObject(val, depth + 1, visited);
        }
    }

    private static void DumpCollection(IEnumerable en, int depth, HashSet<object> visited)
    {
        var list = en.Cast<object?>().ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var it = list[i];
            bool last = i == list.Count - 1;

            TreeConsole.WriteNode("[" + i + "] = " + TreeConsole.PrettyValue(it), depth, last, ConsoleColor.Yellow);

            if (it != null && !IsSimple(it.GetType()))
                DumpObject(it, depth + 1, visited);
        }
    }

    // ---- Getter/setter building ----

    private static Dictionary<string, Func<object, object?>> BuildGetters(Type t)
    {
        var d = new Dictionary<string, Func<object, object?>>();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            d[p.Name] = BuildPropGetter(t, p);
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            d[f.Name] = BuildFieldGetter(t, f);
        return d;
    }

    private static Dictionary<string, SetterInfo> BuildSetters(Type t)
    {
        // NullabilityInfoContext is not thread-safe; keep it local to this method.
        var ctx = new NullabilityInfoContext();
        var d = new Dictionary<string, SetterInfo>();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanWrite) continue;
            bool skipIfNull = ctx.Create(p).WriteState != NullabilityState.Nullable;
            d[p.Name] = new SetterInfo(BuildPropSetter(t, p), p.PropertyType, skipIfNull);
        }
        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            bool skipIfNull = ctx.Create(f).WriteState != NullabilityState.Nullable;
            d[f.Name] = new SetterInfo(BuildFieldSetter(t, f), f.FieldType, skipIfNull);
        }
        return d;
    }

    private static Func<object, object?> BuildPropGetter(Type t, PropertyInfo p)
    {
        var inst = Expression.Parameter(typeof(object), "i");
        var body = Expression.Convert(Expression.Property(Expression.Convert(inst, t), p), typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, inst).Compile();
    }

    private static Func<object, object?> BuildFieldGetter(Type t, FieldInfo f)
    {
        var inst = Expression.Parameter(typeof(object), "i");
        var body = Expression.Convert(Expression.Field(Expression.Convert(inst, t), f), typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, inst).Compile();
    }

    private static Action<object, object?> BuildPropSetter(Type t, PropertyInfo p)
    {
        var inst = Expression.Parameter(typeof(object), "i");
        var val = Expression.Parameter(typeof(object), "v");
        var assign = BuildSetterAssign(Expression.Property(Expression.Convert(inst, t), p), val, p.PropertyType);
        return Expression.Lambda<Action<object, object?>>(assign, inst, val).Compile();
    }

    private static Action<object, object?> BuildFieldSetter(Type t, FieldInfo f)
    {
        var inst = Expression.Parameter(typeof(object), "i");
        var val = Expression.Parameter(typeof(object), "v");
        var assign = BuildSetterAssign(Expression.Field(Expression.Convert(inst, t), f), val, f.FieldType);
        return Expression.Lambda<Action<object, object?>>(assign, inst, val).Compile();
    }

    private static Expression BuildSetterAssign(MemberExpression member, ParameterExpression val, Type targetType)
    {
        if (targetType == typeof(object))
            return Expression.Assign(member, val);

        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (underlyingType != null)
        {
            return Expression.Assign(member,
                Expression.Condition(
                    Expression.Equal(val, Expression.Constant(null, typeof(object))),
                    Expression.Default(targetType),
                    Expression.Convert(
                        Expression.Call(
                            typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) })!,
                            val,
                            Expression.Constant(underlyingType)),
                        targetType)));
        }

        if (targetType.IsValueType)
        {
            // Fast path: unbox when types match (99% of cases), Convert.ChangeType only for coercion
            return Expression.Assign(member,
                Expression.Condition(
                    Expression.TypeIs(val, targetType),
                    Expression.Unbox(val, targetType),
                    Expression.Convert(
                        Expression.Call(
                            typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) })!,
                            val,
                            Expression.Constant(targetType)),
                        targetType)));
        }

        return Expression.Assign(member, Expression.Convert(val, targetType));
    }

    // ---- Type classification ----

    internal static bool IsSimple(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t.IsPrimitive || t.IsEnum) return true;
        return Type.GetTypeCode(t) switch
        {
            TypeCode.String or TypeCode.Decimal or TypeCode.DateTime => true,
            _ => t == typeof(Guid)
              || t == typeof(TimeSpan)
              || t == typeof(DateOnly)
              || t == typeof(TimeOnly)
              || t == typeof(Uri)
              || t == typeof(Version)
        };
    }

    private static string Path(string name, object obj) => $"{obj.GetType().Name}.{name}";
}
