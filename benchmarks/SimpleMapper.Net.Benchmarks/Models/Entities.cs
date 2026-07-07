namespace SimpleMapper.Net.Benchmarks.Models;

// Synthetic domain used by the benchmarks. It mirrors the complexity of a real
// content-platform object graph (~60 mapped properties, 4-5 nesting levels, simple
// and complex collections, a dictionary and a polymorphic subtype) without depending
// on any real-world domain model.

public enum PublishStatus { Draft, Scheduled, Published, Archived, Unlisted }
public enum RevisionStatus { Pending, InReview, Approved, Rejected, Reverted }
public enum AuthorTier { None, Contributor, Editor, Lead }
public enum HighlightScope { Blog, Post, Section }
public enum HighlightKind { Percentage, Fixed }

public class Blog
{
    public string? Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PublishStatus Status { get; set; }
    public string Language { get; set; } = "en-US";
    public decimal StorageUsedMb { get; set; }
    public decimal BandwidthGb { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal TotalCost { get; set; }
    public string Tagline { get; set; } = "";
    public bool IsFeatured { get; set; }
    public int PostCount { get; set; }
    public Author? Owner { get; set; }
    public Location? Headquarters { get; set; }
    public PublishConfig? Publishing { get; set; }
    public Distribution? Distribution { get; set; }
    public Section? Featured { get; set; }
    public IList<Revision>? Revisions { get; set; }
    public List<string> StatusHistory { get; set; } = new();
    public List<string> AuditLog { get; set; } = new();
}

public class Author
{
    public string? Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";
    public string TaxId { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsVerified { get; set; }
    public AuthorTier Tier { get; set; }
    public int ReputationPoints { get; set; }
    public bool NewsletterOptIn { get; set; }
    public string PreferredLanguage { get; set; } = "";
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string Focus { get; set; } = "";
    public decimal TotalRevenue { get; set; }
    public int PostCount { get; set; }
    public DateTime? LastPostAt { get; set; }
    public string Notes { get; set; } = "";
    public List<Location> Locations { get; set; } = new();
    public List<string> Handles { get; set; } = new();
    public List<Badge> Badges { get; set; } = new();
}

public class Location
{
    public string Street { get; set; } = "";
    public string Number { get; set; } = "";
    public string Complement { get; set; } = "";
    public string District { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Country { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Reference { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class Badge
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class Section
{
    public string? Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Description { get; set; } = "";
    public int ItemCount { get; set; }
    public int ViewCount { get; set; }
    public string Layout { get; set; } = "";
    public bool IsPinned { get; set; }
    public string Language { get; set; } = "en-US";
    public int Position { get; set; }
    public bool IsVisible { get; set; }
    public string Theme { get; set; } = "";
    public int Order { get; set; }
    public Highlight? FeaturedHighlight { get; set; }
    public List<SectionEntry> Entries { get; set; } = new();
}

public class SectionEntry
{
    public Post? Post { get; set; }
    public int Position { get; set; }
    public decimal Score { get; set; }
    public decimal Weight { get; set; }
    public Highlight? Highlight { get; set; }
}

public class Highlight
{
    public string Label { get; set; } = "";
    public decimal Weight { get; set; }
    public HighlightScope Scope { get; set; }
    public HighlightKind Kind { get; set; }
}

public class Post
{
    public string? Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Body { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string SeoTitle { get; set; } = "";
    public string SeoDescription { get; set; } = "";
    public string CanonicalUrl { get; set; } = "";
    public string FeaturedImage { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string Byline { get; set; } = "";
    public string BylineHandle { get; set; } = "";
    public string Language { get; set; } = "";
    public string Template { get; set; } = "";
    public string ThemeColor { get; set; } = "";
    public string CoverCaption { get; set; } = "";
    public string Series { get; set; } = "";
    public string Format { get; set; } = "standard";
    public string License { get; set; } = "";
    public string StyleSheet { get; set; } = "";
    public int DisplayOrder { get; set; }
    public int ReadingMinutes { get; set; }
    public int WordCount { get; set; }
    public int RevisionNumber { get; set; }
    public int HeroImagePosition { get; set; }
    public decimal Rating { get; set; }
    public decimal EngagementScore { get; set; }
    public decimal ReadRatio { get; set; }
    public decimal SentimentScore { get; set; }
    public decimal TrendingScore { get; set; }
    public decimal QualityScore { get; set; }
    public decimal SeoScore { get; set; }
    public bool IsPublished { get; set; }
    public bool IsVisible { get; set; }
    public bool IsSyndicated { get; set; }
    public bool SyncedToCdn { get; set; }
    public bool IsFeatured { get; set; }
    public bool AllowComments { get; set; }
    public bool IsPremium { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string?> Attachments { get; set; } = new();
}

public class VideoPost : Post
{
    public string VideoUrl { get; set; } = "";
    public List<string> Chapters { get; set; } = new();
}

public class PublishConfig
{
    public string Channel { get; set; } = "";
    public string Provider { get; set; } = "";
    public int MaxDrafts { get; set; }
    public decimal ScheduleOffset { get; set; }
    public bool AllowScheduling { get; set; }
    public int MaxRevisions { get; set; }
    public decimal MinInterval { get; set; }
    public string DisplayName { get; set; } = "";
    public List<string> CompatibleChannels { get; set; } = new();
    public Dictionary<string, string> Options { get; set; } = new();
}

public class Revision
{
    public string? Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal ChangeSize { get; set; }
    public RevisionStatus Status { get; set; }
    public string CommitHash { get; set; } = "";
    public string RevisionId { get; set; } = "";
    public string ParentReference { get; set; } = "";
    public string Editor { get; set; } = "";
    public string EditorHandle { get; set; } = "";
    public int Number { get; set; }
    public string Summary { get; set; } = "";
    public string ConflictNote { get; set; } = "";
    public decimal RevertedLines { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? RevertedAt { get; set; }
    public string Kind { get; set; } = "";
    public string Language { get; set; } = "en-US";
    public decimal ReviewScore { get; set; }
    public decimal NetChange { get; set; }
    public int Attempt { get; set; }
}

public class Distribution
{
    public string Channel { get; set; } = "";
    public string TrackingCode { get; set; } = "";
    public DateTime? ScheduledAt { get; set; }
    public Location? Location { get; set; }
    public string Notes { get; set; } = "";
}
