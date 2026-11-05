namespace GravityShift.Gravity;

/// <summary>Per-level gravity state shared by the gameplay systems.</summary>
public sealed class GravityState
{
    public GravityDirection Direction { get; set; } = GravityDirection.Down;

    /// <summary>Seconds remaining before another shift is allowed.</summary>
    public float Cooldown { get; set; }

    /// <summary>Total number of shifts this attempt.</summary>
    public int ShiftCount { get; set; }

    /// <summary>How many gravity-lock zones the parcel overlaps; shifting needs zero.</summary>
    public int LockCount { get; set; }

    public bool IsLocked => LockCount > 0;
}
