namespace ReflavorRP.Tests.Accents;

public class ToughTalkTests
{
    [Theory]
    [InlineData("This is the thing", "Dis is de ting")]
    [InlineData("Them and their brother", "Dem and deir broder")]
    [InlineData("I think that they are with us", "I tink dat dey are wit us")]
    [InlineData("They're there", "Dey're dere")]
    [InlineData("Together with my mother", "Togeder wit my moder")]
    // th at the start of a word before er is voiceless
    [InlineData("Thermal therapy", "Termal terapy")]
    [InlineData("Both of those", "Bot of dose")]
    public void ThStopping(string input, string expected)
    {
        Assert.Equal(expected, Template("Tough talk").Apply(input));
    }
}
