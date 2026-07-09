using SimpleMapper.Net.Benchmarks.Models;

namespace SimpleMapper.Net.Benchmarks;

/// <summary>
/// Hand-written baseline. Mirrors SimpleMapper.Net's published semantics so the
/// numbers stay comparable: nested objects and collection items are new instances
/// (deep copy), dictionaries are copied by reference, and the polymorphic video
/// post is resolved by runtime type check. This is the floor every mapper is
/// paying convenience against.
/// </summary>
public static class ManualMapper
{
    public static BlogDto ToDto(Blog src) => new()
    {
        Id = src.Id,
        Title = src.Title,
        Slug = src.Slug,
        CreatedAt = src.CreatedAt,
        UpdatedAt = src.UpdatedAt,
        Status = src.Status,
        Language = src.Language,
        StorageUsedMb = src.StorageUsedMb,
        BandwidthGb = src.BandwidthGb,
        MonthlyBudget = src.MonthlyBudget,
        TotalCost = src.TotalCost,
        Tagline = src.Tagline,
        IsFeatured = src.IsFeatured,
        PostCount = src.PostCount,
        Owner = src.Owner is null ? null : ToDto(src.Owner),
        Headquarters = src.Headquarters is null ? null : ToDto(src.Headquarters),
        Publishing = src.Publishing is null ? null : ToDto(src.Publishing),
        Distribution = src.Distribution is null ? null : ToDto(src.Distribution),
        Featured = src.Featured is null ? null : ToDto(src.Featured),
        Revisions = src.Revisions?.Select(ToDto).ToList(),
        StatusHistory = new List<string>(src.StatusHistory),
        AuditLog = new List<string>(src.AuditLog),
    };

    public static Blog ToEntity(BlogDto src) => new()
    {
        Id = src.Id,
        Title = src.Title,
        Slug = src.Slug,
        CreatedAt = src.CreatedAt,
        UpdatedAt = src.UpdatedAt,
        Status = src.Status,
        Language = src.Language,
        StorageUsedMb = src.StorageUsedMb,
        BandwidthGb = src.BandwidthGb,
        MonthlyBudget = src.MonthlyBudget,
        TotalCost = src.TotalCost,
        Tagline = src.Tagline,
        IsFeatured = src.IsFeatured,
        PostCount = src.PostCount,
        Owner = src.Owner is null ? null : ToEntity(src.Owner),
        Headquarters = src.Headquarters is null ? null : ToEntity(src.Headquarters),
        Publishing = src.Publishing is null ? null : ToEntity(src.Publishing),
        Distribution = src.Distribution is null ? null : ToEntity(src.Distribution),
        Featured = src.Featured is null ? null : ToEntity(src.Featured),
        Revisions = src.Revisions?.Select(ToEntity).ToList(),
        StatusHistory = new List<string>(src.StatusHistory),
        AuditLog = new List<string>(src.AuditLog),
    };

    public static AuthorDto ToDto(Author src) => new()
    {
        Id = src.Id,
        FirstName = src.FirstName,
        LastName = src.LastName,
        Email = src.Email,
        Website = src.Website,
        TaxId = src.TaxId,
        BirthDate = src.BirthDate,
        JoinedAt = src.JoinedAt,
        IsVerified = src.IsVerified,
        Tier = src.Tier,
        ReputationPoints = src.ReputationPoints,
        NewsletterOptIn = src.NewsletterOptIn,
        PreferredLanguage = src.PreferredLanguage,
        Company = src.Company,
        Role = src.Role,
        Focus = src.Focus,
        TotalRevenue = src.TotalRevenue,
        PostCount = src.PostCount,
        LastPostAt = src.LastPostAt,
        Notes = src.Notes,
        Locations = src.Locations.Select(ToDto).ToList(),
        Handles = new List<string>(src.Handles),
        Badges = src.Badges.Select(ToDto).ToList(),
    };

    public static Author ToEntity(AuthorDto src) => new()
    {
        Id = src.Id,
        FirstName = src.FirstName,
        LastName = src.LastName,
        Email = src.Email,
        Website = src.Website,
        TaxId = src.TaxId,
        BirthDate = src.BirthDate,
        JoinedAt = src.JoinedAt,
        IsVerified = src.IsVerified,
        Tier = src.Tier,
        ReputationPoints = src.ReputationPoints,
        NewsletterOptIn = src.NewsletterOptIn,
        PreferredLanguage = src.PreferredLanguage,
        Company = src.Company,
        Role = src.Role,
        Focus = src.Focus,
        TotalRevenue = src.TotalRevenue,
        PostCount = src.PostCount,
        LastPostAt = src.LastPostAt,
        Notes = src.Notes,
        Locations = src.Locations.Select(ToEntity).ToList(),
        Handles = new List<string>(src.Handles),
        Badges = src.Badges.Select(ToEntity).ToList(),
    };

