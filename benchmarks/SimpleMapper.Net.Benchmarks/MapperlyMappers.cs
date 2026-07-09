using Riok.Mapperly.Abstractions;
using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Source-generated reference point (compile-time mapping, zero runtime reflection).
/// Included as the upper bound: this is what a source generator buys when the
/// project can adopt one — the README recommends exactly that for NativeAOT.
/// Note one semantic difference: Mapperly clones dictionaries, while SimpleMapper
/// and the manual baseline copy them by reference.
/// </summary>
[Mapper]
public static partial class MapperlyMapper
{
    public static partial BlogDto ToDto(Blog source);

    public static partial Blog ToEntity(BlogDto source);

    public static partial CustomerDto ToDto(Customer source);

    public static partial void Update(CustomerDto source, Customer target);

    // The polymorphic pair is dispatched by hand: MapDerivedType cannot list the
    // non-abstract base pair without recursing into the dispatch method itself, so
    // the runtime type check lives here and each concrete pair gets its own
    // generated mapping. This mirrors what the Include<> registrations do in the
    // AutoMapper/Mapster configs.
    [UserMapping(Default = true)]
    private static PostDto PostToDto(Post source)
        => source is VideoPost video ? VideoPostToDto(video) : PostBaseToDto(source);

    [UserMapping(Default = true)]
    private static Post PostToEntity(PostDto source)
        => source is VideoPostDto video ? VideoPostToEntity(video) : PostBaseToEntity(source);

    private static partial VideoPostDto VideoPostToDto(VideoPost source);

    private static partial PostDto PostBaseToDto(Post source);

    private static partial VideoPost VideoPostToEntity(VideoPostDto source);

    private static partial Post PostBaseToEntity(PostDto source);
}
