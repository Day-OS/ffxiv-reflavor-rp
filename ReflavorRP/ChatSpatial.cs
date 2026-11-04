using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace ReflavorRP;

internal static class ChatSpatial
{
    internal static float GetDistanceStrength(float distance, float range)
    {
        range = float.IsFinite(range) ? Math.Clamp(range, 5, 100) : 30;
        return Math.Clamp((range - distance) / (range - 3), 0, 1);
    }

    internal static unsafe Vector2 GetCameraDirection(Vector3 source, SceneCamera* camera)
    {
        var relative = source - (Vector3)camera->Position;
        var view = camera->ViewMatrix;
        return new Vector2(
            relative.X * view.M11 + relative.Y * view.M21 + relative.Z * view.M31,
            relative.X * view.M13 + relative.Y * view.M23 + relative.Z * view.M33);
    }

    internal static unsafe float GetCharacterHeight(IPlayerCharacter character) =>
        Math.Clamp(((NativeGameObject*)character.Address)->Height, 0.5f, 5f);
}
