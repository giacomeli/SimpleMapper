using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SimpleMapper.Net;

internal sealed record SubtypeRegistration(
    Type BaseType,
    Func<object, bool> Discriminator,
    Type TargetType);

/// <summary>
/// Configuration object used by AddSimpleMapper to register polymorphic subtype rules.
/// </summary>
public sealed class SimpleMapperConfig
{
    internal readonly List<SubtypeRegistration> Subtypes = new();

    /// <summary>
    /// Registers a subtype for polymorphic mapping. When the source is declared as
    /// <typeparamref name="TBase"/> but matches the discriminator, the subtype's
    /// target type is created instead.
    /// </summary>
    /// <remarks>
    /// WIP / experimental: rules live in a global static registry and must be
    /// registered before the first mapping of the affected types. See the
    /// "Polymorphic mapping" section of the documentation for details.
    /// </remarks>
    [Experimental("SMEXP001")]
    public SimpleMapperConfig MapSubtype<TBase>(Func<object, bool> discriminator, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(discriminator);
        ArgumentNullException.ThrowIfNull(targetType);
        Subtypes.Add(new SubtypeRegistration(typeof(TBase), discriminator, targetType));
        return this;
    }
}
