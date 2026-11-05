using GravityShift.Gravity;
using GravityShift.Levels;
using Yaeger.Input;

namespace GravityShift.Tests;

public class BeltAndLockTests
{
    // A long floor belt running right, with walls at both ends.
    private static readonly LevelData Belt = LevelParser.Parse(
        "Belt",
        ["##########", "#.......G#", "#..P.....#", "#>>>>>>>>#", "##########"]
    );

    // The parcel stands on a belt that points along gravity: it's just a floor.
    private static readonly LevelData UpBelt = LevelParser.Parse(
        "UpBelt",
        ["#######", "#....G#", "#..P..#", "#..^..#", "#######"]
    );

    // A belt in the right wall: a lift once the right wall is the floor.
    private static readonly LevelData Lift = LevelParser.Parse(
        "Lift",
        ["######", "#...G#", "#...^#", "#...^#", "#...^#", "#P..^#", "######"]
    );

    // A lock in the middle of the floor; walk into it, then out the other side.
    private static readonly LevelData Locked = LevelParser.Parse(
        "Locked",
        ["########", "#.....G#", "#......#", "#P.LL..#", "########"]
    );

    [Fact]
    public void FloorBelt_CarriesTheParcel()
    {
        var bot = new Bot(Belt);
        var start = bot.Position.X;

        bot.Wait(0.5f);

        Assert.True(bot.Position.X > start + 0.4f, $"Moved {bot.Position.X - start}");
    }

    [Fact]
    public void WalkingAgainstABelt_StillGoesWithIt()
    {
        var bot = new Bot(Belt);
        var start = bot.Position.X;

        // Holding Left the whole time, the parcel still reaches the right-hand end.
        bot.Walk(Keys.Left, c => c.Column >= 6);

        Assert.True(bot.Position.X > start, "Belts are one-way");
    }

    [Fact]
    public void BeltAlongGravity_DoesNothing()
    {
        var bot = new Bot(UpBelt).Settle();
        var start = bot.Position;

        bot.Wait(0.5f);

        Assert.True(
            System.Numerics.Vector2.Distance(start, bot.Position) < 1e-4f,
            $"Moved from {start} to {bot.Position}"
        );
    }

    [Fact]
    public void WallBelt_BecomesALift_AfterShiftingOntoIt()
    {
        var bot = new Bot(Lift);

        bot.Shift(Keys.E).Settle();

        Assert.Equal(GravityShift.Systems.LevelOutcome.Delivered, bot.Outcome);
    }

    [Fact]
    public void Lock_RefusesShifts_UntilYouWalkOut()
    {
        var bot = new Bot(Locked).Settle();
        var refused = 0;
        bot.Session.GravityShift.ShiftLocked += () => refused++;

        bot.WalkInto(Keys.Right, column: 3).TryShift(Keys.W);
        Assert.True(bot.Session.Gravity.IsLocked);
        Assert.Equal(GravityDirection.Down, bot.Session.Gravity.Direction);
        Assert.Equal(1, refused);

        bot.WalkInto(Keys.Right, column: 6).Shift(Keys.W);
        Assert.False(bot.Session.Gravity.IsLocked);
        Assert.Equal(GravityDirection.Up, bot.Session.Gravity.Direction);
    }
}
