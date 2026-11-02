using System.Numerics;
using GravityShift.Components;
using GravityShift.Levels;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Physics.Components;
using Yaeger.Systems;

namespace GravityShift.Systems;

public enum LevelOutcome
{
    Playing,
    Delivered,
    Shredded,
    Lost,
}

/// <summary>
/// Decides whether the parcel reached a chute, hit a shredder or left the room.
/// Uses plain AABB checks because the kinematic controller doesn't feed the trigger pipeline.
/// </summary>
public sealed class LevelRulesSystem(World world, LevelData level) : IUpdateSystem
{
    // Chutes are generous (touching counts), shredders forgiving (must overlap a bit).
    private const float GoalGrow = Tuning.TileSize * 0.1f;
    private const float HazardShrink = Tuning.TileSize * 0.15f;

    public LevelOutcome Outcome { get; private set; } = LevelOutcome.Playing;

    public void Update(float deltaTime)
    {
        if (Outcome != LevelOutcome.Playing)
            return;

        foreach (
            var (_, _, controller, transform) in world
                .Query<Player, CharacterController2D, Transform2D>()
                .ToList()
        )
        {
            var center = transform.Position + controller.Offset;
            var half = controller.HalfSize;

            if (OverlapsAny<Goal>(center, half, GoalGrow))
            {
                Outcome = LevelOutcome.Delivered;
                return;
            }

            if (OverlapsAny<Hazard>(center, half, -HazardShrink))
            {
                Outcome = LevelOutcome.Shredded;
                return;
            }

            var bounds = level.WorldSize / 2f + new Vector2(Tuning.TileSize);
            if (MathF.Abs(center.X) > bounds.X || MathF.Abs(center.Y) > bounds.Y)
            {
                Outcome = LevelOutcome.Lost;
                return;
            }
        }
    }

    private bool OverlapsAny<TMarker>(Vector2 center, Vector2 half, float grow)
        where TMarker : struct
    {
        foreach (var (_, _, transform) in world.Query<TMarker, Transform2D>())
        {
            var otherHalf = transform.Scale / 2f + new Vector2(grow);
            var delta = Vector2.Abs(center - transform.Position);
            if (delta.X < half.X + otherHalf.X && delta.Y < half.Y + otherHalf.Y)
                return true;
        }

        return false;
    }
}
