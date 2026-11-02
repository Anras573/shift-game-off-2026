using GravityShift.Components;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Eases the camera so the current gravity always points down on screen, and keeps the
/// parcel sprite upright relative to the camera. Runs once per rendered frame.
/// </summary>
public sealed class CameraRotationSystem(World world, GravityState gravity) : IUpdateSystem
{
    public void Update(float deltaTime)
    {
        var target = gravity.Direction.CameraRotation();
        var blend = 1f - MathF.Exp(-Tuning.CameraRotationSmoothing * deltaTime);
        float? rotation = null;

        foreach (var (entity, camera) in world.GetStore<Camera2D>().All().ToList())
        {
            var current = camera.Rotation;
            var next = current + ShortestAngle(current, target) * blend;
            world.AddComponent(entity, camera with { Rotation = next });
            rotation ??= next;
        }

        if (rotation is null)
            return;

        foreach (var (entity, _, transform) in world.Query<Player, Transform2D>().ToList())
        {
            var upright = transform;
            upright.Rotation = rotation.Value;
            world.AddComponent(entity, upright);
        }
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
