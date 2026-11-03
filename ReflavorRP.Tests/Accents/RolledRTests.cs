namespace ReflavorRP.Tests.Accents;

public class RolledRTests
{
    [Theory]
    [InlineData("Run, brother", "Rrun, brrotherr")]
    [InlineData("Sorry", "Sorrry")]
    [InlineData("RUN", "RRUN")]
    [InlineData("Stay close", "Stay close")]
    public void OneMoreR(string input, string expected)
    {
        Assert.Equal(expected, Template("Rolled R").Apply(input));
    }
}
