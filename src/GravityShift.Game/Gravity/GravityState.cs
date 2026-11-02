using Yaeger.Physics.Components;

namespace GravityShift.Gravity;

/// <summary>Per-level gravity state shared by the gameplay systems.</summary>
public sealed class GravityState
{
    public GravityDirection Direction { get; set; } = GravityDirection.Down;

    /// <summary>Seconds remaining before another shift is allowed.</summary>
    public float Cooldown { get; set; }

    /// <summary>Total number of shifts this attempt (handy for a par/score system later).</summary>
    public int ShiftCount { get; set; }

    /// <summary>
    /// Whether the controller is resting on the surface gravity is currently pulling it into.
    /// Yaeger's controller reports contacts on fixed world axes, so map them onto gravity here.
    /// </summary>
    public bool IsOnFloor(in CharacterController2D controller) =>
        Direction switch
        {
            GravityDirection.Down => controller.IsGrounded,
            GravityDirection.Up => controller.IsTouchingCeiling,
            GravityDirection.Left => controller.IsTouchingWallLeft,
            GravityDirection.Right => controller.IsTouchingWallRight,
            _ => false,
        };
}
