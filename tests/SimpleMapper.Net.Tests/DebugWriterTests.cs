using SimpleMapper.Net;

namespace SimpleMapper.Net.Tests;

/// <summary>
/// WithDebugLogging(TextWriter): the mapping tree must land on the given writer
/// as plain text, making the diagnostic usable in tests and server logs.
/// </summary>
public sealed class DebugWriterTests
{
    private class Inner { public string Value { get; set; } = ""; }

    private class Source
    {
        public string Name { get; set; } = "";
        public Inner? Child { get; set; }
    }

    private class Target
    {
        public string Name { get; set; } = "";
        public Inner? Child { get; set; }
    }

    [Fact]
    public void WithDebugLogging_Writer_WritesMappingTree()
    {
        var writer = new StringWriter();
        var src = new Source { Name = "julian", Child = new Inner { Value = "v" } };

        var dto = src.Map()
            .WithDebugLogging(writer)
            .To<Target>();

        var output = writer.ToString();
        Assert.Contains("Name", output);
        Assert.Contains("Child", output);
        Assert.Contains("->", output);
        Assert.Equal("julian", dto.Name);
        Assert.Equal("v", dto.Child!.Value);
    }

    [Fact]
    public void WithDebugLogging_Writer_MarksIgnoredProperties()
    {
        var writer = new StringWriter();
        var src = new Source { Name = "julian" };

        src.Map()
            .Ignore(nameof(Source.Name))
            .WithDebugLogging(writer)
            .To<Target>();

        Assert.Contains("(ign)", writer.ToString());
    }

    [Fact]
    public void WithDebugLogging_NullWriter_Throws()
    {
        var src = new Source();

        Assert.Throws<ArgumentNullException>(() => src.Map().WithDebugLogging(null!));
    }
}
