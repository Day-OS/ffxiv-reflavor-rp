using System.Numerics;
using GameMatrix = FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4;
using SceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

namespace ReflavorRP.Tests;

public class ChatSpatialTests
{
    [Fact]
    public unsafe void CameraDirectionMatchesTheViewTransformForATranslatedRotatedCamera()
    {
        var cameraPosition = new Vector3(10, 4, 10);
        var source = new Vector3(3, 2, -10);
        var view = Matrix4x4.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);
        var gameView = new GameMatrix
        {
            M11 = view.M11, M21 = view.M21, M31 = view.M31,
            M13 = view.M13, M23 = view.M23, M33 = view.M33,
        };

        var camera = new SceneCamera();
        camera.Position = cameraPosition;
        camera.ViewMatrix = gameView;
        var direction = ChatSpatial.GetCameraDirection(source, &camera);
        var expected = Vector3.Transform(source, view);

        Assert.Equal(expected.X, direction.X, 4);
        Assert.Equal(expected.Z, direction.Y, 4);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 1)]
    [InlineData(16.5f, 0.5f)]
    [InlineData(30, 0)]
    [InlineData(100, 0)]
    public void SharedDistanceStrengthPreservesTheBubbleOpacityFloor(float distance, float strength)
    {
        var actual = ChatSpatial.GetDistanceStrength(distance, 30);
        Assert.Equal(strength, actual, 5);
        var previousBubbleOpacity = 1 - 0.7f * Math.Clamp((distance - 3) / 27, 0, 1);
        Assert.Equal(previousBubbleOpacity, 0.3f + 0.7f * actual, 5);
    }

    [Fact]
    public void CustomRangeAndInvalidSavedValuesStayBounded()
    {
        Assert.Equal(0, ChatSpatial.GetDistanceStrength(10, 10));
        Assert.Equal(0, ChatSpatial.GetDistanceStrength(30, float.NaN));
        Assert.Equal(0, ChatSpatial.GetDistanceStrength(5, -10));
        Assert.Equal(0, ChatSpatial.GetDistanceStrength(100, 1000));
    }
}
