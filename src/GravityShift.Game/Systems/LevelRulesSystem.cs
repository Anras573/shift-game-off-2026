using System.Numerics;
using GravityShift.Components;
using GravityShift.Levels;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Physics.Components;
using Yaeger.Physics.Systems;
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
/// Decides whether the parcel reached a chute, hit a shredder or left the room. Chutes and
/// shredders are trigger colliders reported by <see cref="CharacterControllerSystem"/>'s
/// trigger events. Leaving the room is a plain bounds check.
/// </summary>
public sealed class LevelRulesSystem : IUpdateSystem
{
    private readonly World _world;
    private readonly LevelData _level;

    public LevelRulesSystem(World world, LevelData level, CharacterControllerSystem controllers)
    {
        _world = world;
        _level = level;
        controllers.OnTriggerEnter += OnTriggerEnter;
    }

    public LevelOutcome Outcome { get; private set; } = LevelOutcome.Playing;

    /// <summary>Checks the out-of-bounds rule. Run after the controller system each step.</summary>
    public void Update(float deltaTime)
    {
        if (Outcome != LevelOutcome.Playing)
            return;

        var bounds = _level.WorldSize / 2f + new Vector2(Tuning.TileSize);
        foreach (
            var (_, _, controller, transform) in _world.Query<
                Player,
                CharacterController2D,
                Transform2D
            >()
        )
        {
            var center = transform.Position + controller.Offset;
            if (MathF.Abs(center.X) > bounds.X || MathF.Abs(center.Y) > bounds.Y)
                Outcome = LevelOutcome.Lost;
        }
    }

    private void OnTriggerEnter(Entity controller, Entity trigger)
    {
        if (Outcome != LevelOutcome.Playing || !_world.TryGetComponent<Player>(controller, out _))
            return;

        if (_world.TryGetComponent<Goal>(trigger, out _))
            Outcome = LevelOutcome.Delivered;
        else if (_world.TryGetComponent<Hazard>(trigger, out _))
            Outcome = LevelOutcome.Shredded;
    }
}
