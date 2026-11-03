namespace ReflavorRP.Tests.Accents;

public class StutterTests
{
    [Theory]
    [InlineData("I don't know", "I-I d-don't k-know")]
    [InlineData("What is this", "Wh-What i-is th-this")]
    [InlineData("WHAT", "WH-WHAT")]
    [InlineData("Shut up, Phil", "Sh-Shut u-up, Ph-Phil")]
    public void StuttersAtTheStartOfWords(string input, string expected)
    {
        // always stutter, so the result is predictable
        var stutter = Template("Stutter").Copy("Always stutter");
        stutter.Rules[0].Chance = 100;

        Assert.Equal(expected, stutter.Apply(input));
    }

    [Fact]
    public void OnlyHitsSomeWords()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 1000));

        var result = Template("Stutter").Apply(words, new Random(1));

        var stuttered = result.Split(' ').Count(w => w == "w-word");
        Assert.InRange(stuttered, 200, 300);
    }
}
