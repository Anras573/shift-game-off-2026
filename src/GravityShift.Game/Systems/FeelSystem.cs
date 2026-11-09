using System.Numerics;
using GravityShift.Components;
using GravityShift.Feel;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Physics.Components;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Purely visual game feel. Physics never reads anything written here:
/// <list type="bullet">
/// <item>The parcel you see is a separate sprite that follows the physics body; it stretches
/// while falling and squashes on landing (a damped spring), anchored at its feet.</item>
/// <item>Hard landings, refused shifts and shredding add screen shake.</item>
/// <item>A gravity lock flashes when it refuses a shift.</item>
/// <item>The parcel shrinks into the chute when delivered, and flashes and jitters when
/// shredded.</item>
/// </list>
/// Runs once per rendered frame, after the camera has rotated.
/// </summary>
public sealed class FeelSystem : IUpdateSystem
{
    /// <summary>Lock tint at rest: the texture is drawn strong and shown translucent.</summary>
    public static readonly Color LockTint = new(255, 255, 255, 110);

    private static readonly Color ShreddedTint = new(255, 80, 80);
    private static readonly Color LockFlashTint = Color.White;

    private readonly World _world;
    private readonly GravityState _gravity;
    private readonly Func<LevelOutcome> _outcome;
    private readonly Func<float> _cameraRotation;

    private float _squash; // + squashed (wide, short), - stretched (narrow, tall)
    private float _squashVelocity;
    private float _lockFlash;
    private float _endTime;
    private LevelOutcome _seenOutcome = LevelOutcome.Playing;

    public FeelSystem(
        World world,
        GravityState gravity,
        ScreenShake shake,
        Func<LevelOutcome> outcome,
        Func<float> cameraRotation
    )
    {
        _world = world;
        _gravity = gravity;
        Shake = shake;
        _outcome = outcome;
        _cameraRotation = cameraRotation;
    }

    public ScreenShake Shake { get; }

    /// <summary>Current squash amount: positive squashed, negative stretched.</summary>
    public float Squash => _squash;

    /// <summary>Call when the parcel lands, with its speed along gravity just before impact.</summary>
    public void OnLanded(float impactSpeed)
    {
        var hardness = Math.Clamp(impactSpeed / Tuning.TerminalVelocity, 0f, 1f);
        _squash = MathF.Max(_squash, hardness * Tuning.SquashMax);
        _squashVelocity = 0f;

        var overThreshold =
            (impactSpeed - Tuning.ShakeLandingThreshold)
            / (Tuning.TerminalVelocity - Tuning.ShakeLandingThreshold);
        Shake.AddTrauma(Math.Clamp(overThreshold, 0f, 1f) * Tuning.ShakeLandingTrauma);
    }

    /// <summary>Call when a gravity lock refuses a shift.</summary>
    public void OnShiftLocked()
    {
        _lockFlash = Tuning.LockFlashTime;
        Shake.AddTrauma(Tuning.ShakeLockedTrauma);
    }

    public void Update(float deltaTime)
    {
        var outcome = _outcome();
        if (outcome != _seenOutcome)
        {
            _seenOutcome = outcome;
            _endTime = 0f;
            if (outcome == LevelOutcome.Shredded)
                Shake.AddTrauma(Tuning.ShakeShreddedTrauma);
        }
        else
        {
            _endTime += deltaTime;
        }

        UpdateSquash(deltaTime);
        UpdateParcel();
        UpdateLocks(deltaTime);
        Shake.Update(deltaTime);
    }

    private void UpdateSquash(float deltaTime)
    {
        // Stretch towards a target set by fall speed while airborne; rest at 0 on the ground.
        var target = 0f;
        foreach (
            var (_, _, velocity, controller) in _world.Query<
                Player,
                Velocity2D,
                CharacterController2D
            >()
        )
        {
            if (!controller.IsGrounded)
            {
                var fall = Vector2.Dot(velocity.Linear, _gravity.Direction.ToVector());
                target = -Math.Clamp(fall / Tuning.TerminalVelocity, 0f, 1f) * Tuning.StretchMax;
            }
        }

        // Damped spring, integrated in small steps so it's stable at any frame rate.
        for (var remaining = deltaTime; remaining > 0f; remaining -= 1f / 240f)
        {
            var dt = MathF.Min(remaining, 1f / 240f);
            var accel =
                -Tuning.SquashStiffness * (_squash - target)
                - Tuning.SquashDamping * _squashVelocity;
            _squashVelocity += accel * dt;
            _squash += _squashVelocity * dt;
        }
    }

    private void UpdateParcel()
    {
        if (
            !_world.TryGetEntity("player", out var player)
            || !_world.TryGetEntity("parcel", out var parcel)
            || !_world.TryGetComponent<Transform2D>(player, out var body)
        )
            return;

        var down = _gravity.Direction.ToVector();
        var size = Tuning.PlayerSize;
        var squash = Math.Clamp(_squash, -0.5f, 0.6f);
        var scale = new Vector2(size * (1f + squash), size * (1f - squash));
        // Keep the feet where the physics body's feet are: a shorter parcel sits lower.
        var position = body.Position + down * (size * squash / 2f);
        var tint = Color.White;

        switch (_seenOutcome)
        {
            case LevelOutcome.Delivered:
            {
                var shrink = MathF.Max(0f, 1f - _endTime / 0.35f);
                scale *= shrink;
                position += down * (size * (1f - shrink) / 2f); // sink into the chute
                break;
            }
            case LevelOutcome.Shredded:
            {
                var flash = MathF.Sin(_endTime * 40f) > 0f;
                tint = flash ? ShreddedTint : Color.White;
                var jitter = MathF.Max(0f, 1f - _endTime / 0.5f) * size * 0.25f;
                position +=
                    new Vector2(MathF.Sin(_endTime * 83f), MathF.Cos(_endTime * 71f)) * jitter;
                break;
            }
            case LevelOutcome.Lost:
                scale *= MathF.Max(0f, 1f - _endTime / 0.3f);
                break;
        }

        _world.AddComponent(parcel, new Transform2D(position, _cameraRotation(), scale));
        _world.AddComponent(parcel, new Sprite(Art.Parcel, tint));
    }

    private void UpdateLocks(float deltaTime)
    {
        if (_lockFlash <= 0f)
            return;

        _lockFlash = MathF.Max(0f, _lockFlash - deltaTime);
        var tint = _lockFlash > 0f ? LockFlashTint : LockTint;
        foreach (var (entity, _, _) in _world.Query<GravityLock, Sprite>().ToList())
            _world.AddComponent(entity, new Sprite(Art.Lock, tint));
    }
}
