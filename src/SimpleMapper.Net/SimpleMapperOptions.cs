namespace SimpleMapper.Net;

/// <summary>
/// Global options for SimpleMapper.Net.
/// </summary>
public static class SimpleMapperOptions
{
    private static int _maxDepth = 100;

    /// <summary>
    /// Maximum object-graph nesting depth followed during a single mapping.
    /// Guards against uncontrolled recursion (cyclic or extremely deep graphs):
    /// exceeding it throws <see cref="MappingDepthExceededException"/> instead of
    /// terminating the process with a StackOverflowException. Default: 100.
    /// </summary>
    /// <remarks>
    /// SimpleMapper.Net does not follow reference cycles. Raise this only if your
    /// graph is legitimately deep; a value at or below zero is rejected.
    /// </remarks>
    public static int MaxDepth
    {
        get => _maxDepth;
        set
        {
            if (value < 1)
                throw new System.ArgumentOutOfRangeException(nameof(value),
                    "MaxDepth must be at least 1.");
            _maxDepth = value;
        }
    }

    /// <summary>
    /// How mapping targets without a parameterless constructor are created.
    /// Default: <see cref="ObjectConstructionMode.RequireParameterlessConstructor"/> —
    /// mapping to such a type throws <see cref="MappingException"/> naming the type
    /// and the two ways out. The mode is checked every time an instance is created,
    /// so changing it also affects mappings whose plans are already compiled.
    /// </summary>
    public static ObjectConstructionMode ObjectConstruction { get; set; }
        = ObjectConstructionMode.RequireParameterlessConstructor;
}
