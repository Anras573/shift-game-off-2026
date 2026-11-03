using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Graphics;
using Yaeger.Input;
using Yaeger.Physics.Components;

namespace GravityShift.Tests;

/// <summary>
/// Drives a <see cref="LevelSession"/> through real physics with cell-based conditions
/// ("walk until column 13", "shift once below row 5") so solution scripts don't depend on
/// exact timings and survive tuning changes.
/// </summary>
internal sealed class Bot
{
    private const float Frame = 1f / 60f;
    private const float Timeout = 5f;

    private readonly FakeInput _input = new();

    public Bot(LevelData level) => Session = new LevelSession(level, _input);

    public LevelSession Session { get; }
    public LevelOutcome Outcome => Session.Outcome;
    public bool Playing => Outcome == LevelOutcome.Playing;

    /// <summary>The player's current grid cell (row 0 at the top).</summary>
    public Cell Cell
    {
        get
        {
            var level = Session.Level;
            var p = Session
                .World.GetComponent<Transform2D>(Session.World.GetEntity("player"))
                .Position;
            var column = (int)MathF.Floor((p.X - level.Origin.X) / Tuning.TileSize);
            var row = (int)MathF.Floor(level.Height - (p.Y - level.Origin.Y) / Tuning.TileSize);
            return new Cell(column, row);
        }
    }

    public System.Numerics.Vector2 Position =>
        Session.World.GetComponent<Transform2D>(Session.World.GetEntity("player")).Position;

    public bool Grounded =>
        Session
            .World.GetComponent<CharacterController2D>(Session.World.GetEntity("player"))
            .IsGrounded;

    /// <summary>Shift now and let the cooldown pass (the player keeps moving meanwhile).</summary>
    public Bot Shift(Keys key)
    {
        ShiftNow(key);
        return Wait(Tuning.ShiftCooldown);
    }

    /// <summary>Shift on exactly this frame, e.g. in mid-air.</summary>
    public Bot ShiftNow(Keys key)
    {
        _input.Tap(key);
        Step();
        Assert.True(!Playing || Session.Gravity.Cooldown > 0f, $"Shift {key} was not accepted.");
        return this;
    }

    /// <summary>Runs until the player is resting on the current floor.</summary>
    public Bot Settle() => WaitUntil(() => Grounded && IsSlow(), "settle");

    /// <summary>Holds a walk key until <paramref name="until"/> holds for the player's cell.</summary>
    public Bot Walk(Keys key, Func<Cell, bool> until)
    {
        _input.Hold(key);
        try
        {
            return WaitUntil(() => until(Cell), $"walk {key}");
        }
        finally
        {
            _input.Release(key);
        }
    }

    /// <summary>
    /// Holds a walk key until the player stops moving, i.e. walks into a wall or corner.
    /// Like a human player, this lines the parcel up exactly with the wall.
    /// </summary>
    public Bot WalkToWall(Keys key)
    {
        _input.Hold(key);
        try
        {
            // WaitUntil checks before each step, so start from a position we can't be at.
            var last = new System.Numerics.Vector2(float.NaN);
            return WaitUntil(
                () =>
                {
                    var now = Position;
                    var stopped = (now - last).LengthSquared() < 1e-10f;
                    last = now;
                    return stopped;
                },
                $"walk {key} to a wall"
            );
        }
        finally
        {
            _input.Release(key);
        }
    }

    /// <summary>
    /// Holds a walk key until the parcel is entirely inside <paramref name="row"/> (or
    /// <paramref name="column"/>), so it can fly along a corridor without clipping its edges.
    /// </summary>
    public Bot WalkInto(Keys key, int? row = null, int? column = null)
    {
        var level = Session.Level;
        var slack = (Tuning.TileSize - Tuning.PlayerSize) / 2f;
        _input.Hold(key);
        try
        {
            return WaitUntil(
                () =>
                {
                    var p = Position;
                    var okRow =
                        row is not { } r
                        || MathF.Abs(p.Y - level.CellCenter(new Cell(0, r)).Y) <= slack;
                    var okColumn =
                        column is not { } c
                        || MathF.Abs(p.X - level.CellCenter(new Cell(c, 0)).X) <= slack;
                    return okRow && okColumn;
                },
                $"walk {key} into row {row} / column {column}"
            );
        }
        finally
        {
            _input.Release(key);
        }
    }

    /// <summary>Runs until <paramref name="condition"/> holds for the player's cell.</summary>
    public Bot WaitFor(Func<Cell, bool> condition) => WaitUntil(() => condition(Cell), "wait");

    public Bot Wait(float seconds)
    {
        for (var t = 0f; t < seconds && Playing; t += Frame)
            Step();
        return this;
    }

    private Bot WaitUntil(Func<bool> condition, string what)
    {
        for (var t = 0f; t < Timeout; t += Frame)
        {
            if (!Playing || condition())
                return this;
            Step();
        }

        Assert.Fail($"Timed out waiting to {what}; player at {Cell}, outcome {Outcome}.");
        return this;
    }

    private bool IsSlow() =>
        Session
            .World.GetComponent<Velocity2D>(Session.World.GetEntity("player"))
            .Linear.LengthSquared() < 1e-4f;

    private void Step()
    {
        Session.Update(Frame);
        _input.EndFrame();
    }
}
