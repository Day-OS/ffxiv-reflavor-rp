using System.Numerics;

namespace ReflavorRP.Tests;

public class SpeechNotificationSoundTests
{
    [Theory]
    [InlineData(-1, 0, 1, 0)]
    [InlineData(1, 0, 0, 1)]
    public void SideSpeakersUseTheCorrespondingStereoChannel(float horizontal, float depth,
        float left, float right)
    {
        Assert.Equal(new Vector2(left, right),
            SpeechNotificationSound.GetChannelGains(new Vector2(horizontal, depth)));
    }

    [Fact]
    public void PanningKeepsConstantPowerAndDoesNotReverseBehindTheCamera()
    {
        var ahead = SpeechNotificationSound.GetChannelGains(new Vector2(-1, -1));
        var behind = SpeechNotificationSound.GetChannelGains(new Vector2(-1, 1));
        var farther = SpeechNotificationSound.GetChannelGains(new Vector2(-100, 100));
        Assert.Equal(ahead, behind);
        Assert.Equal(behind.X, farther.X, 5);
        Assert.Equal(behind.Y, farther.Y, 5);
        Assert.True(ahead.X > ahead.Y);
        Assert.Equal(1, ahead.LengthSquared(), 5);
    }

    [Fact]
    public void CenteredAndCoincidentSourcesUseBalancedChannels()
    {
        foreach (var direction in new[] { Vector2.Zero, Vector2.UnitY, -Vector2.UnitY })
        {
            var gains = SpeechNotificationSound.GetChannelGains(direction);
            Assert.Equal(gains.X, gains.Y);
            Assert.Equal(1, gains.LengthSquared(), 5);
        }
    }

    [Fact]
    public void SpeechSampleIsEmbeddedAndShortEnoughForANotification()
    {
        using var stream = typeof(SpeechNotificationSound).Assembly.GetManifestResourceStream("ReflavorRP.SpeechNotification.pcm");
        Assert.NotNull(stream);
        Assert.Equal(0, stream.Length % 2);
        var duration = stream.Length / (44100d * 2);
        Assert.InRange(duration, 0.1, 0.5);
    }
}
