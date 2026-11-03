namespace ReflavorRP.Tests.Accents;

public class IshgardianNobleTests
{
    [Theory]
    [InlineData("Are you well?", "Art thou well?")]
    [InlineData("You are brave.", "Thou art brave.")]
    [InlineData("you're late", "thou art late")]
    [InlineData("Do you know him?", "Dost thou know him?")]
    [InlineData("Have you eaten?", "Hast thou eaten?")]
    [InlineData("Will you come?", "Wilt thou come?")]
    // you at the end of a clause is thee
    [InlineData("Thank you.", "Thank thee.")]
    [InlineData("I love you", "I love thee")]
    [InlineData("Your eyes, your blade, yours, yourself", "Thine eyes, thy blade, thine, thyself")]
    [InlineData("It's fine. It is done.", "'Tis fine. 'Tis done.")]
    [InlineData("She does, he has", "She doth, he hath")]
    [InlineData("Hello there!", "Hail there!")]
    [InlineData("Yes, perhaps it is often so", "Aye, perchance 'tis oft so")]
    [InlineData("ARE YOU MAD?", "ART THOU MAD?")]
    public void ArchaicSpeech(string input, string expected)
    {
        Assert.Equal(expected, Template("Ishgardian noble").Apply(input));
    }
}
