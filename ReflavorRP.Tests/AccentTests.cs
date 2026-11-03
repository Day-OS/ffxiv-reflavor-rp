namespace ReflavorRP.Tests;

// how accents run, whatever their rules are
public class AccentTests
{
    private static readonly Accent Lisp = Template("Frontal lisp");

    [Fact]
    public void RulesRunInOrder()
    {
        var accent = new Accent
        {
            Rules =
            [
                new() { Pattern = "a", Replacement = "b" },
                new() { Pattern = "b", Replacement = "c" },
            ],
        };

        Assert.Equal("cc", accent.Apply("ab"));
    }

    [Fact]
    public void SkipsBadAndEmptyPatterns()
    {
        var accent = new Accent
        {
            Rules =
            [
                new() { Pattern = "", Replacement = "x" },
                new() { Pattern = "(", Replacement = "x" },
                new() { Pattern = "o", Replacement = "0" },
            ],
        };

        Assert.Equal("f00", accent.Apply("foo"));
    }

    [Theory]
    [InlineData("Sorry", "Thorry")]
    [InlineData("SORRY", "THORRY")]
    [InlineData("McSally", "McThally")]
    [InlineData("Science SCIENCE", "Thienthe THIENTHE")]
    [InlineData("Psychology", "Thychology")]
    [InlineData("PSYCHIC", "THYCHIC")]
    [InlineData("Xylophone", "Thylophone")]
    [InlineData("SCHOOL", "THKOOL")]
    [InlineData("Pizza", "Pitha")]
    [InlineData("SELF-SERVICE", "THELF-THERVITHE")]
    // single letter words don't count as shouting
    [InlineData("I MISS you", "I MITH you")]
    [InlineData("A SIN", "A THIN")]
    // apostrophes are part of the word
    [InlineData("IT'S FINE", "IT'TH FINE")]
    [InlineData("It's fine", "It'th fine")]
    [InlineData("DON’T STOP", "DON’T THTOP")]
    [InlineData("'SUP", "'THUP")]
    [InlineData("Sam's", "Tham'th")]
    public void KeepsCase(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Fact]
    public void ZeroChanceNeverReplaces()
    {
        var accent = new Accent { Rules = [new() { Pattern = "o", Replacement = "0", Chance = 0 }] };

        Assert.Equal("foo", accent.Apply("foo"));
    }

    [Fact]
    public void CopyKeepsChance()
    {
        var copy = Template("Stutter").Copy("Mine");

        Assert.Equal(25, copy.Rules[0].Chance);
    }

    [Fact]
    public void CombinedAccentsRunInOrder()
    {
        var combined = Accent.Combine("Lisp + Moogle", [Lisp, Template("Moogle")]);

        Assert.Equal("Thorry, kupo!", combined.Apply("Sorry!"));
    }

    [Fact]
    public void CombineOrderMatters()
    {
        var drunk = Template("Drunk");

        // the lisp gets to the s first, so the drunk rule has nothing left to slur
        Assert.Equal("Thorry", Accent.Combine("", [Lisp, drunk]).Apply("Sorry"));
        // the drunk sh comes first, and the lisp leaves sh alone
        Assert.Equal("Shorry", Accent.Combine("", [drunk, Lisp]).Apply("Sorry"));
    }

    [Fact]
    public void CopyOfCombinedIsIndependent()
    {
        var uwu = Template("Uwufication");
        var copy = Accent.Combine("", [Lisp, uwu]).Copy("Mine");
        copy.Rules[0].Pattern = "changed";

        Assert.NotEqual("changed", Lisp.Rules[0].Pattern);
        Assert.Equal(Lisp.Rules.Count + uwu.Rules.Count, copy.Rules.Count);
    }
}
