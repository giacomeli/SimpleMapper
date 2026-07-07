using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Deterministic, hard-coded object graph builder. Complexity is equivalent to a
/// real content platform: ~60 mapped properties across 4-5 nesting levels, simple
/// and complex collections, a dictionary and one polymorphic video post.
/// </summary>
public static class TestData
{
    public static Blog BuildBlog()
    {
        var location = new Location
        {
            Street = "Flower Street",
            Number = "123",
            Complement = "Suite 45",
            District = "Downtown",
            City = "Springfield",
            State = "IL",
            Country = "US",
            ZipCode = "62701",
            Reference = "Next to the central park",
            Latitude = 39.7817,
            Longitude = -89.6501
        };

        var owner = new Author
        {
            Id = "aut_0001",
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@example.com",
            Website = "https://johnsmith.dev",
            TaxId = "123.456.789-00",
            BirthDate = new DateTime(1988, 4, 12),
            JoinedAt = new DateTime(2023, 1, 15),
            IsVerified = true,
            Tier = AuthorTier.Lead,
            ReputationPoints = 4200,
            NewsletterOptIn = true,
            PreferredLanguage = "en-US",
            Company = "Acme Media",
            Role = "Editor-in-Chief",
            Focus = "Technology",
            TotalRevenue = 15890.42m,
            PostCount = 37,
            LastPostAt = new DateTime(2026, 5, 2),
            Notes = "Publishes on weekday mornings",
            Locations = new List<Location> { location },
            Handles = new List<string> { "@johnsmith", "@jsmith_dev" },
            Badges = new List<Badge>
            {
                new() { Name = "TOP_AUTHOR", Count = 1 },
                new() { Name = "STREAK_30", Count = 3 },
            }
        };

        var posts = Enumerable.Range(1, 3).Select(i => BuildPost(i)).ToList();

        var featured = new Section
        {
            Id = "sec_0001",
            CreatedAt = new DateTime(2026, 6, 30, 10, 15, 0),
            Title = "Editor's Picks",
            Slug = "editors-picks",
            Description = "Hand-selected posts for this week.",
            ItemCount = 6,
            ViewCount = 12045,
            Layout = "grid",
            IsPinned = true,
            Language = "en-US",
            Position = 1,
            IsVisible = true,
            Theme = "spotlight",
            Order = 1,
            FeaturedHighlight = new Highlight
            {
                Label = "TRENDING",
                Weight = 10,
                Scope = HighlightScope.Blog,
                Kind = HighlightKind.Percentage
            },
            Entries = new List<SectionEntry>
            {
                new()
                {
                    Post = posts[0],
                    Position = 1,
                    Score = 129.90m,
                    Weight = 2.5m,
                    Highlight = new Highlight
                    {
                        Label = "STAFF_PICK",
                        Weight = 10,
                        Scope = HighlightScope.Post,
                        Kind = HighlightKind.Percentage
                    }
                },
                new() { Post = posts[1], Position = 2, Score = 29.90m, Weight = 1.5m },
                new()
                {
                    // Polymorphic entry: a video post declared as Post
                    Post = BuildVideoPost(),
                    Position = 3,
                    Score = 40.20m,
                    Weight = 1.0m
                },
            }
        };

        return new Blog
        {
            Id = "blog_0001",
            Title = "The Acme Journal",
            Slug = "acme-journal",
            CreatedAt = new DateTime(2026, 6, 30, 10, 21, 33),
            UpdatedAt = new DateTime(2026, 6, 30, 10, 25, 0),
            Status = PublishStatus.Published,
            Language = "en-US",
            StorageUsedMb = 389.70m,
            BandwidthGb = 12.50m,
            MonthlyBudget = 38.97m,
            TotalCost = 363.23m,
            Tagline = "Stories from the Acme newsroom",
            IsFeatured = true,
            PostCount = 6,
            Owner = owner,
            Headquarters = location,
            Publishing = new PublishConfig
            {
                Channel = "web",
                Provider = "acme-publish",
                MaxDrafts = 3,
                ScheduleOffset = 121.08m,
                AllowScheduling = true,
                MaxRevisions = 12,
                MinInterval = 25m,
                DisplayName = "Web (scheduled up to 12x)",
                CompatibleChannels = new List<string> { "web", "newsletter", "rss" },
                Options = new Dictionary<string, string>
                {
                    ["workspace_id"] = "w-778899",
                    ["autopublish"] = "on",
                    ["byline_format"] = "ACME*NEWS"
                }
            },
            Distribution = new Distribution
            {
                Channel = "Syndicate",
                TrackingCode = "SYN123456789US",
                ScheduledAt = new DateTime(2026, 7, 4),
                Location = location,
                Notes = "Distributed via Syndicate standard"
            },
            Featured = featured,
            Revisions = new List<Revision>
            {
                new()
                {
                    Id = "rev_0001",
                    CreatedAt = new DateTime(2026, 6, 30, 10, 22, 0),
                    ChangeSize = 363.23m,
                    Status = RevisionStatus.Approved,
                    CommitHash = "AUTH-889900",
                    RevisionId = "TX-556677",
                    ParentReference = "gw-ref-112233",
                    Editor = "John Smith",
                    EditorHandle = "@johnsmith",
                    Number = 3,
                    Summary = "Copyedit pass",
                    ConflictNote = "",
                    RevertedLines = 0m,
                    PublishedAt = new DateTime(2026, 6, 30, 10, 22, 5),
                    RevertedAt = null,
                    Kind = "copyedit",
                    Language = "en-US",
                    ReviewScore = 10.90m,
                    NetChange = 352.33m,
                    Attempt = 1
                }
            },
            StatusHistory = new List<string> { "Draft", "Scheduled", "Published" },
            AuditLog = new List<string>
            {
                "2026-06-30T10:21:33Z blog created",
                "2026-06-30T10:22:05Z revision approved",
                "2026-06-30T10:25:00Z featured flag set"
            }
        };
    }

