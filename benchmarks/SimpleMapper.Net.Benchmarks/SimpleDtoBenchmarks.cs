using AutoMapper;
using BenchmarkDotNet.Attributes;
using Mapster;
using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Flat-DTO scenario: a pair of eight scalar members and no nesting. Fixed per-call
/// overhead (cache lookup, delegate dispatch) dominates here, so this is where the
/// relative gap between runtime mappers and compile-time code is at its widest —
/// published on purpose.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class SimpleDtoBenchmarks
{
    private Customer _customer = null!;
    private IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.DisableConstructorMapping();
            cfg.CreateMap<Customer, CustomerDto>();
        });
        _autoMapper = config.CreateMapper();
        _mapsterConfig = new TypeAdapterConfig();

        _customer = new Customer
        {
            Id = Guid.Parse("5f0c6b39-08e5-4d54-9db1-9c9f0a1a2b3c"),
            FirstName = "Ana",
            LastName = "Souza",
            Email = "ana.souza@example.com",
            CreatedAt = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            IsActive = true,
            LoyaltyPoints = 4200,
            CreditLimit = 15_000.50m,
        };

        // Warm up every mapper so lazy caches are built outside the measurement.
        _ = ManualMapper.ToDto(_customer);
        _ = MapperlyMapper.ToDto(_customer);
        _ = _customer.MapTo<CustomerDto>();
        _ = _autoMapper.Map<CustomerDto>(_customer);
        _ = _customer.Adapt<CustomerDto>(_mapsterConfig);
    }

    [Benchmark(Description = "Manual: Customer -> CustomerDto", Baseline = true)]
    public CustomerDto Manual()
        => ManualMapper.ToDto(_customer);

    [Benchmark(Description = "Mapperly: Customer -> CustomerDto")]
    public CustomerDto Mapperly()
        => MapperlyMapper.ToDto(_customer);

    [Benchmark(Description = "SimpleMapper: Customer -> CustomerDto")]
    public CustomerDto SimpleMapper()
        => _customer.MapTo<CustomerDto>();

    [Benchmark(Description = "AutoMapper: Customer -> CustomerDto")]
    public CustomerDto AutoMapper()
        => _autoMapper.Map<CustomerDto>(_customer);

    [Benchmark(Description = "Mapster: Customer -> CustomerDto")]
    public CustomerDto Mapster()
        => _customer.Adapt<CustomerDto>(_mapsterConfig);
}
