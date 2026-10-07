using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class SubtypeTests
{
    private class Animal { public string Name { get; set; } = ""; }
    private class Dog : Animal { public string Breed { get; set; } = ""; }
    private class AnimalDto { public string Name { get; set; } = ""; }
    private class DogDto : AnimalDto { public string Breed { get; set; } = ""; }

    [Fact]
    public void RegisterSubtype_WhenSourceIsDerived_MapsToSubtypeTarget()
    {
        SimpleMapperExtensions.RegisterSubtype<Animal>(
            source => source is Dog,
            typeof(DogDto));

        var dog = new Dog { Name = "Rex", Breed = "Labrador" };
        var result = dog.MapTo<AnimalDto>();

        Assert.IsType<DogDto>(result);
        Assert.Equal("Rex", result.Name);
        Assert.Equal("Labrador", ((DogDto)result).Breed);
    }

    [Fact]
    public void RegisterSubtype_WhenSourceIsBase_MapsToBaseTarget()
    {
        SimpleMapperExtensions.RegisterSubtype<Animal>(
            source => source is Dog,
            typeof(DogDto));

        var animal = new Animal { Name = "Generic" };
        var result = animal.MapTo<AnimalDto>();

        Assert.IsType<AnimalDto>(result);
        Assert.Equal("Generic", result.Name);
    }

    [Fact]
    public void RegisterSubtype_ReverseDirection_Works()
    {
        SimpleMapperExtensions.RegisterSubtype<AnimalDto>(
            source => source is DogDto,
            typeof(Dog));

        var dogDto = new DogDto { Name = "Rex", Breed = "Labrador" };
        var result = dogDto.MapTo<Animal>();

        Assert.IsType<Dog>(result);
        Assert.Equal("Labrador", ((Dog)result).Breed);
    }

    private class Vehicle { public string Plate { get; set; } = ""; }
    private class Car : Vehicle { public int Doors { get; set; } }
    private class VehicleDto { public string Plate { get; set; } = ""; }
    private class CarDto : VehicleDto { public int Doors { get; set; } }

    [Fact]
    public void RegisterSubtype_WhenRuleTargetIsIncompatibleWithRequestedTarget_IsSkipped()
    {
        // Rule registered for the DTO -> model direction.
        SimpleMapperExtensions.RegisterSubtype<VehicleDto>(
            source => source is CarDto,
            typeof(Car));

        var carDto = new CarDto { Plate = "ABC1D23", Doors = 4 };

        // A DTO -> DTO copy must not resolve to the model subtype (Car is not a VehicleDto).
        var copy = carDto.MapTo<VehicleDto>();
        var model = carDto.MapTo<Vehicle>();

        Assert.IsAssignableFrom<VehicleDto>(copy);
        Assert.Equal("ABC1D23", copy.Plate);
        Assert.IsType<Car>(model);
        Assert.Equal(4, ((Car)model).Doors);
    }

    [Fact]
    public void RegisterSubtype_IncompatibleRule_IsSkippedForCollectionItems()
    {
        SimpleMapperExtensions.RegisterSubtype<VehicleDto>(
            source => source is CarDto,
            typeof(Car));

        var items = new List<VehicleDto> { new CarDto { Plate = "XYZ9A87", Doors = 2 }, new VehicleDto { Plate = "KLM5B43" } };

        var copies = items.MapListTo<VehicleDto>();

        Assert.Equal(2, copies.Count);
        Assert.All(copies, c => Assert.IsAssignableFrom<VehicleDto>(c));
        Assert.Equal(new[] { "XYZ9A87", "KLM5B43" }, copies.Select(c => c.Plate));
    }

    private class Garage { public Vehicle? Main { get; set; } }
    private class GarageDto { public VehicleDto? Main { get; set; } }

    [Fact]
    public void RegisterSubtype_NestedProperty_UsesTheDeclaredTargetType()
    {
        SimpleMapperExtensions.RegisterSubtype<VehicleDto>(
            source => source is CarDto,
            typeof(Car));

        var dto = new GarageDto { Main = new CarDto { Plate = "QWE4R56", Doors = 4 } };

        var copy = dto.MapTo<GarageDto>();
        var model = dto.MapTo<Garage>();

        Assert.IsAssignableFrom<VehicleDto>(copy.Main);
        Assert.IsType<Car>(model.Main);
        Assert.Equal(4, ((Car)model.Main!).Doors);
    }
}
