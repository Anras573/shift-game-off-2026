using GravityShift.Components;
using GravityShift.Gravity;
using Yaeger.ECS;
using Yaeger.Physics.Systems;

namespace GravityShift.Systems;

/// <summary>
/// Counts the gravity-lock zones the parcel overlaps (via the controller's trigger events) so
/// <see cref="GravityShiftSystem"/> can refuse shifts while it's inside one.
/// </summary>
public sealed class GravityLockSystem
{
    private readonly World _world;
    private readonly GravityState _gravity;

    public GravityLockSystem(
        World world,
        GravityState gravity,
        CharacterControllerSystem controllers
    )
    {
        _world = world;
        _gravity = gravity;
        controllers.OnTriggerEnter += (controller, trigger) => Adjust(controller, trigger, +1);
        controllers.OnTriggerExit += (controller, trigger) => Adjust(controller, trigger, -1);
    }

    private void Adjust(Entity controller, Entity trigger, int delta)
    {
        if (
            _world.TryGetComponent<Player>(controller, out _)
            && _world.TryGetComponent<GravityLock>(trigger, out _)
        )
            _gravity.LockCount = Math.Max(0, _gravity.LockCount + delta);
    }
}
