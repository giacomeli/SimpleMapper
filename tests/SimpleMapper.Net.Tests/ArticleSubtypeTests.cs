using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// Polymorphic mapping over a realistic base/derived pair, mirroring the classic
/// "article vs video article" scenario without depending on any external domain.
/// </summary>
public sealed class ArticleSubtypeTests
{
    // ---- Test models (blog domain) ----

    public class Article
    {
        public string? Id { get; set; }
        public string Title { get; set; } = "";
        public int Views { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class VideoArticle : Article
    {
        public string VideoUrl { get; set; } = "";
        public int DurationSeconds { get; set; }
    }

    public class ArticleDto
    {
        public string? Id { get; set; }
        public string Title { get; set; } = "";
        public int Views { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class VideoArticleDto : ArticleDto
    {
        public string VideoUrl { get; set; } = "";
        public int DurationSeconds { get; set; }
    }

    static ArticleSubtypeTests()
    {
        SimpleMapperExtensions.RegisterSubtype<Article>(
            source => source is VideoArticle, typeof(VideoArticleDto));
        SimpleMapperExtensions.RegisterSubtype<ArticleDto>(
            source => source is VideoArticleDto, typeof(VideoArticle));
    }

    [Fact]
    public void VideoArticle_MapsTo_VideoArticleDto()
    {
        var video = new VideoArticle { Title = "Launch", VideoUrl = "https://cdn/x.mp4", DurationSeconds = 90 };
        var result = video.MapTo<ArticleDto>();

        Assert.IsType<VideoArticleDto>(result);
        Assert.Equal("Launch", result.Title);
        Assert.Equal("https://cdn/x.mp4", ((VideoArticleDto)result).VideoUrl);
    }

    [Fact]
    public void Article_MapsTo_ArticleDto_NotVideoDto()
    {
        var article = new Article { Title = "Notes" };
        var result = article.MapTo<ArticleDto>();

        Assert.IsType<ArticleDto>(result);
        Assert.Equal("Notes", result.Title);
    }

    [Fact]
    public void VideoArticleDto_MapsTo_VideoArticle()
    {
        var dto = new VideoArticleDto { Title = "Recap" };
        var result = dto.MapTo<Article>();

        Assert.IsType<VideoArticle>(result);
        Assert.Equal("Recap", result.Title);
    }
}
