using System.Numerics;
using GravityShift.Systems;
using Yaeger.Platform;

namespace GravityShift.Tests;

public class CameraFitTests
{
    [Theory]
    [InlineData(1280, 800, 1f)] // landscape: height is the limit, zoom unchanged
    [InlineData(800, 800, 1f)]
    [InlineData(400, 800, 0.5f)] // portrait phone: zoom out so the room's width fits
    public void FitZoom_KeepsTheWholeRoomOnScreen(float width, float height, float factor)
    {
        var zoom = CameraRotationSystem.FitZoom(new Viewport(width, height));

        Assert.Equal(Tuning.CameraZoom * factor, zoom, 5);
        // Camera2D shows 2·aspect/zoom by 2/zoom units; the room is 2 by 2.
        var aspect = width / height;
        Assert.True(2f * aspect / zoom >= 2f - 1e-4f && 2f / zoom >= 2f - 1e-4f);
    }

    [Fact]
    public void FitZoom_WithoutAViewport_IsTheDefault() =>
        Assert.Equal(Tuning.CameraZoom, CameraRotationSystem.FitZoom(null));

    private sealed class Viewport(float width, float height) : IViewport
    {
        public Vector2 Size => new(width, height);
        public float PixelRatio => 1f;
    }
}