    public static LocationDto ToDto(Location src) => new()
    {
        Street = src.Street,
        Number = src.Number,
        Complement = src.Complement,
        District = src.District,
        City = src.City,
        State = src.State,
        Country = src.Country,
        ZipCode = src.ZipCode,
        Reference = src.Reference,
        Latitude = src.Latitude,
        Longitude = src.Longitude,
    };

    public static Location ToEntity(LocationDto src) => new()
    {
        Street = src.Street,
        Number = src.Number,
        Complement = src.Complement,
        District = src.District,
        City = src.City,
        State = src.State,
        Country = src.Country,
        ZipCode = src.ZipCode,
        Reference = src.Reference,
        Latitude = src.Latitude,
        Longitude = src.Longitude,
    };

    public static BadgeDto ToDto(Badge src) => new() { Name = src.Name, Count = src.Count };

    public static Badge ToEntity(BadgeDto src) => new() { Name = src.Name, Count = src.Count };

    public static SectionDto ToDto(Section src) => new()
    {
        Id = src.Id,
        CreatedAt = src.CreatedAt,
        Title = src.Title,
        Slug = src.Slug,
        Description = src.Description,
        ItemCount = src.ItemCount,
        ViewCount = src.ViewCount,
        Layout = src.Layout,
        IsPinned = src.IsPinned,
        Language = src.Language,
        Position = src.Position,
        IsVisible = src.IsVisible,
        Theme = src.Theme,
        Order = src.Order,
        FeaturedHighlight = src.FeaturedHighlight is null ? null : ToDto(src.FeaturedHighlight),
        Entries = src.Entries.Select(ToDto).ToList(),
    };

    public static Section ToEntity(SectionDto src) => new()
    {
        Id = src.Id,
        CreatedAt = src.CreatedAt,
        Title = src.Title,
        Slug = src.Slug,
        Description = src.Description,
        ItemCount = src.ItemCount,
        ViewCount = src.ViewCount,
        Layout = src.Layout,
        IsPinned = src.IsPinned,
        Language = src.Language,
        Position = src.Position,
        IsVisible = src.IsVisible,
        Theme = src.Theme,
        Order = src.Order,
        FeaturedHighlight = src.FeaturedHighlight is null ? null : ToEntity(src.FeaturedHighlight),
        Entries = src.Entries.Select(ToEntity).ToList(),
    };

    public static SectionEntryDto ToDto(SectionEntry src) => new()
    {
        Post = src.Post is null ? null : ToDto(src.Post),
        Position = src.Position,
        Score = src.Score,
        Weight = src.Weight,
        Highlight = src.Highlight is null ? null : ToDto(src.Highlight),
    };

    public static SectionEntry ToEntity(SectionEntryDto src) => new()
    {
        Post = src.Post is null ? null : ToEntity(src.Post),
        Position = src.Position,
        Score = src.Score,
        Weight = src.Weight,
        Highlight = src.Highlight is null ? null : ToEntity(src.Highlight),
    };

    public static HighlightDto ToDto(Highlight src) => new()
    {
        Label = src.Label,
        Weight = src.Weight,
        Scope = src.Scope,
        Kind = src.Kind,
    };

    public static Highlight ToEntity(HighlightDto src) => new()
    {
        Label = src.Label,
        Weight = src.Weight,
        Scope = src.Scope,
        Kind = src.Kind,
    };

    public static PostDto ToDto(Post src)
    {
        // Polymorphic pair resolved by runtime type, like the mappers do.
        var dto = src is VideoPost video
            ? new VideoPostDto { VideoUrl = video.VideoUrl, Chapters = new List<string>(video.Chapters) }
            : new PostDto();
        CopyPost(src, dto);
        return dto;
    }

    public static Post ToEntity(PostDto src)
    {
        var entity = src is VideoPostDto video
            ? new VideoPost { VideoUrl = video.VideoUrl, Chapters = new List<string>(video.Chapters) }
            : new Post();
        CopyPost(src, entity);
        return entity;
    }

