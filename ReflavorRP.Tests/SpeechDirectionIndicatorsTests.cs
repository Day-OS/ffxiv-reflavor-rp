using System.Numerics;

namespace ReflavorRP.Tests;

public class SpeechDirectionIndicatorsTests
{
    [Theory]
    [InlineData(-5, -10, 1, 0)]
    [InlineData(5, -10, 0, 1)]
    [InlineData(-5, 10, 1, 0)]
    [InlineData(5, 10, 0, 1)]
    [InlineData(0, 10, 1, 1)]
    [InlineData(0, -10, 0, 0)]
    public void DirectionDoesNotReverseForSpeakersBehindTheCamera(float horizontal, float depth,
        float left, float right)
    {
        Assert.Equal(new Vector2(left, right), SpeechDirectionIndicators.GetSides(horizontal, depth));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(90, 0.5f)]
    [InlineData(180, 1)]
    [InlineData(3000, 1)]
    [InlineData(3500, 0.5f)]
    [InlineData(4000, 0)]
    [InlineData(5000, 0)]
    public void CueFadesInAndOutThenExpires(long age, float expected)
    {
        Assert.Equal(expected, SpeechDirectionIndicators.GetTimeStrength(age), 5);
    }

    [Fact]
    public void GradientIsStrongestAtEdgeCenterAndDisappearsAtItsBoundaries()
    {
        Assert.Equal(1, SpeechDirectionIndicators.GetGradientStrength(0, 0));
        Assert.Equal(0, SpeechDirectionIndicators.GetGradientStrength(1, 0));
        Assert.Equal(0, SpeechDirectionIndicators.GetGradientStrength(0, -1));
        Assert.Equal(0, SpeechDirectionIndicators.GetGradientStrength(0, 1));
        Assert.True(SpeechDirectionIndicators.GetGradientStrength(0.25f, 0) >
                    SpeechDirectionIndicators.GetGradientStrength(0.75f, 0));
        Assert.Equal(SpeechDirectionIndicators.GetGradientStrength(0.2f, -0.5f),
                     SpeechDirectionIndicators.GetGradientStrength(0.2f, 0.5f));
    }

}
