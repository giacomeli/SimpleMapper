using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Mapping onto an existing instance: MapTo(destination) and the builder's
/// To(destination). The classic use case is applying a DTO onto a tracked entity.
/// </summary>
public sealed class MapIntoTests
{
    private class UserDto
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class UserEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Audit { get; set; } = "";
    }

    [Fact]
    public void MapTo_Destination_OverwritesMatchedAndKeepsUnmatched()
    {
        var entity = new UserEntity { Id = 10, Name = "old", Age = 1, Audit = "keep" };
        var dto = new UserDto { Name = "new", Age = 30 };

        var result = dto.MapTo(entity);

        Assert.Same(entity, result);
        Assert.Equal("new", entity.Name);
        Assert.Equal(30, entity.Age);
        Assert.Equal(10, entity.Id);
        Assert.Equal("keep", entity.Audit);
    }

    [Fact]
    public void MapTo_Destination_NullSource_LeavesDestinationUntouched()
    {
        var entity = new UserEntity { Name = "old" };

        var result = ((UserDto?)null).MapTo(entity);

        Assert.Same(entity, result);
        Assert.Equal("old", entity.Name);
    }

    [Fact]
    public void MapTo_Destination_NullDestination_Throws()
    {
        var dto = new UserDto { Name = "n" };

        Assert.Throws<ArgumentNullException>(() => dto.MapTo((UserEntity)null!));
    }

    [Fact]
    public void Builder_To_Destination_RespectsIgnore()
    {
        var entity = new UserEntity { Name = "old", Age = 1 };
        var dto = new UserDto { Name = "new", Age = 30 };

        dto.Map()
            .Ignore(nameof(UserDto.Age))
            .To(entity);

        Assert.Equal("new", entity.Name);
        Assert.Equal(1, entity.Age);
    }

    [Fact]
    public void Builder_To_Destination_RespectsRename()
    {
        var entity = new UserEntity();
        var dto = new UserDto { Name = "renamed" };

        dto.Map()
            .Map("Name", "Audit")
            .To(entity);

        Assert.Equal("renamed", entity.Audit);
    }
}
