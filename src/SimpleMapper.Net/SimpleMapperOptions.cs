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
}
