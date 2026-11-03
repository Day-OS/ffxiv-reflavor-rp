namespace ReflavorRP.Tests.Accents;

public class HissingTests
{
    [Theory]
    [InlineData("Yes, master", "Yesss, massster")]
    [InlineData("This is nice", "Thisss isss nissse")]
    [InlineData("Shh, she sees us", "Shh, she ssseesss usss")]
    [InlineData("Amazing pizza", "Amazzzing pizzza")]
    [InlineData("YES", "YESSS")]
    public void DrawnOutSibilants(string input, string expected)
    {
        Assert.Equal(expected, Template("Hissing").Apply(input));
    }
}