    private static void CopyPost(Post src, PostDto tgt)
    {
        tgt.Id = src.Id;
        tgt.Title = src.Title;
        tgt.Slug = src.Slug;
        tgt.Subtitle = src.Subtitle;
        tgt.Summary = src.Summary;
        tgt.Body = src.Body;
        tgt.BodyHtml = src.BodyHtml;
        tgt.Excerpt = src.Excerpt;
        tgt.SeoTitle = src.SeoTitle;
        tgt.SeoDescription = src.SeoDescription;
        tgt.CanonicalUrl = src.CanonicalUrl;
        tgt.FeaturedImage = src.FeaturedImage;
        tgt.Thumbnail = src.Thumbnail;
        tgt.ThumbnailUrl = src.ThumbnailUrl;
        tgt.Byline = src.Byline;
        tgt.BylineHandle = src.BylineHandle;
        tgt.Language = src.Language;
        tgt.Template = src.Template;
        tgt.ThemeColor = src.ThemeColor;
        tgt.CoverCaption = src.CoverCaption;
        tgt.Series = src.Series;
        tgt.Format = src.Format;
        tgt.License = src.License;
        tgt.StyleSheet = src.StyleSheet;
        tgt.DisplayOrder = src.DisplayOrder;
        tgt.ReadingMinutes = src.ReadingMinutes;
        tgt.WordCount = src.WordCount;
        tgt.RevisionNumber = src.RevisionNumber;
        tgt.HeroImagePosition = src.HeroImagePosition;
        tgt.Rating = src.Rating;
        tgt.EngagementScore = src.EngagementScore;
        tgt.ReadRatio = src.ReadRatio;
        tgt.SentimentScore = src.SentimentScore;
        tgt.TrendingScore = src.TrendingScore;
        tgt.QualityScore = src.QualityScore;
        tgt.SeoScore = src.SeoScore;
        tgt.IsPublished = src.IsPublished;
        tgt.IsVisible = src.IsVisible;
        tgt.IsSyndicated = src.IsSyndicated;
        tgt.SyncedToCdn = src.SyncedToCdn;
        tgt.IsFeatured = src.IsFeatured;
        tgt.AllowComments = src.AllowComments;
        tgt.IsPremium = src.IsPremium;
        tgt.CreatedAt = src.CreatedAt;
        tgt.UpdatedAt = src.UpdatedAt;
        tgt.Categories = new List<string>(src.Categories);
        tgt.Tags = new List<string>(src.Tags);
        tgt.Attachments = new List<string?>(src.Attachments);
    }

    private static void CopyPost(PostDto src, Post tgt)
    {
        tgt.Id = src.Id;
        tgt.Title = src.Title;
        tgt.Slug = src.Slug;
        tgt.Subtitle = src.Subtitle;
        tgt.Summary = src.Summary;
        tgt.Body = src.Body;
        tgt.BodyHtml = src.BodyHtml;
        tgt.Excerpt = src.Excerpt;
        tgt.SeoTitle = src.SeoTitle;
        tgt.SeoDescription = src.SeoDescription;
        tgt.CanonicalUrl = src.CanonicalUrl;
        tgt.FeaturedImage = src.FeaturedImage;
        tgt.Thumbnail = src.Thumbnail;
        tgt.ThumbnailUrl = src.ThumbnailUrl;
        tgt.Byline = src.Byline;
        tgt.BylineHandle = src.BylineHandle;
        tgt.Language = src.Language;
        tgt.Template = src.Template;
        tgt.ThemeColor = src.ThemeColor;
        tgt.CoverCaption = src.CoverCaption;
        tgt.Series = src.Series;
        tgt.Format = src.Format;
        tgt.License = src.License;
        tgt.StyleSheet = src.StyleSheet;
        tgt.DisplayOrder = src.DisplayOrder;
        tgt.ReadingMinutes = src.ReadingMinutes;
        tgt.WordCount = src.WordCount;
        tgt.RevisionNumber = src.RevisionNumber;
        tgt.HeroImagePosition = src.HeroImagePosition;
        tgt.Rating = src.Rating;
        tgt.EngagementScore = src.EngagementScore;
        tgt.ReadRatio = src.ReadRatio;
        tgt.SentimentScore = src.SentimentScore;
        tgt.TrendingScore = src.TrendingScore;
        tgt.QualityScore = src.QualityScore;
        tgt.SeoScore = src.SeoScore;
        tgt.IsPublished = src.IsPublished;
        tgt.IsVisible = src.IsVisible;
        tgt.IsSyndicated = src.IsSyndicated;
        tgt.SyncedToCdn = src.SyncedToCdn;
        tgt.IsFeatured = src.IsFeatured;
        tgt.AllowComments = src.AllowComments;
        tgt.IsPremium = src.IsPremium;
        tgt.CreatedAt = src.CreatedAt;
        tgt.UpdatedAt = src.UpdatedAt;
        tgt.Categories = new List<string>(src.Categories);
        tgt.Tags = new List<string>(src.Tags);
        tgt.Attachments = new List<string?>(src.Attachments);
    }

