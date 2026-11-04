using System.Numerics;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Moq;

namespace ReflavorRP.Tests;

public class SpeechSoundsTests
{
    private readonly Mock<IObjectTable> objects = new();
    private readonly Mock<IPlayerCharacter> localPlayer = new();
    private readonly Mock<IPlayerCharacter> speaker = new();
    private readonly Configuration config = new() { PlaySpeechSound = true, ShowSpeechDirection = false };
    private readonly List<(Vector3 Position, float Volume)> played = [];
    private readonly List<float> centered = [];
    private readonly SpeechSounds sounds;

    public SpeechSoundsTests()
    {
        localPlayer.SetupGet(p => p.EntityId).Returns(1);
        localPlayer.SetupGet(p => p.Position).Returns(new Vector3(100, 10, 100));
        speaker.SetupGet(p => p.EntityId).Returns(2);
        speaker.SetupGet(p => p.Name).Returns((SeString)"Speaker Tia");
        speaker.SetupGet(p => p.HomeWorld).Returns(
            new Lumina.Excel.RowRef<Lumina.Excel.Sheets.World>(null!, 75, Lumina.Data.Language.English));
        speaker.SetupGet(p => p.Position).Returns(new Vector3(103, 10, 100));
        objects.SetupGet(o => o.LocalPlayer).Returns(localPlayer.Object);
        objects.SetupGet(o => o.PlayerObjects).Returns(new[] { speaker.Object });
        sounds = new SpeechSounds(objects.Object, config, (position, volume) => played.Add((position, volume)), centered.Add);
    }

    private void Receive(long now = 1000) => sounds.Add(speaker.Object, now);

    [Fact]
    public void SpeechPlaysImmediatelyWithoutVisualIndicatorsOrScreenProjection()
    {
        Receive();
        var sound = Assert.Single(played);
        Assert.Equal(speaker.Object.Position, sound.Position);
        Assert.Equal(1f, sound.Volume, 5);
    }

    [Fact]
    public void SoundUsesItsOwnDistanceSetting()
    {
        config.SpeechDirectionRange = 5;
        config.SpeechSoundRange = 30;
        speaker.SetupGet(p => p.Position).Returns(new Vector3(116.5f, 10, 100));
        Receive();
        Assert.Equal(0.5f, Assert.Single(played).Volume, 5);
    }

    [Fact]
    public void GlobalCooldownDropsBurstsWithoutQueuingThem()
    {
        Receive(1000);
        Receive(1500);
        Receive(2499);
        Receive(2500);
        Assert.Equal(2, played.Count);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void RespectsPluginChatStylingAndSoundSwitches(bool enabled, bool styling, bool sound)
    {
        config.Enabled = enabled;
        config.StyleIncomingChat = styling;
        config.PlaySpeechSound = sound;
        Receive();
        Assert.Empty(played);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    [InlineData(0.08f, 30)]
    [InlineData(0.08f, 100)]
    public void MutedAndOutOfRangeSpeechStaysSilent(float volume, float distance)
    {
        config.SpeechSoundVolume = volume;
        speaker.SetupGet(p => p.Position).Returns(new Vector3(100 + distance, 10, 100));
        Receive();
        Assert.Empty(played);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(float.NaN, 1)]
    [InlineData(float.PositiveInfinity, 1)]
    public void LimitsInvalidSavedVolume(float savedVolume, float expected)
    {
        config.SpeechSoundVolume = savedVolume;
        Receive();
        Assert.Equal(expected, Assert.Single(played).Volume, 5);
    }

    [Fact]
    public void IncomingTellPlaysWithoutLoadedPlayersCameraOrDistance()
    {
        objects.SetupGet(o => o.LocalPlayer).Returns((IPlayerCharacter?)null);
        objects.SetupGet(o => o.PlayerObjects).Returns(Array.Empty<IPlayerCharacter>());
        config.SpeechSoundRange = 5;
        sounds.AddTell(1000);
        Assert.Equal(1f, Assert.Single(centered), 5);
        Assert.Empty(played);
    }

    [Fact]
    public void TellAndPublicSpeechShareTheSameCooldown()
    {
        Receive(1000);
        sounds.AddTell(1500);
        sounds.AddTell(2500);
        Receive(3000);
        Receive(4000);
        Assert.Single(centered);
        Assert.Equal(2, played.Count);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void IncomingTellRespectsEnableSettings(bool enabled, bool styling, bool sound)
    {
        config.Enabled = enabled;
        config.StyleIncomingChat = styling;
        config.PlaySpeechSound = sound;
        sounds.AddTell(1000);
        Assert.Empty(centered);
    }
}
