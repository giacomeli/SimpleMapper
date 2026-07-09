using AutoMapper;
using BenchmarkDotNet.Attributes;
using Mapster;
using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Deep-graph comparison over the same synthetic object graph: manual mapping
/// (baseline), Mapperly (source generator), SimpleMapper.Net, AutoMapper 14 and
/// Mapster. Every mapper runs in the same process, so any resource limits (see
/// docker-compose.benchmarks.yml) apply equally by construction.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MappingBenchmarks
{
    private Blog _blog = null!;
    private BlogDto _blogDto = null!;
    private IMapper _autoMapper = null!;
    private TypeAdapterConfig _mapsterConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        // AutoMapper: explicit map registration for every pair, both directions.
        var config = new MapperConfiguration(cfg =>
        {
            cfg.DisableConstructorMapping();

            cfg.CreateMap<Blog, BlogDto>();
            cfg.CreateMap<BlogDto, Blog>();
            cfg.CreateMap<Author, AuthorDto>();
            cfg.CreateMap<AuthorDto, Author>();
            cfg.CreateMap<Location, LocationDto>();
            cfg.CreateMap<LocationDto, Location>();
            cfg.CreateMap<Badge, BadgeDto>();
            cfg.CreateMap<BadgeDto, Badge>();
            cfg.CreateMap<Section, SectionDto>();
            cfg.CreateMap<SectionDto, Section>();
            cfg.CreateMap<SectionEntry, SectionEntryDto>();
            cfg.CreateMap<SectionEntryDto, SectionEntry>();
            cfg.CreateMap<Highlight, HighlightDto>();
            cfg.CreateMap<HighlightDto, Highlight>();
            cfg.CreateMap<Post, PostDto>()
                .Include<VideoPost, VideoPostDto>();
            cfg.CreateMap<PostDto, Post>()
                .Include<VideoPostDto, VideoPost>();
            cfg.CreateMap<VideoPost, VideoPostDto>();
            cfg.CreateMap<VideoPostDto, VideoPost>();
            cfg.CreateMap<PublishConfig, PublishConfigDto>();
            cfg.CreateMap<PublishConfigDto, PublishConfig>();
            cfg.CreateMap<Revision, RevisionDto>();
            cfg.CreateMap<RevisionDto, Revision>();
            cfg.CreateMap<Distribution, DistributionDto>();
            cfg.CreateMap<DistributionDto, Distribution>();
        });
        _autoMapper = config.CreateMapper();

        // Mapster: convention-based; the polymorphic pair needs Include, like AutoMapper.
        _mapsterConfig = new TypeAdapterConfig();
        _mapsterConfig.NewConfig<Post, PostDto>()
            .Include<VideoPost, VideoPostDto>();
        _mapsterConfig.NewConfig<PostDto, Post>()
            .Include<VideoPostDto, VideoPost>();

        // SimpleMapper: convention-based; only the polymorphic pair needs registration.
        SimpleMapperExtensions.RegisterSubtype<Post>(
            source => source is VideoPost, typeof(VideoPostDto));
        SimpleMapperExtensions.RegisterSubtype<PostDto>(
            source => source is VideoPostDto, typeof(VideoPost));

        _blog = TestData.BuildBlog();

        // Build the DTO graph once so the reverse scenario maps an equivalent object.
        _blogDto = _autoMapper.Map<BlogDto>(_blog);

        // Warm up every mapper so lazy caches are built outside the measurement.
        _ = _blog.MapTo<BlogDto>();
        _ = _blogDto.MapTo<Blog>();
        _ = _blog.Adapt<BlogDto>(_mapsterConfig);
        _ = _blogDto.Adapt<Blog>(_mapsterConfig);
        _ = MapperlyMapper.ToDto(_blog);
        _ = MapperlyMapper.ToEntity(_blogDto);
        _ = ManualMapper.ToDto(_blog);
        _ = ManualMapper.ToEntity(_blogDto);
    }

    [Benchmark(Description = "Manual: Blog -> BlogDto", Baseline = true)]
    public BlogDto Manual_EntityToDto()
        => ManualMapper.ToDto(_blog);

    [Benchmark(Description = "Manual: BlogDto -> Blog")]
    public Blog Manual_DtoToEntity()
        => ManualMapper.ToEntity(_blogDto);

    [Benchmark(Description = "Mapperly: Blog -> BlogDto")]
    public BlogDto Mapperly_EntityToDto()
        => MapperlyMapper.ToDto(_blog);

    [Benchmark(Description = "Mapperly: BlogDto -> Blog")]
    public Blog Mapperly_DtoToEntity()
        => MapperlyMapper.ToEntity(_blogDto);

    [Benchmark(Description = "Mapster: Blog -> BlogDto")]
    public BlogDto Mapster_EntityToDto()
        => _blog.Adapt<BlogDto>(_mapsterConfig);

    [Benchmark(Description = "Mapster: BlogDto -> Blog")]
    public Blog Mapster_DtoToEntity()
        => _blogDto.Adapt<Blog>(_mapsterConfig);

    [Benchmark(Description = "AutoMapper: Blog -> BlogDto")]
    public BlogDto AutoMapper_EntityToDto()
        => _autoMapper.Map<BlogDto>(_blog);

    [Benchmark(Description = "SimpleMapper: Blog -> BlogDto")]
    public BlogDto SimpleMapper_EntityToDto()
        => _blog.MapTo<BlogDto>();

    [Benchmark(Description = "AutoMapper: BlogDto -> Blog")]
    public Blog AutoMapper_DtoToEntity()
        => _autoMapper.Map<Blog>(_blogDto);

    [Benchmark(Description = "SimpleMapper: BlogDto -> Blog")]
    public Blog SimpleMapper_DtoToEntity()
        => _blogDto.MapTo<Blog>();

    [Benchmark(Description = "AutoMapper: Blog -> BlogDto (x100)")]
    public List<BlogDto> AutoMapper_Batch100()
    {
        var results = new List<BlogDto>(100);
        for (int i = 0; i < 100; i++)
            results.Add(_autoMapper.Map<BlogDto>(_blog));
        return results;
    }

    [Benchmark(Description = "SimpleMapper: Blog -> BlogDto (x100)")]
    public List<BlogDto> SimpleMapper_Batch100()
    {
        var results = new List<BlogDto>(100);
        for (int i = 0; i < 100; i++)
            results.Add(_blog.MapTo<BlogDto>());
        return results;
    }
}
