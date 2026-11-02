using System.Numerics;
using GravityShift.Components;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Input;
using Yaeger.Physics.Components;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Walks the parcel along whatever surface is currently the floor and caps its fall speed.
/// Runs before <c>CharacterControllerSystem</c> each fixed step.
/// </summary>
public sealed class PlayerMovementSystem(World world, GravityState gravity, IInputState input)
    : IUpdateSystem
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

        foreach (var (entity, _, velocity) in world.Query<Player, Velocity2D>().ToList())
        {
            var fall = MathF.Min(Vector2.Dot(velocity.Linear, down), Tuning.TerminalVelocity);
            var updated = velocity;
            updated.Linear = down * fall + right * (walk * Tuning.WalkSpeed);
            world.AddComponent(entity, updated);
        }
    }
}
