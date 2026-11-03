namespace ReflavorRP.Tests.Accents;

public class DrunkTests
{
    [Theory]
    [InlineData("This is the best night", "Thish ish the besht night")]
    [InlineData("I'm going home.", "I'm goin' home.")]
    [InlineData("She sells sea shells", "She shellsh shea shellsh")]
    [InlineData("Nothing is wrong!", "Nothin' ish wrong!")]
    public void SlurredSpeech(string input, string expected)
    {
        Assert.Equal(expected, Template("Drunk").Apply(input));
    }
}