    private static Post BuildPost(int i) => new()
    {
        Id = $"pst_{i:0000}",
        Title = $"Getting Started, Part {i}",
        Slug = $"getting-started-part-{i}",
        Subtitle = "A practical walkthrough",
        Summary = "Short summary of the post content.",
        Body = "The full body text of the article goes here, spanning several paragraphs.",
        BodyHtml = "<p>The full body text of the article goes here.</p>",
        Excerpt = "A practical walkthrough for beginners.",
        SeoTitle = $"Getting Started, Part {i} - The Acme Journal",
        SeoDescription = "Read the practical getting-started guide online.",
        CanonicalUrl = $"https://acme.example.com/getting-started-part-{i}",
        FeaturedImage = $"featured-{i}.jpg",
        Thumbnail = $"thumb-{i}.jpg",
        ThumbnailUrl = $"https://cdn.example.com/posts/thumb-{i}.jpg",
        Byline = "John Smith",
        BylineHandle = "@johnsmith",
        Language = "en-US",
        Template = "default",
        ThemeColor = "#0A6EBD",
        CoverCaption = "Cover image caption",
        Series = "Getting Started",
        Format = "standard",
        License = "CC-BY-4.0",
        StyleSheet = "",
        DisplayOrder = i,
        ReadingMinutes = 6 + i,
        WordCount = 1200 + i,
        RevisionNumber = 4,
        HeroImagePosition = 0,
        Rating = 4.7m,
        EngagementScore = 116.91m,
        ReadRatio = 0.74m,
        SentimentScore = 0.62m,
        TrendingScore = 24m,
        QualityScore = 12m,
        SeoScore = 8m,
        IsPublished = true,
        IsVisible = true,
        IsSyndicated = false,
        SyncedToCdn = true,
        IsFeatured = false,
        AllowComments = true,
        IsPremium = false,
        CreatedAt = new DateTime(2025, 11, 2),
        UpdatedAt = new DateTime(2026, 6, 1),
        Categories = new List<string> { "guides", "beginner", "tutorials" },
        Tags = new List<string> { "onboarding", "howto", "basics" },
        Attachments = new List<string?> { $"file-{i}-1.pdf", $"file-{i}-2.pdf", null }
    };

    private static VideoPost BuildVideoPost()
    {
        var video = new VideoPost
        {
            VideoUrl = "https://cdn.example.com/videos/starter.mp4",
            Chapters = new List<string> { "Intro", "Setup" }
        };

        // Reuse the standard post shape for the video post's inherited properties.
        var template = BuildPost(9);
        video.Id = "pst_video";
        video.Title = "Starter Video Guide";
        video.Slug = "starter-video-guide";
        video.Subtitle = "Watch and follow along";
        video.Summary = "A video walkthrough of the starter guide.";
        video.Body = template.Body;
        video.BodyHtml = template.BodyHtml;
        video.Excerpt = "Video walkthrough for beginners.";
        video.SeoTitle = "Starter Video Guide - The Acme Journal";
        video.SeoDescription = template.SeoDescription;
        video.CanonicalUrl = "https://acme.example.com/starter-video-guide";
        video.FeaturedImage = template.FeaturedImage;
        video.Thumbnail = template.Thumbnail;
        video.ThumbnailUrl = template.ThumbnailUrl;
        video.Byline = template.Byline;
        video.BylineHandle = template.BylineHandle;
        video.Language = template.Language;
        video.Template = "video";
        video.ThemeColor = template.ThemeColor;
        video.CoverCaption = template.CoverCaption;
        video.Series = template.Series;
        video.Format = "video";
        video.License = template.License;
        video.StyleSheet = template.StyleSheet;
        video.DisplayOrder = 99;
        video.ReadingMinutes = 3;
        video.WordCount = 320;
        video.RevisionNumber = 2;
        video.HeroImagePosition = 0;
        video.Rating = 4.9m;
        video.EngagementScore = 40.20m;
        video.ReadRatio = 0.9m;
        video.SentimentScore = template.SentimentScore;
        video.TrendingScore = 30m;
        video.QualityScore = 20m;
        video.SeoScore = 10m;
        video.IsPublished = true;
        video.IsVisible = true;
        video.IsSyndicated = false;
        video.SyncedToCdn = true;
        video.IsFeatured = false;
        video.AllowComments = false;
        video.IsPremium = false;
        video.CreatedAt = template.CreatedAt;
        video.UpdatedAt = template.UpdatedAt;
        video.Categories = new List<string> { "guides", "video" };
        video.Tags = new List<string> { "video", "starter" };
        video.Attachments = new List<string?> { "transcript.txt" };
        return video;
    }
}
