namespace GravityShift;

/// <summary>
/// Gameplay feel knobs, all in one place so they're easy to tweak during the jam.
/// World units: one level tile is <see cref="TileSize"/> units wide.
/// </summary>
public static class Tuning
{
    /// <summary>Edge length of one level tile, in world units.</summary>
    public const float TileSize = 0.125f;

    /// <summary>Player collision box edge length (slightly smaller than a tile so gaps are passable).</summary>
    public const float PlayerSize = 0.1f;

    /// <summary>Gravity acceleration magnitude, in units/s².</summary>
    public const float GravityStrength = 12f;

    /// <summary>Maximum speed along the gravity axis, in units/s. Also guards against tunnelling.</summary>
    public const float TerminalVelocity = 3.5f;

    /// <summary>Sideways ("walking") speed relative to the current floor, in units/s.</summary>
    public const float WalkSpeed = 0.9f;

    /// <summary>
    /// Conveyor belt speed, in units/s. Faster than <see cref="WalkSpeed"/>, so belts are
    /// one-way: walking against one only slows you down.
    /// </summary>
    public const float BeltSpeed = 1.3f;

    /// <summary>Seconds after a gravity shift before another one is accepted.</summary>
    public const float ShiftCooldown = 0.3f;

    /// <summary>Exponential rate at which the camera eases towards the new "down" (higher = snappier).</summary>
    public const float CameraRotationSmoothing = 12f;

    /// <summary>Camera zoom. 1 shows exactly 2 world units vertically; rooms are 2 units square.</summary>
    public const float CameraZoom = 0.9f;

    /// <summary>Seconds the "Delivered" banner shows before the next level starts.</summary>
    public const float DeliveredPause = 1.6f;

    /// <summary>Seconds the "Shredded" / "Lost" banner shows before the level restarts.</summary>
    public const float FailedPause = 0.8f;

    // ---- Game feel (visual only; physics is unaffected) ----

    /// <summary>How flat the parcel squashes on the hardest landing (0.35 = 35% shorter).</summary>
    public const float SquashMax = 0.35f;

    /// <summary>How tall the parcel stretches at terminal velocity.</summary>
    public const float StretchMax = 0.18f;

    /// <summary>Squash spring stiffness and damping: a quick, slightly bouncy recovery.</summary>
    public const float SquashStiffness = 320f;

    public const float SquashDamping = 16f;

    /// <summary>Landings slower than this (units/s) don't shake the screen.</summary>
    public const float ShakeLandingThreshold = 1.2f;

    /// <summary>Trauma added by a landing at terminal velocity.</summary>
    public const float ShakeLandingTrauma = 0.55f;

    /// <summary>Trauma added when a shift is refused inside a gravity lock.</summary>
    public const float ShakeLockedTrauma = 0.3f;

    /// <summary>Trauma added when the parcel is shredded.</summary>
    public const float ShakeShreddedTrauma = 0.8f;

    /// <summary>Trauma lost per second.</summary>
    public const float ShakeDecay = 1.8f;

    /// <summary>Camera offset (world units) and roll (radians) at full trauma.</summary>
    public const float ShakeMaxOffset = 0.05f;

    public const float ShakeMaxAngle = 0.035f;

    /// <summary>Seconds a gravity-lock zone flashes after refusing a shift.</summary>
    public const float LockFlashTime = 0.3f;

    /// <summary>Fixed simulation step, in seconds. Keeps physics frame-rate independent.</summary>
    public const float FixedTimeStep = 1f / 120f;

    /// <summary>Upper bound on simulation steps per frame (spiral-of-death guard).</summary>
    public const int MaxStepsPerFrame = 8;
}
