namespace ReflavorRP.Tests.Accents;

public class LimsaPirateTests
{
    [Theory]
    [InlineData("Hello my friend, how is it going?", "Ahoy me matey, 'ow be it goin'?")]
    [InlineData("Hey friends!", "Ahoy mateys!")]
    [InlineData("Yes, your ship is here", "Aye, yer ship be 'ere")]
    [InlineData("Where are you going?", "Where be ye goin'?")]
    // short -ing words keep their g
    [InlineData("That's nothing, a thing for the king", "That's nothin', a thing fer the king")]
    [InlineData("Cup of tea", "Cup o' tea")]
    [InlineData("How dare you!", "'Ow dare ye!")]
    [InlineData("HOW ARE YOU", "'OW BE YE")]
    public void PirateSpeech(string input, string expected)
    {
        Assert.Equal(expected, Template("Limsa pirate").Apply(input));
    }
}
