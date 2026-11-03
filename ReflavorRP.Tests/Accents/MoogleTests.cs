namespace ReflavorRP.Tests.Accents;

public class MoogleTests
{
    [Theory]
    [InlineData("I found it!", "I found it, kupo!")]
    [InlineData("hello", "hello, kupo")]
    [InlineData("Hi. How are you?", "Hi, kupo. How are you, kupo?")]
    [InlineData("Wait... what", "Wait, kupo... what, kupo")]
    [InlineData("HELP ME!", "HELP ME, KUPO!")]
    // no double kupo
    [InlineData("Thanks, kupo!", "Thanks, kupo!")]
    public void KupoAfterEverySentence(string input, string expected)
    {
        Assert.Equal(expected, Template("Moogle").Apply(input));
    }
}
