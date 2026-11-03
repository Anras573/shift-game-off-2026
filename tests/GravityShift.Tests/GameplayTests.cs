using System.Numerics;
using GravityShift.Gravity;
using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Graphics;
using Yaeger.Input;

namespace GravityShift.Tests;

/// <summary>
/// Headless play-throughs: drive a <see cref="LevelSession"/> with scripted input and check
/// that each built-in level is actually solvable (and that the obvious wrong move fails).
/// </summary>
public class GameplayTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void Parcel_FallsAndRestsOnTheFloor()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[0], input);
        var spawn = PlayerPosition(session);

        Run(session, input, seconds: 1f);

        Assert.Equal(LevelOutcome.Playing, session.Outcome);
        Assert.Equal(spawn.X, PlayerPosition(session).X, 3);
        Assert.True(Controller(session).IsGrounded);
    }

    [Fact]
    public void Shift_WorksInMidAir()
    {
        // Design decision: shifting is allowed while falling (only the cooldown limits it),
        // so levels can chain shifts in mid-air.
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[0], input);
        Run(session, input, seconds: 0.5f);

        Shift(session, input, Keys.W); // flip: start falling up towards the ceiling
        Assert.False(Controller(session).IsGrounded);

        Shift(session, input, Keys.Q); // shift again before landing
        Assert.Equal(GravityDirection.Right, session.Gravity.Direction);
        Assert.Equal(2, session.Gravity.ShiftCount);

        Run(session, input, seconds: 1f);
        Assert.True(Controller(session).IsGrounded);
        Assert.Equal(-Vector2.UnitX, Controller(session).GroundNormal);
    }

    [Fact]
    public void Shift_RotatesGravityAndCamera()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[0], input);

        Shift(session, input, Keys.E);
        Run(session, input, seconds: 1f);

        Assert.Equal(GravityDirection.Right, session.Gravity.Direction);
        // Grounding follows gravity: the right wall is now the floor.
        Assert.True(Controller(session).IsGrounded);
        Assert.Equal(-Vector2.UnitX, Controller(session).GroundNormal);
        Assert.Equal(
            GravityDirection.Right.CameraRotation(),
            session.World.GetComponent<Camera2D>(session.World.GetEntity("camera")).Rotation,
            2
        );
    }

    [Fact]
    public void Level2_ShiftingTowardTheShredders_Fails()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[1], input);

        Shift(session, input, Keys.E);
        Run(session, input, seconds: 1.5f);

        Assert.Equal(LevelOutcome.Shredded, session.Outcome);
    }

    private static void Shift(LevelSession session, FakeInput input, Keys key)
    {
        input.Tap(key);
        session.Update(Frame);
        input.EndFrame();
        Run(session, input, Tuning.ShiftCooldown + Frame);
    }

    private static void Walk(LevelSession session, FakeInput input, Keys key, float seconds)
    {
        input.Hold(key);
        Run(session, input, seconds);
        input.Release(key);
    }

    private static void Run(LevelSession session, FakeInput input, float seconds)
    {
        for (var t = 0f; t < seconds && session.Outcome == LevelOutcome.Playing; t += Frame)
        {
            session.Update(Frame);
            input.EndFrame();
        }
    }

    private static Vector2 PlayerPosition(LevelSession session) =>
        session.World.GetComponent<Transform2D>(session.World.GetEntity("player")).Position;

    private static Yaeger.Physics.Components.CharacterController2D Controller(
        LevelSession session
    ) =>
        session.World.GetComponent<Yaeger.Physics.Components.CharacterController2D>(
            session.World.GetEntity("player")
        );
}
