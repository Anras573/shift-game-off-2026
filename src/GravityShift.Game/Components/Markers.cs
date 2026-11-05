namespace GravityShift.Components;

// Yaeger components must be structs. These are zero-size tags used to find gameplay entities.

/// <summary>The player-controlled parcel.</summary>
public struct Player;

/// <summary>The delivery chute: touching it completes the level.</summary>
public struct Goal;

/// <summary>Shredders, spikes and other things that send the parcel back to the start.</summary>
public struct Hazard;

/// <summary>A gravity-lock zone: no shifting while the parcel overlaps it.</summary>
public struct GravityLock;

/// <summary>The moving stripe drawn on a conveyor belt tile.</summary>
public struct BeltMarker(System.Numerics.Vector2 center, System.Numerics.Vector2 direction)
{
    public System.Numerics.Vector2 Center = center;
    public System.Numerics.Vector2 Direction = direction;
}
