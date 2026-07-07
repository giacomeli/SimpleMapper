using System;
using SimpleMapper.Net;

// Placed in the Microsoft.Extensions.DependencyInjection namespace on purpose,
// following the ecosystem convention for Add* registration methods.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// IServiceCollection registration extensions for SimpleMapper.Net.
/// </summary>
public static class SimpleMapperServiceCollectionExtensions
{
    /// <summary>
    /// Registers polymorphic subtype rules for SimpleMapper.Net.
    /// Mapping itself is fully dynamic — caches are built lazily on first use.
    /// </summary>
    /// <remarks>
    /// WIP / experimental: subtype rules live in a global static registry and must be
    /// registered before the first mapping of the affected types.
    /// </remarks>
    public static IServiceCollection AddSimpleMapper(
        this IServiceCollection services,
        Action<SimpleMapperConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new SimpleMapperConfig();
        configure(config);

        foreach (var sub in config.Subtypes)
            MapperEngine.RegisterSubtype(sub.BaseType, sub.Discriminator, sub.TargetType);

        return services;
    }

    /// <summary>
    /// Registers SimpleMapper.Net without subtype rules.
    /// Mapping is fully dynamic — caches are built lazily on first use.
    /// </summary>
    public static IServiceCollection AddSimpleMapper(this IServiceCollection services)
        => services;
}
