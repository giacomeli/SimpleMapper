using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

public sealed class DeepPathTests
{
    // ---- Test models (blog domain) ----

    private class Account
    {
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public string TaxId { get; set; } = "";
        public string Document { get; set; } = "";
    }

    private class AccountDto
    {
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public string TaxId { get; set; } = "";
        public string Document { get; set; } = "";
    }

    private class Media
    {
        public string CoverUrl { get; set; } = "";
        public List<string>? Thumbnails { get; set; }
    }

    private class MediaDto
    {
        public string CoverUrl { get; set; } = "";
        public List<string>? Thumbnails { get; set; }
    }

    private class Article
    {
        public string Title { get; set; } = "";
        public Media Media { get; set; } = new();
    }

    private class ArticleDto
    {
        public string Title { get; set; } = "";
        public MediaDto Media { get; set; } = new();
    }

    private class User
    {
        public Account Account { get; set; } = new();
        public List<Article> Articles { get; set; } = new();
        public int PostCount { get; set; }
        public string InternalNotes { get; set; } = "";
    }

    private class UserDto
    {
        public AccountDto Account { get; set; } = new();
        public List<ArticleDto> Articles { get; set; } = new();
        public int PostCount { get; set; }
        public string InternalNotes { get; set; } = "";
    }

    // ---- Tests ----

    [Fact]
    public void DeepIgnore_SingleNestedProperty()
    {
        var user = new User { Account = new Account { Name = "John", Password = "secret" } };

        var dto = user.Map<User>()
            .Ignore(x => x.Account.Password)
            .To<UserDto>();

        Assert.Equal("John", dto.Account.Name);
        Assert.Equal("", dto.Account.Password); // default, not "secret"
    }

    [Fact]
    public void DeepIgnore_ThroughCollection_WithEach()
    {
        var user = new User
        {
            Articles = new List<Article>
            {
                new() { Title = "Intro", Media = new Media { Thumbnails = new List<string> { "img1.jpg" } } },
                new() { Title = "Deep Dive", Media = new Media { Thumbnails = new List<string> { "img2.jpg" } } },
            }
        };

        var dto = user.Map<User>()
            .Ignore(x => x.Articles.Each().Media.Thumbnails!)
            .To<UserDto>();

        Assert.Equal(2, dto.Articles.Count);
        Assert.Equal("Intro", dto.Articles[0].Title);
        Assert.Equal("Deep Dive", dto.Articles[1].Title);
        Assert.Null(dto.Articles[0].Media.Thumbnails); // ignored
        Assert.Null(dto.Articles[1].Media.Thumbnails); // ignored
    }

    [Fact]
    public void DeepIgnore_EntireCollection_WithoutEach()
    {
        var user = new User
        {
            PostCount = 100,
            Articles = new List<Article>
            {
                new() { Title = "Intro" },
            }
        };

        var dto = user.Map<User>()
            .Ignore(x => x.Articles)
            .To<UserDto>();

        Assert.Equal(100, dto.PostCount);
        Assert.Empty(dto.Articles); // collection ignored (default empty list from constructor)
    }

    [Fact]
    public void DeepMap_RenameNestedProperty()
    {
        var user = new User
        {
            Account = new Account { Name = "John", TaxId = "12345678901" }
        };

        var dto = user.Map<User>()
            .Map(x => x.Account.TaxId, x => x.Account.Document)
            .To<UserDto>();

        Assert.Equal("John", dto.Account.Name);
        Assert.Equal("12345678901", dto.Account.Document);
    }

    [Fact]
    public void FlatIgnore_StillWorks_BackwardsCompat()
    {
        var user = new User { InternalNotes = "internal" };

        var dto = user.Map<User>()
            .Ignore("InternalNotes")
            .To<UserDto>();

        Assert.Equal("", dto.InternalNotes);
    }

    [Fact]
    public void FlatMap_StillWorks_BackwardsCompat()
    {
        var account = new Account { TaxId = "123" };

        var dto = account.Map<Account>()
            .Map("TaxId", "Document")
            .To<AccountDto>();

        Assert.Equal("123", dto.Document);
    }

    [Fact]
    public void MixFlatAndDeep()
    {
        var user = new User
        {
            InternalNotes = "internal",
            Account = new Account { Name = "John", Password = "secret", TaxId = "123" },
            Articles = new List<Article>
            {
                new() { Title = "Intro", Media = new Media { Thumbnails = new List<string> { "img.jpg" } } },
            }
        };

        var dto = user.Map<User>()
            .Ignore("InternalNotes")                              // flat
            .Ignore(x => x.Account.Password)                      // deep
            .Ignore(x => x.Articles.Each().Media.Thumbnails!)     // deep + Each
            .Map(x => x.Account.TaxId, x => x.Account.Document)   // deep map
            .To<UserDto>();

        Assert.Equal("", dto.InternalNotes);                     // flat ignore
        Assert.Equal("John", dto.Account.Name);                  // not ignored
        Assert.Equal("", dto.Account.Password);                  // deep ignore
        Assert.Equal("123", dto.Account.Document);               // deep map
        Assert.Equal("Intro", dto.Articles[0].Title);            // not ignored
        Assert.Null(dto.Articles[0].Media.Thumbnails);           // deep ignore via Each
    }

    [Fact]
    public void DefaultConfig_IsEmpty()
    {
        Assert.True(MappingConfig.Default.IsEmpty);
    }
}
