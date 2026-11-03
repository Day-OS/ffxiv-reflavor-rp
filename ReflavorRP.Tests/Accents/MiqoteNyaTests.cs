namespace ReflavorRP.Tests.Accents;

public class MiqoteNyaTests
{
    [Theory]
    [InlineData("No, not now", "Nyo, nyot nyow")]
    [InlineData("What a nice night", "What a nyice nyight")]
    [InlineData("I need dinner", "I nyeed dinnyer")]
    [InlineData("NO", "NYO")]
    // n before a consonant or y stays
    [InlineData("and any thing", "and any thing")]
    [InlineData("nya", "nya")]
    public void NBeforeAVowelIsNy(string input, string expected)
    {
        Assert.Equal(expected, Template("Miqo'te nya").Apply(input));
    }
}
