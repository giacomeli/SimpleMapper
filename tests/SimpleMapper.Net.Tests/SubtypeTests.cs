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
}
