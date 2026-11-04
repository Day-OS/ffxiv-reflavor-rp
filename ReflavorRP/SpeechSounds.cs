using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;

namespace ReflavorRP;

public sealed class SpeechSounds(IObjectTable objects, Configuration config, Action<Vector3, float> playSpeechSound,
    Action<float> playCenteredSound)
{
    private long? lastSoundAt;

    public void Add(IPlayerCharacter speaker) => Add(speaker, Environment.TickCount64);

    internal void Add(IPlayerCharacter speaker, long now)
    {
        var localPlayer = objects.LocalPlayer;
        if (localPlayer == null)
            return;

        var volume = GetVolume(now);
        var position = speaker.Position;
        volume *= ChatSpatial.GetDistanceStrength(Vector3.Distance(localPlayer.Position, position), config.SpeechSoundRange);
        if (volume <= 0.001f)
            return;

        // React once to the message itself, independently of screen visibility or visual indicators.
        lastSoundAt = now;
        playSpeechSound(position, volume);
    }

    public void AddTell() => AddTell(Environment.TickCount64);

    internal void AddTell(long now)
    {
        var volume = GetVolume(now);
        if (volume <= 0.001f)
            return;
        // Tell has no physical hearing range or reliable world position; it must work across worlds too.
        lastSoundAt = now;
        playCenteredSound(volume);
    }

    private float GetVolume(long now)
    {
        if (!config.Enabled || !config.StyleIncomingChat || !config.PlaySpeechSound ||
            (lastSoundAt is { } last && now - last < 1500))
            return 0;
        return float.IsFinite(config.SpeechSoundVolume)
            ? Math.Clamp(config.SpeechSoundVolume, 0, 2f) : 1f;
    }
}
