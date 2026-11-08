using System.Numerics;
using GravityShift.Feel;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Eases the camera so the current gravity always points down on screen, zooms so the square
/// room fits the screen in portrait as well as landscape, and adds screen shake on top.
/// Runs once per rendered frame.
/// </summary>
public sealed class CameraRotationSystem(
    World world,
    GravityState gravity,
    IViewport? viewport = null,
    ScreenShake? shake = null
) : IUpdateSystem
{
    private float? _rotation;

    /// <summary>The eased camera rotation, without shake: what "upright" means on screen.</summary>
    public float Rotation => _rotation ?? gravity.Direction.CameraRotation();

    public void Update(float deltaTime)
    {
        var target = gravity.Direction.CameraRotation();
        var blend = 1f - MathF.Exp(-Tuning.CameraRotationSmoothing * deltaTime);
        var current = _rotation ?? target;
        _rotation = current + ShortestAngle(current, target) * blend;

        foreach (var (entity, camera) in world.GetStore<Camera2D>().All().ToList())
        {
            world.AddComponent(
                entity,
                camera with
                {
                    Position = shake?.Offset ?? Vector2.Zero,
                    Rotation = _rotation.Value + (shake?.Angle ?? 0f),
                    Zoom = FitZoom(viewport),
                }
            );
        }
    }

    /// <summary>
    /// Zoom that fits the 2-unit room on screen. Camera2D shows 2/zoom units vertically and
    /// 2·aspect/zoom horizontally, so on a portrait screen (aspect &lt; 1) the width is the limit.
    /// </summary>
    internal static float FitZoom(IViewport? viewport)
    {
        var aspect = viewport?.AspectRatio ?? 1f;
        return Tuning.CameraZoom * MathF.Min(1f, aspect > 0f ? aspect : 1f);
    }

    /// <summary>Signed difference <c>to - from</c>, wrapped into (-π, π].</summary>
    internal static float ShortestAngle(float from, float to)
    {
        var delta = (to - from) % MathF.Tau;
        if (delta > MathF.PI)
            delta -= MathF.Tau;
        else if (delta <= -MathF.PI)
            delta += MathF.Tau;
        return delta;
    }
}