    public static PublishConfigDto ToDto(PublishConfig src) => new()
    {
        Channel = src.Channel,
        Provider = src.Provider,
        MaxDrafts = src.MaxDrafts,
        ScheduleOffset = src.ScheduleOffset,
        AllowScheduling = src.AllowScheduling,
        MaxRevisions = src.MaxRevisions,
        MinInterval = src.MinInterval,
        DisplayName = src.DisplayName,
        CompatibleChannels = new List<string>(src.CompatibleChannels),
        Options = src.Options, // by reference, matching SimpleMapper's documented dictionary semantics
    };

    public static PublishConfig ToEntity(PublishConfigDto src) => new()
    {
        Channel = src.Channel,
        Provider = src.Provider,
        MaxDrafts = src.MaxDrafts,
        ScheduleOffset = src.ScheduleOffset,
        AllowScheduling = src.AllowScheduling,
        MaxRevisions = src.MaxRevisions,
        MinInterval = src.MinInterval,
        DisplayName = src.DisplayName,
        CompatibleChannels = new List<string>(src.CompatibleChannels),
        Options = src.Options, // by reference, matching SimpleMapper's documented dictionary semantics
    };

    public static RevisionDto ToDto(Revision src) => new()
    {
        Id = src.Id,
        CreatedAt = src.CreatedAt,
        ChangeSize = src.ChangeSize,
        Status = src.Status,
        CommitHash = src.CommitHash,
        RevisionId = src.RevisionId,
        ParentReference = src.ParentReference,
        Editor = src.Editor,
        EditorHandle = src.EditorHandle,
        Number = src.Number,
        Summary = src.Summary,
        ConflictNote = src.ConflictNote,
        RevertedLines = src.RevertedLines,
        PublishedAt = src.PublishedAt,
        RevertedAt = src.RevertedAt,
        Kind = src.Kind,
        Language = src.Language,
        ReviewScore = src.ReviewScore,
        NetChange = src.NetChange,
        Attempt = src.Attempt,
    };

    public static Revision ToEntity(RevisionDto src) => new()
    {
        Id = src.Id,
        CreatedAt = src.CreatedAt,
        ChangeSize = src.ChangeSize,
        Status = src.Status,
        CommitHash = src.CommitHash,
        RevisionId = src.RevisionId,
        ParentReference = src.ParentReference,
        Editor = src.Editor,
        EditorHandle = src.EditorHandle,
        Number = src.Number,
        Summary = src.Summary,
        ConflictNote = src.ConflictNote,
        RevertedLines = src.RevertedLines,
        PublishedAt = src.PublishedAt,
        RevertedAt = src.RevertedAt,
        Kind = src.Kind,
        Language = src.Language,
        ReviewScore = src.ReviewScore,
        NetChange = src.NetChange,
        Attempt = src.Attempt,
    };

    public static DistributionDto ToDto(Distribution src) => new()
    {
        Channel = src.Channel,
        TrackingCode = src.TrackingCode,
        ScheduledAt = src.ScheduledAt,
        Location = src.Location is null ? null : ToDto(src.Location),
        Notes = src.Notes,
    };

    public static Distribution ToEntity(DistributionDto src) => new()
    {
        Channel = src.Channel,
        TrackingCode = src.TrackingCode,
        ScheduledAt = src.ScheduledAt,
        Location = src.Location is null ? null : ToEntity(src.Location),
        Notes = src.Notes,
    };

    public static CustomerDto ToDto(Customer src) => new()
    {
        Id = src.Id,
        FirstName = src.FirstName,
        LastName = src.LastName,
        Email = src.Email,
        CreatedAt = src.CreatedAt,
        IsActive = src.IsActive,
        LoyaltyPoints = src.LoyaltyPoints,
        CreditLimit = src.CreditLimit,
    };

    public static void Update(CustomerDto src, Customer tgt)
    {
        tgt.Id = src.Id;
        tgt.FirstName = src.FirstName;
        tgt.LastName = src.LastName;
        tgt.Email = src.Email;
        tgt.CreatedAt = src.CreatedAt;
        tgt.IsActive = src.IsActive;
        tgt.LoyaltyPoints = src.LoyaltyPoints;
        tgt.CreditLimit = src.CreditLimit;
    }
}
