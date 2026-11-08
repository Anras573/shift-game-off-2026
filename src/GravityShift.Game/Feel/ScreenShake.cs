using System.Numerics;

namespace GravityShift.Feel;

/// <summary>
/// Trauma-based 2D screen shake: impacts add trauma (0..1), which decays over time, and the
/// camera jitters by an amount proportional to trauma² (small knocks barely shake, big ones
/// shake hard). The jitter is a deterministic sum of sines, so tests can check it exactly.
/// </summary>
public sealed class ScreenShake(bool enabled = true)
{
    private float _time;

    /// <summary>False when the player asked for reduced motion: trauma is ignored.</summary>
    public bool Enabled { get; } = enabled;

    public float Trauma { get; private set; }

    public void AddTrauma(float amount)
    {
        if (Enabled && amount > 0f)
            Trauma = Math.Clamp(Trauma + amount, 0f, 1f);
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        Trauma = MathF.Max(0f, Trauma - Tuning.ShakeDecay * deltaTime);
    }

    /// <summary>Camera position offset, in world units.</summary>
    public Vector2 Offset
    {
        get
        {
            var amount = Tuning.ShakeMaxOffset * Trauma * Trauma;
            return new Vector2(Noise(_time, 1.3f), Noise(_time, 2.9f)) * amount;
        }
    }

    /// <summary>Camera rotation offset, in radians.</summary>
    public float Angle => Tuning.ShakeMaxAngle * Trauma * Trauma * Noise(_time, 4.7f);

    // A few incommensurate sines: smooth, irregular-looking, in [-1, 1], and repeatable.
    private static float Noise(float t, float seed) =>
        (
            MathF.Sin(t * 41f + seed)
            + MathF.Sin(t * 67f + seed * 2.1f) * 0.6f
            + MathF.Sin(t * 97f + seed * 3.7f) * 0.4f
        ) / 2f;
}
