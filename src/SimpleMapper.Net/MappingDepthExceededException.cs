using System;

namespace SimpleMapper.Net;

/// <summary>
/// Thrown when a mapping exceeds <see cref="SimpleMapperOptions.MaxDepth"/>, which
/// usually indicates a cyclic object graph (for example bidirectional or ORM
/// navigation references) or an extremely deep one. Raising it turns an
/// unrecoverable StackOverflowException into a catchable error.
/// </summary>
public sealed class MappingDepthExceededException : InvalidOperationException
{
    /// <summary>The configured maximum depth that was exceeded.</summary>
    public int MaxDepth { get; }

    /// <summary>Creates the exception for the given configured maximum depth.</summary>
    public MappingDepthExceededException(int maxDepth)
        : base($"Mapping exceeded the maximum depth of {maxDepth}. This usually means the " +
               "object graph is cyclic (e.g. a bidirectional or navigation reference) or " +
               "extremely deep. SimpleMapper.Net does not follow reference cycles: break the " +
               "cycle before mapping (e.g. Ignore the back-reference) or raise " +
               "SimpleMapperOptions.MaxDepth if the graph is legitimately deep.")
    {
        MaxDepth = maxDepth;
    }
}
