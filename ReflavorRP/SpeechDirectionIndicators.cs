using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace ReflavorRP;

public sealed class SpeechDirectionIndicators(IObjectTable objects, IGameGui gameGui, Configuration config)
{
    private sealed record Cue(string Name, uint World, long StartedAt);

    private const long DurationMilliseconds = 4000;
    private readonly Dictionary<uint, Cue> cues = [];
    private Vector2 edgeOpacity;
    private long lastDrawAt;
    private long? demoStartedAt;

    public void Preview() => demoStartedAt = Environment.TickCount64;

    public void Add(IPlayerCharacter speaker)
    {
        if (!config.Enabled || !config.ShowSpeechDirection)
            return;
        // Only keep identity and time; chat messages and character pointers are borrowed.
        cues[speaker.EntityId] = new Cue(speaker.Name.TextValue, speaker.HomeWorld.RowId, Environment.TickCount64);
    }

    public unsafe void Draw()
    {
        var now = Environment.TickCount64;
        if (demoStartedAt is { } startedAt)
        {
            if (now - startedAt >= DurationMilliseconds)
                demoStartedAt = null;
            else
                DrawEdges(Vector2.One * GetTimeStrength(now - startedAt), preview: true);
        }

        var localPlayer = objects.LocalPlayer;
        if (!config.Enabled || !config.StyleIncomingChat || !config.ShowSpeechDirection || localPlayer == null)
        {
            cues.Clear();
            edgeOpacity = Vector2.Zero;
            lastDrawAt = 0;
            return;
        }

        if (cues.Count == 0 && edgeOpacity.X < 0.001f && edgeOpacity.Y < 0.001f)
        {
            edgeOpacity = Vector2.Zero;
            lastDrawAt = 0;
            return;
        }

        var elapsed = lastDrawAt == 0 ? 1f / 60 : Math.Clamp((now - lastDrawAt) / 1000f, 0, 0.1f);
        lastDrawAt = now;
        var targetOpacity = Vector2.Zero;
        var cameraManager = SceneCameraManager.Instance();
        var camera = cameraManager == null ? null : cameraManager->CurrentCamera;
        foreach (var (id, cue) in cues)
        {
            if (now - cue.StartedAt >= DurationMilliseconds)
            {
                cues.Remove(id);
                continue;
            }
            var speaker = objects.PlayerObjects.OfType<IPlayerCharacter>().FirstOrDefault(player => player.EntityId == id);
            if (speaker == null || !speaker.IsValid() || speaker.Name.TextValue != cue.Name ||
                speaker.HomeWorld.RowId != cue.World)
            {
                cues.Remove(id);
                continue;
            }
            if (camera == null)
                continue;

            var position = speaker.Position;
            var height = ChatSpatial.GetCharacterHeight(speaker);
            var onScreen = gameGui.WorldToScreen(position, out _) ||
                gameGui.WorldToScreen(position + new Vector3(0, height + 0.25f, 0), out _);
            if (onScreen)
                continue;

            // Use camera-space direction, not projected screen X: projection reverses behind the camera.
            var direction = ChatSpatial.GetCameraDirection(position, camera);
            var sides = GetSides(direction.X, direction.Y);
            var distance = Vector3.Distance(localPlayer.Position, position);
            var intensity = ChatSpatial.GetDistanceStrength(distance,
                config.SpeechDirectionRange) * GetTimeStrength(now - cue.StartedAt);
            // Several speakers on one side should not accumulate into an opaque border.
            targetOpacity = Vector2.Max(targetOpacity, sides * intensity);
        }

        edgeOpacity = Vector2.Lerp(edgeOpacity, targetOpacity, 1 - MathF.Exp(-elapsed / 0.12f));
        // The demo replaces live cues while visible, avoiding stacked opacity.
        if (demoStartedAt == null)
            DrawEdges(edgeOpacity);
    }

    private void DrawEdges(Vector2 strength, bool preview = false)
    {
        var viewport = ImGui.GetMainViewport();
        var draw = preview ? ImGui.GetForegroundDrawList(viewport) : ImGui.GetBackgroundDrawList(viewport);
        var color = config.SpeechDirectionColor;
        color = new Vector3(ClampSetting(color.X, 0, 1, 0.76f),
            ClampSetting(color.Y, 0, 1, 0.64f), ClampSetting(color.Z, 0, 1, 0.85f));
        var opacity = ClampSetting(config.SpeechDirectionOpacity, 0, 1, 0.65f);
        var width = Math.Min(ClampSetting(config.SpeechDirectionWidth, 20, 600, 180) *
            ImGuiHelpers.GlobalScale, viewport.Size.X * 0.5f);
        var height = viewport.Size.Y * ClampSetting(config.SpeechDirectionHeight, 0.1f, 1, 0.6f);
        DrawEdge(draw, viewport.Pos, viewport.Size, false, strength.X * opacity, color, width, height);
        DrawEdge(draw, viewport.Pos, viewport.Size, true, strength.Y * opacity, color, width, height);
    }

    private static float ClampSetting(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    internal static Vector2 GetSides(float horizontal, float depth)
    {
        // Directly behind: either turn works. Directly above/below and ahead needs no sideways cue.
        if (MathF.Abs(horizontal) < 0.01f)
            return depth > 0 ? Vector2.One : Vector2.Zero;
        return horizontal < 0 ? Vector2.UnitX : Vector2.UnitY;
    }

    internal static float GetTimeStrength(long age) =>
        Math.Clamp(age / 180f, 0, 1) * Math.Clamp((DurationMilliseconds - age) / 1000f, 0, 1);

    internal static float GetGradientStrength(float inward, float vertical)
    {
        var across = 1 - Math.Clamp(inward, 0, 1);
        var down = Math.Max(0, 1 - vertical * vertical);
        return across * across * down * down;
    }

    private static void DrawEdge(ImDrawListPtr draw, Vector2 origin, Vector2 size, bool right, float opacity,
        Vector3 color, float width, float height)
    {
        if (opacity < 0.001f || size.X <= 0 || size.Y <= 0)
            return;
        const int columns = 12;
        const int rows = 16;
        var edgeX = right ? origin.X + size.X : origin.X;
        var top = origin.Y + (size.Y - height) / 2;
        uint Color(float u, float v) => ImGui.ColorConvertFloat4ToU32(
            new Vector4(color, opacity * GetGradientStrength(u, v)));

        // A small vertex-colored grid makes both the inward and vertical fade smooth, with no textures.
        for (var row = 0; row < rows; row++)
        {
            var v0 = row / (float)rows;
            var v1 = (row + 1) / (float)rows;
            for (var column = 0; column < columns; column++)
            {
                var u0 = column / (float)columns;
                var u1 = (column + 1) / (float)columns;
                var x0 = edgeX + (right ? -u1 : u0) * width;
                var x1 = edgeX + (right ? -u0 : u1) * width;
                var left = right ? u1 : u0;
                var rightU = right ? u0 : u1;
                draw.AddRectFilledMultiColor(new Vector2(x0, top + v0 * height),
                    new Vector2(x1, top + v1 * height), Color(left, v0 * 2 - 1), Color(rightU, v0 * 2 - 1),
                    Color(rightU, v1 * 2 - 1), Color(left, v1 * 2 - 1));
            }
        }
    }
}
