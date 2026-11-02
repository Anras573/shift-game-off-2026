namespace GravityShift.Gravity;

/// <summary>Per-level gravity state shared by the gameplay systems.</summary>
public sealed class GravityState
{
    public GravityDirection Direction { get; set; } = GravityDirection.Down;

    /// <summary>Seconds remaining before another shift is allowed.</summary>
    public float Cooldown { get; set; }

    /// <summary>Total number of shifts this attempt (handy for a par/score system later).</summary>
    public int ShiftCount { get; set; }
}
