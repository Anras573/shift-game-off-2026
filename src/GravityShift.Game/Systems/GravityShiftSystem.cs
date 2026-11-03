using GravityShift.Components;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Input;
using Yaeger.Physics.Components;
using Yaeger.Physics.Systems;
using Yaeger.Platform;

namespace GravityShift.Systems;

/// <summary>
/// The core mechanic: Q / E rotate gravity so the left / right wall becomes the floor, and
/// W / Up / Space flips it so the ceiling becomes the floor.
/// </summary>
public sealed class GravityShiftSystem(
    World world,
    GravityState gravity,
    CharacterControllerSystem controllerSystem,
    IInputState input
)
{
    public static readonly Keys[] ShiftLeftKeys = [Keys.Q, Keys.Z];
    public static readonly Keys[] ShiftRightKeys = [Keys.E, Keys.X];
    public static readonly Keys[] FlipKeys = [Keys.W, Keys.Up, Keys.Space];

    /// <summary>Raised after every successful shift, e.g. for a sound effect.</summary>
    public event Action<GravityDirection>? Shifted;

    /// <summary>Reads one-shot key presses. Call once per rendered frame, not per physics step.</summary>
    public void HandleInput()
    {
        if (ShiftLeftKeys.Any(input.WasKeyPressed))
            TryShift(gravity.Direction.ShiftToLeftWall());
        else if (ShiftRightKeys.Any(input.WasKeyPressed))
            TryShift(gravity.Direction.ShiftToRightWall());
        else if (FlipKeys.Any(input.WasKeyPressed))
            TryShift(gravity.Direction.Flip());
    }

    /// <summary>Ticks the cooldown. Call once per fixed physics step.</summary>
    public void Update(float deltaTime) =>
        gravity.Cooldown = MathF.Max(0f, gravity.Cooldown - deltaTime);

    /// <summary>
    /// Applies a new gravity direction immediately if off cooldown. Deliberately works in
    /// mid-air too (no grounded check) so levels can be built around chained shifts.
    /// </summary>
    public bool TryShift(GravityDirection newDirection)
    {
        if (gravity.Cooldown > 0f || newDirection == gravity.Direction)
            return false;

        gravity.Direction = newDirection;
        gravity.Cooldown = Tuning.ShiftCooldown;
        gravity.ShiftCount++;
        controllerSystem.Gravity = newDirection.ToVector() * Tuning.GravityStrength;

        // Kill momentum so every shift starts from a clean, readable fall.
        foreach (var (entity, _, _) in world.Query<Player, Velocity2D>().ToList())
            world.AddComponent(entity, Velocity2D.Zero);

        Shifted?.Invoke(newDirection);
        return true;
    }
}
