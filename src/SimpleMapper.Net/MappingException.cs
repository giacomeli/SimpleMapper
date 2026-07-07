using System;

namespace SimpleMapper.Net;

/// <summary>
/// Thrown when a source/target member pair cannot be mapped — incompatible types,
/// an unsupported target collection type, or a value-type (struct) target that is
/// not identical to its source. The message always names the offending property
/// and both types so the failure is diagnosable without a debugger.
/// </summary>
public sealed class MappingException : InvalidOperationException
{
    /// <summary>Creates the exception with a message naming the failing member pair.</summary>
    public MappingException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the underlying failure as inner exception.</summary>
    public MappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    internal static MappingException ForMember(
        Type srcType, string srcName, Type srcMemberType,
        Type tgtType, string tgtName, Type tgtMemberType,
        string reason, Exception? inner = null)
    {
        var message =
            $"Cannot map {srcType.Name}.{srcName} ({srcMemberType.Name}) -> " +
            $"{tgtType.Name}.{tgtName} ({tgtMemberType.Name}): {reason}";
        return inner is null ? new MappingException(message) : new MappingException(message, inner);
    }
}
