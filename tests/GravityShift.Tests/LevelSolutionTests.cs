using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Input;

namespace GravityShift.Tests;

/// <summary>
/// A real-physics play-through for every level in <see cref="LevelCatalog"/>: proves each one
/// is solvable and catches tuning changes that would break it. Every new level needs an entry.
/// </summary>
/// <remarks>
/// Walk keys are screen-relative. In grid terms (row 0 at the top):
/// gravity Down: Right = column+; Up: Right = column-; Left: Right = row+; Right: Right = row-.
/// Q turns gravity Down → Left → Up → Right → Down, E the other way, W flips it.
/// </remarks>
public class LevelSolutionTests
{
    private static readonly Dictionary<string, Action<Bot>> Solutions = new()
    {
        ["First Day on the Night Shift"] = bot =>
            bot.Shift(Keys.E).Settle().Walk(Keys.Right, _ => false),

        ["Mind the Shredder"] = bot =>
            bot.Shift(Keys.W)
                .Settle()
                .Walk(Keys.Left, c => c.Column >= 13) // upside down: screen-left is world right
                .Shift(Keys.W)
                .Settle(),

        ["Sorting Office"] = bot =>
            bot.Shift(Keys.W) // up the corridor
                .Settle()
                .Walk(Keys.Left, c => c.Column >= 9) // along the ceiling, past the box
                .Shift(Keys.Q) // right wall
                .Settle()
                .Walk(Keys.Left, c => c.Row >= 10) // down the right wall
                .Shift(Keys.W) // across to the corridor wall
                .Settle()
                .Shift(Keys.Q) // up into the box
                .Settle(),

        ["Return to Sender"] = bot =>
            bot.Walk(Keys.Right, c => c.Row >= 3) // off the ledge
                .WaitFor(c => c.Row >= 5)
                .ShiftNow(Keys.E) // mid-air: the right wall's chute
                .Settle(),

        ["This Side Up"] = bot =>
            bot.Shift(Keys.E) // right wall
                .Settle()
                .WalkInto(Keys.Right, row: 7) // up into the corridor under the box
                .Shift(Keys.W) // fly left; the far wall lines you up with the opening
                .Settle()
                .Shift(Keys.Q) // rise into the box
                .Settle(),

        ["Express Post"] = bot =>
            bot.Shift(Keys.E) // right wall
                .Settle()
                .WalkInto(Keys.Left, row: 7) // down to the middle of the gap between the shelves
                .ShiftNow(Keys.W) // fly left...
                .WaitFor(c => c.Column <= 6)
                .ShiftNow(Keys.E) // ...and drop through mid-air
                .Settle(),

        ["Shredder Alley"] = bot =>
            bot.Shift(Keys.W) // rise into the box, stop under the chute's floor
                .Settle()
                .Shift(Keys.Q) // right wall of the box
                .Settle()
                .WalkInto(Keys.Right, row: 6) // up, short of the shredder lid
                .Shift(Keys.W) // across into the chute
                .Settle(),

        ["Dead Letter Office"] = bot =>
            bot.Shift(Keys.E) // along the bottom to the inner wall
                .Settle()
                .Walk(Keys.Right, c => c.Column >= 11) // up, then off the ledge to the right
                .Settle()
                .WalkToWall(Keys.Right) // up the right wall into the top corner
                .Shift(Keys.W) // along the top corridor
                .Settle()
                .Walk(Keys.Right, c => c.Column <= 6) // down, off the ledge into the left pocket
                .Settle()
                .WalkToWall(Keys.Left) // up, off the ledge, along the top to the left wall
                .Settle()
                .WalkInto(Keys.Right, row: 8) // down the left wall, above the shredders
                .Shift(Keys.W) // across the middle
                .Settle()
                .Walk(Keys.Right, c => c.Column >= 12) // up, off the ledge towards the office
                .Settle()
                .WalkToWall(Keys.Right) // up beside the office
                .Shift(Keys.W) // into the chute
                .Settle(),
    };

    public static TheoryData<string> LevelNames() => [.. LevelCatalog.All.Select(l => l.Name)];

    [Theory]
    [MemberData(nameof(LevelNames))]
    public void EveryLevelHasAWorkingSolution(string name)
    {
        Assert.True(Solutions.ContainsKey(name), $"Add a solution for '{name}'.");
        var bot = new Bot(LevelCatalog.All.Single(l => l.Name == name));

        Solutions[name](bot);

        Assert.Equal(LevelOutcome.Delivered, bot.Outcome);
    }

    [Fact]
    public void LevelNamesAreUnique() =>
        Assert.Equal(
            LevelCatalog.All.Count,
            LevelCatalog.All.Select(l => l.Name).Distinct().Count()
        );

    [Fact]
    public void ReturnToSender_ShiftingOnTheLedge_Shreds()
    {
        var bot = new Bot(LevelCatalog.All.Single(l => l.Name == "Return to Sender"));

        bot.Shift(Keys.E).Settle();

        Assert.Equal(LevelOutcome.Shredded, bot.Outcome);
    }

    [Fact]
    public void ExpressPost_DroppingTooEarly_Shreds()
    {
        var bot = new Bot(LevelCatalog.All.Single(l => l.Name == "Express Post"));

        bot.Shift(Keys.E)
            .Settle()
            .WalkInto(Keys.Left, row: 7)
            .ShiftNow(Keys.W)
            .Wait(Tuning.ShiftCooldown)
            .ShiftNow(Keys.E) // over the lower shelf, not the chute
            .Settle();

        Assert.Equal(LevelOutcome.Shredded, bot.Outcome);
    }
}
