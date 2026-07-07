using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Net;

// Support types kept in separate namespaces on purpose: they simulate a consumer
// application with distinct domain and DTO namespaces.
namespace SimpleMapper.Net.Tests.Domain
{
    public class Article { public string Title { get; set; } = ""; public int Views  { get; set; } }
    public class Author  { public string Name  { get; set; } = ""; public string Email { get; set; } = ""; }
}

namespace SimpleMapper.Net.Tests.Dtos
{
    public class ArticleDto { public string Title { get; set; } = ""; public int Views  { get; set; } }
    public class AuthorDto  { public string Name  { get; set; } = ""; public string Email { get; set; } = ""; }
}

namespace SimpleMapper.Net.Tests
{
public sealed class LazyMappingTests
{
    [Fact]
    public void MapTo_WorksWithoutAnyRegistration()
    {
        var article = new Domain.Article { Title = "Lazy", Views = 99 };
        var dto = article.MapTo<Dtos.ArticleDto>();

        Assert.Equal("Lazy", dto.Title);
        Assert.Equal(99, dto.Views);
    }

    [Fact]
    public void MapTo_Bidirectional_DtoToModel()
    {
        var dto = new Dtos.ArticleDto { Title = "Reverse", Views = 50 };
        var model = dto.MapTo<Domain.Article>();

        Assert.Equal("Reverse", model.Title);
        Assert.Equal(50, model.Views);
    }

    [Fact]
    public void MapTo_Bidirectional_MultipleTypes()
    {
        var author = new Domain.Author { Name = "John", Email = "john@test.com" };
        var dto = author.MapTo<Dtos.AuthorDto>();

        Assert.Equal("John", dto.Name);
        Assert.Equal("john@test.com", dto.Email);

        var back = dto.MapTo<Domain.Author>();
        Assert.Equal("John", back.Name);
        Assert.Equal("john@test.com", back.Email);
    }

    [Fact]
    public void AddSimpleMapper_WithSubtypes_RegistersCorrectly()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddSimpleMapper(config => config
                .MapSubtype<Domain.Article>(
                    _ => false,
                    typeof(Dtos.ArticleDto))));

        Assert.Null(exception);
    }

    [Fact]
    public void AddSimpleMapper_NoArgs_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddSimpleMapper());

        Assert.Null(exception);
    }
}
}
