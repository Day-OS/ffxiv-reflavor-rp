namespace ReflavorRP.Tests.Accents;

public class StuffyNoseTests
{
    [Theory]
    [InlineData("My nose is stuffed", "By dose is stuffed")]
    [InlineData("Good morning", "Good bordig")]
    [InlineData("I know nothing", "I dow dothig")]
    [InlineData("Summer dinner", "Subber didder")]
    public void BlockedNasals(string input, string expected)
    {
        Assert.Equal(expected, Template("Stuffy nose").Apply(input));
    }
}
