using System.Numerics;
using GravityShift.Components;
using GravityShift.Gravity;
using GravityShift.Levels;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Input;
using Yaeger.Physics.Components;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Walks the parcel along whatever surface is currently the floor and caps its fall speed.
/// Runs before <c>CharacterControllerSystem</c> each fixed step.
/// </summary>
public sealed class PlayerMovementSystem(
    World world,
    GravityState gravity,
    IInputState input,
    LevelData level
) : IUpdateSystem
{
    public void Update(float deltaTime)
    {
        // Input is relative to the screen, which is rotated so gravity always points down.
        var walk = 0f;
        if (input.IsKeyPressed(Keys.Left) || input.IsKeyPressed(Keys.A))
            walk -= 1f;
        if (input.IsKeyPressed(Keys.Right) || input.IsKeyPressed(Keys.D))
            walk += 1f;

        var down = gravity.Direction.ToVector();
        var right = gravity.Direction.ScreenRight();

        foreach (
            var (entity, _, velocity, controller, transform) in world
                .Query<Player, Velocity2D, CharacterController2D, Transform2D>()
                .ToList()
        )
        {
            var fall = MathF.Min(Vector2.Dot(velocity.Linear, down), Tuning.TerminalVelocity);
            var belt = BeltPush(controller, transform.Position + controller.Offset, down, right);
            var updated = velocity;
            updated.Linear = down * fall + right * (walk * Tuning.WalkSpeed + belt);
            world.AddComponent(entity, updated);
        }
    }

    /// <summary>
    /// Sideways speed (along <paramref name="right"/>) from a belt under the parcel's feet.
    /// Belts push in their own world direction, so only belts running along the current floor
    /// do anything; a belt running along gravity is just a wall.
    /// </summary>
    private float BeltPush(
        in CharacterController2D controller,
        Vector2 center,
        Vector2 down,
        Vector2 right
    )
    {
        if (!controller.IsGrounded || level.Belts.Count == 0)
            return 0f;

        // Probe just past the feet: middle first, then both edges.
        var feet = center + down * (controller.HalfSize.X + Tuning.TileSize * 0.1f);
        var edge = right * controller.HalfSize.X * 0.9f;
        foreach (var probe in (ReadOnlySpan<Vector2>)[feet, feet + edge, feet - edge])
        {
            if (level.BeltAt(level.CellAt(probe)) is not { } direction)
                continue;
            var along = Vector2.Dot(direction, right);
            if (MathF.Abs(along) > 0.5f)
                return MathF.Sign(along) * Tuning.BeltSpeed;
        }

        return 0f;
    }
}
