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
        Assert.True(session.Gravity.IsOnFloor(Controller(session)));
    }

    [Fact]
    public void Shift_RotatesGravityAndCamera()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[0], input);

        Shift(session, input, Keys.E);
        Run(session, input, seconds: 1f);

        Assert.Equal(GravityDirection.Right, session.Gravity.Direction);
        Assert.True(session.Gravity.IsOnFloor(Controller(session)));
        Assert.Equal(
            GravityDirection.Right.CameraRotation(),
            session.World.GetComponent<Camera2D>(session.World.GetEntity("camera")).Rotation,
            2
        );
    }

    [Fact]
    public void Level1_ShiftRightThenWalk_Delivers()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[0], input);

        Shift(session, input, Keys.E);
        Run(session, input, seconds: 0.8f);
        Walk(session, input, Keys.Right, seconds: 2f);

        Assert.Equal(LevelOutcome.Delivered, session.Outcome);
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

    [Fact]
    public void Level2_OverTheCeiling_Delivers()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[1], input);

        Shift(session, input, Keys.Q); // left wall (already touching it)
        Shift(session, input, Keys.Q); // ceiling
        Run(session, input, seconds: 1f);
        Walk(session, input, Keys.Left, seconds: 2f); // upside down: screen-left is world right
        Shift(session, input, Keys.Q); // right wall
        Run(session, input, seconds: 0.5f);
        Shift(session, input, Keys.Q); // back to the floor, next to the chute
        Run(session, input, seconds: 1f);
        Walk(session, input, Keys.Left, seconds: 0.5f);

        Assert.Equal(LevelOutcome.Delivered, session.Outcome);
    }

    [Fact]
    public void Level2_FlipAcrossTheCeiling_Delivers()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[1], input);

        Shift(session, input, Keys.W); // ceiling
        Run(session, input, seconds: 1f);
        Walk(session, input, Keys.Left, seconds: 1.67f); // upside down: screen-left is world right
        AssertColumn(session, 13);
        Shift(session, input, Keys.W); // drop straight into the chute
        Run(session, input, seconds: 1.5f);

        Assert.Equal(LevelOutcome.Delivered, session.Outcome);
    }

    [Fact]
    public void Level3_ChainedShifts_Deliver()
    {
        var input = new FakeInput();
        var session = new LevelSession(LevelCatalog.All[2], input);

        Shift(session, input, Keys.W); // up the corridor to the ceiling
        Run(session, input, seconds: 1f);
        Walk(session, input, Keys.Left, seconds: 1f); // along the ceiling, past the box
        Shift(session, input, Keys.Q); // right wall
        Run(session, input, seconds: 0.5f);
        Walk(session, input, Keys.Left, seconds: 1.2f); // down the right wall
        AssertRowBetween(session, 8, 13);
        Shift(session, input, Keys.W); // fly left to the corridor wall
        Run(session, input, seconds: 1f);
        Shift(session, input, Keys.Q); // ...then up into the box
        Run(session, input, seconds: 1.5f);

        Assert.Equal(LevelOutcome.Delivered, session.Outcome);
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

    private static System.Numerics.Vector2 PlayerPosition(LevelSession session) =>
        session.World.GetComponent<Transform2D>(session.World.GetEntity("player")).Position;

    private static Yaeger.Physics.Components.CharacterController2D Controller(
        LevelSession session
    ) =>
        session.World.GetComponent<Yaeger.Physics.Components.CharacterController2D>(
            session.World.GetEntity("player")
        );

    private static void AssertColumn(LevelSession session, int column)
    {
        var x = PlayerPosition(session).X;
        Assert.Equal(column, (int)MathF.Floor((x - session.Level.Origin.X) / Tuning.TileSize));
    }

    private static void AssertRowBetween(LevelSession session, int minRow, int maxRow)
    {
        var y = PlayerPosition(session).Y;
        var row = (int)
            MathF.Floor(session.Level.Height - (y - session.Level.Origin.Y) / Tuning.TileSize);
        Assert.InRange(row, minRow, maxRow);
    }
}
