using GravityShift.Components;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Systems;

namespace GravityShift.Systems;

/// <summary>Slides each belt's stripe across its tile so you can see which way it runs.</summary>
public sealed class BeltMarkerSystem(World world) : IUpdateSystem
{
    private float _time;

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        // One tile per (TileSize / BeltSpeed) seconds: the stripe moves at belt speed.
        var phase = (_time * Tuning.BeltSpeed / Tuning.TileSize) % 1f;
        var offset = (phase - 0.5f) * Tuning.TileSize * 0.8f;

        foreach (var (entity, marker, transform) in world.Query<BeltMarker, Transform2D>().ToList())
        {
            var moved = transform;
            moved.Position = marker.Center + marker.Direction * offset;
            world.AddComponent(entity, moved);
        }
    }
}
