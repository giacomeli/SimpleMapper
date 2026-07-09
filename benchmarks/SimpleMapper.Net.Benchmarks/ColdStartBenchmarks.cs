using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Mapster;
using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Cold-start scenario: the first mapping of the Blog graph, including whatever
/// each mapper builds lazily — SimpleMapper's plan build and expression compile
/// (caches are reset before every iteration via the internal ClearCaches hook),
/// AutoMapper's configuration plus first map, Mapster's fresh config plus first map.
/// Source generators and manual code have no runtime construction step, so they are
/// out of scope here.
///
/// Methodology note: single-invocation measurements (RunStrategy.Monitoring with
/// one op per iteration) are inherently less precise than steady-state
/// microbenchmarks — read these numbers as orders of magnitude, not exact costs.
/// </summary>
[SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 2, iterationCount: 30, invocationCount: 1)]
public class ColdStartBenchmarks
{
    private Blog _blog = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Subtype rules are user registrations, not derived caches: they are set up
        // once and survive ClearCaches, exactly like in a real application startup.
        SimpleMapperExtensions.RegisterSubtype<Post>(
            source => source is VideoPost, typeof(VideoPostDto));

        _blog = TestData.BuildBlog();
    }

    [IterationSetup(Target = nameof(SimpleMapper_FirstMapping))]
    public void ResetSimpleMapperCaches()
        => MapperEngine.ClearCaches();

    [Benchmark(Description = "SimpleMapper: first Blog -> BlogDto (plan build + compile)")]
    public BlogDto SimpleMapper_FirstMapping()
        => _blog.MapTo<BlogDto>();

    [Benchmark(Description = "AutoMapper: config + first Blog -> BlogDto")]
    public BlogDto AutoMapper_ConfigAndFirstMapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.DisableConstructorMapping();

            cfg.CreateMap<Blog, BlogDto>();
            cfg.CreateMap<Author, AuthorDto>();
            cfg.CreateMap<Location, LocationDto>();
            cfg.CreateMap<Badge, BadgeDto>();
            cfg.CreateMap<Section, SectionDto>();
            cfg.CreateMap<SectionEntry, SectionEntryDto>();
            cfg.CreateMap<Highlight, HighlightDto>();
            cfg.CreateMap<Post, PostDto>()
                .Include<VideoPost, VideoPostDto>();
            cfg.CreateMap<VideoPost, VideoPostDto>();
            cfg.CreateMap<PublishConfig, PublishConfigDto>();
            cfg.CreateMap<Revision, RevisionDto>();
            cfg.CreateMap<Distribution, DistributionDto>();
        });

        return config.CreateMapper().Map<BlogDto>(_blog);
    }

    [Benchmark(Description = "Mapster: fresh config + first Blog -> BlogDto")]
    public BlogDto Mapster_ConfigAndFirstMapping()
    {
        var config = new TypeAdapterConfig();
        config.NewConfig<Post, PostDto>()
            .Include<VideoPost, VideoPostDto>();

        return _blog.Adapt<BlogDto>(config);
    }
}
