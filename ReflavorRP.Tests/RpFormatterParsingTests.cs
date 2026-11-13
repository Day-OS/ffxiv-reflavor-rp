using Dalamud.Game.Text;

namespace ReflavorRP.Tests;

public class RpFormatterParsingTests
{
    [Theory]
    [InlineData("> hello")]
    [InlineData("hello >>")]
    [InlineData("1/2 hello")]
    [InlineData("+ hello")]
    [InlineData("(cont.) hello")]
    public void PreservesLiteralPrefixesAndSuffixes(string input)
    {
        Assert.Equal([new RpTextSegment(RpTextKind.Speech, input)],
            RpFormatter.Parse(input, RpTextKind.Speech, new Configuration { ParenthesesAreOoc = false }));
    }

    [Theory]
    [InlineData("Hello *waves* Bye")]
    [InlineData("Hello [waves] Bye")]
    [InlineData("\"Hello\" waves \"Bye\"")]
    public void ParsesMarkedContent(string input)
    {
        Assert.Equal(new[]
        {
            new RpTextSegment(RpTextKind.Speech, "Hello"),
            new RpTextSegment(RpTextKind.Action, "waves"),
            new RpTextSegment(RpTextKind.Speech, "Bye"),
        }, RpFormatter.Parse(input, RpTextKind.Speech, new Configuration()));
    }

    [Theory]
    [InlineData("Hello. *waves* Bye.")]
    [InlineData("Hello. **waves** Bye.")]
    [InlineData("Hello. [waves] Bye.")]
    [InlineData("Hello. {waves} Bye.")]
    [InlineData("Hello. <waves> Bye.")]
    [InlineData("\"Hello.\" waves \"Bye.\"")]
    [InlineData("“Hello.” waves «Bye.»")]
    [InlineData("Hello. [waves] \"Bye.\"")]
    [InlineData("\"Hello.\" [waves] Bye.")]
    [InlineData("- Hello. *waves* - Bye. -")]
    [InlineData("— Hello. [waves] — Bye. —")]
    [InlineData("— Hello. — waves — Bye.")]
    public void EquivalentNotationProducesTheSameOrderedContent(string input)
    {
        Assert.Equal(new[]
        {
            new RpTextSegment(RpTextKind.Speech, "Hello."),
            new RpTextSegment(RpTextKind.Action, "waves"),
            new RpTextSegment(RpTextKind.Speech, "Bye."),
        }, RpFormatter.Parse(input, RpTextKind.Speech, new Configuration()));
    }

    [Theory]
    [InlineData("(( brb ))")]
    [InlineData("// brb")]
    [InlineData("brb //")]
    [InlineData("OOC: brb")]
    [InlineData("[OOC: brb]")]
    public void RecognizesExplicitOoc(string input)
    {
        Assert.Equal([new RpTextSegment(RpTextKind.Ooc, "brb")],
            RpFormatter.Parse(input, RpTextKind.Speech, new Configuration()));
    }

    [Theory]
    [InlineData("Hello (friend) <t> <se.1>")]
    [InlineData("See https://example.org/rp")]
    [InlineData("ul'dahn and Raih'li")]
    [InlineData("\"I *really* [mean] it\"", "I *really* [mean] it")]
    public void PreservesLiteralSpeech(string input, string? expected = null)
    {
        Assert.Equal([new RpTextSegment(RpTextKind.Speech, expected ?? input)],
            RpFormatter.Parse(input, RpTextKind.Speech, new Configuration { ParenthesesAreOoc = false }));
    }

    [Fact]
    public void KeepsOocSeparateFromSpeechAndActions()
    {
        Assert.Equal(new[]
        {
            new RpTextSegment(RpTextKind.Speech, "Hello"),
            new RpTextSegment(RpTextKind.Action, "waves"),
            new RpTextSegment(RpTextKind.Ooc, "brb"),
        }, RpFormatter.Parse("Hello [waves] (( brb ))", RpTextKind.Speech, new Configuration()));
    }

    [Fact]
    public void AppliesAmbiguousNotationOnlyAsConfigured()
    {
        var config = new Configuration { BracketsAreOoc = true, ParenthesesAreOoc = true };
        Assert.Equal(new[]
        {
            new RpTextSegment(RpTextKind.Ooc, "brb"),
            new RpTextSegment(RpTextKind.Ooc, "afk"),
        }, RpFormatter.Parse("[brb] (afk)", RpTextKind.Speech, config));
        Assert.Equal([new RpTextSegment(RpTextKind.Ooc, "Hello [waves]")],
            RpFormatter.Parse("Hello [waves]", RpTextKind.Ooc, config));
    }

    [Fact]
    public void AppliesTheSingleOocChannel()
    {
        var config = new Configuration { AlwaysOocChannel = XivChatType.Party };
        Assert.Equal("/party (( brb ))", RpFormatter.Rewrite("/party brb", 1, config, "Test Player"));
        Assert.Equal("/cwl8 \"Waves.\"", RpFormatter.Rewrite("/cwl8 waves", 1, config, "Test Player"));
        Assert.Equal("/say \"Hello.\"", RpFormatter.Rewrite("hello", 1, config));
    }

    [Fact]
    public void AccentsOnlyTransformSpeechInMixedNotation()
    {
        var config = new Configuration { ActiveAccents = ["Uwufication"] };
        Assert.Equal("/say \"Weawwy.\" rolls. (( really ))",
            RpFormatter.Rewrite("/say really [rolls] (( really ))", 1, config));
        Assert.Equal("/say (( really ))", RpFormatter.Rewrite("// really", 1, config));
    }
}
