namespace ReflavorRP.Tests.Accents;

public class UwuficationTests
{
    [Theory]
    [InlineData("Hello", "Hewwo")]
    [InlineData("Really", "Weawwy")]
    [InlineData("love", "wuv")]
    [InlineData("LOVE", "WUV")]
    [InlineData("nice", "nyice")]
    [InlineData("No", "Nyo")]
    public void UwuSounds(string input, string expected)
    {
        Assert.Equal(expected, Template("Uwufication").Apply(input));
    }
}
