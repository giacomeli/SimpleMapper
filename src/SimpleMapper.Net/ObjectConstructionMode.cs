namespace SimpleMapper.Net;

/// <summary>
/// Controls how mapping targets without a parameterless constructor are created.
/// Configured globally via <see cref="SimpleMapperOptions.ObjectConstruction"/>, or
/// per call via <see cref="MapperBuilder{TSource}.AllowUninitializedObjects"/>.
/// </summary>
public enum ObjectConstructionMode
{
    /// <summary>
    /// Default. Targets must expose a parameterless constructor (any visibility);
    /// mapping to a type without one throws <see cref="MappingException"/> instead
    /// of silently creating an instance whose constructor never ran.
    /// </summary>
    RequireParameterlessConstructor,

    /// <summary>
    /// Targets without a parameterless constructor are created uninitialized
    /// (<see cref="System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(System.Type)"/>)
    /// and populated member by member. Constructor logic, constructor-enforced
    /// invariants and field initializers are all skipped — opt in only for types
    /// that tolerate it (e.g. positional records used as DTOs).
    /// </summary>
    AllowUninitializedObjects,
}
