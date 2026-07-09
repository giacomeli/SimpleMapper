using AutoMapper;
using BenchmarkDotNet.Attributes;
using Mapster;
using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Map-into-existing-instance scenario over the flat Customer pair — the shape the
/// README recommends for updating tracked entities (scalars only, no navigations).
/// Every mapper writes onto the same preallocated destination.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MapIntoBenchmarks
{
    private CustomerDto _dto = null!;
    private Customer _destination = null!;
    private IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.DisableConstructorMapping();
            cfg.CreateMap<CustomerDto, Customer>();
        });
        _autoMapper = config.CreateMapper();
        _mapsterConfig = new TypeAdapterConfig();

        _dto = new CustomerDto
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
        _destination = new Customer();

        // Warm up every mapper so lazy caches are built outside the measurement.
        ManualMapper.Update(_dto, _destination);
        MapperlyMapper.Update(_dto, _destination);
        _dto.MapTo(_destination);
        _ = _autoMapper.Map(_dto, _destination);
        _ = _dto.Adapt(_destination, _mapsterConfig);
    }

    [Benchmark(Description = "Manual: CustomerDto -> existing Customer", Baseline = true)]
    public Customer Manual()
    {
        ManualMapper.Update(_dto, _destination);
        return _destination;
    }

    [Benchmark(Description = "Mapperly: CustomerDto -> existing Customer")]
    public Customer Mapperly()
    {
        MapperlyMapper.Update(_dto, _destination);
        return _destination;
    }

    [Benchmark(Description = "SimpleMapper: CustomerDto -> existing Customer")]
    public Customer SimpleMapper()
    {
        _dto.MapTo(_destination);
        return _destination;
    }

    [Benchmark(Description = "AutoMapper: CustomerDto -> existing Customer")]
    public Customer AutoMapper()
        => _autoMapper.Map(_dto, _destination);

    [Benchmark(Description = "Mapster: CustomerDto -> existing Customer")]
    public Customer Mapster()
        => _dto.Adapt(_destination, _mapsterConfig);
}
