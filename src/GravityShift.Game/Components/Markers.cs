namespace GravityShift.Components;

// Yaeger components must be structs. These are zero-size tags used to find gameplay entities.

/// <summary>The player-controlled parcel.</summary>
public struct Player;

/// <summary>The delivery chute: touching it completes the level.</summary>
public struct Goal;

/// <summary>Shredders, spikes and other things that send the parcel back to the start.</summary>
public struct Hazard;
