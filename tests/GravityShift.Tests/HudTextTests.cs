using GravityShift.Hud;
using GravityShift.Systems;

namespace GravityShift.Tests;

public class HudTextTests
{
    [Fact]
    public void Title_IsOneBased() =>
        Assert.Equal("3/8  Sorting Office", HudText.Title(2, 8, "Sorting Office"));

    [Theory]
    [InlineData(0, 2, null, "Shifts 0   Par 2")]
    [InlineData(3, 2, 2, "Shifts 3   Par 2   Best 2")]
    [InlineData(1, 0, null, "Shifts 1")]
    public void Counter(int shifts, int par, int? best, string expected) =>
        Assert.Equal(expected, HudText.Counter(shifts, par, best));

    [Theory]
    [InlineData(LevelOutcome.Delivered, 1, 2, "Delivered in 1 shift. Under par!")]
    [InlineData(LevelOutcome.Delivered, 2, 2, "Delivered in 2 shifts. Par!")]
    [InlineData(LevelOutcome.Delivered, 4, 2, "Delivered in 4 shifts. Par is 2, press R to retry")]
    [InlineData(LevelOutcome.Delivered, 4, 0, "Delivered in 4 shifts!")]
    [InlineData(LevelOutcome.Shredded, 4, 2, "Shredded! Back to the start...")]
    [InlineData(LevelOutcome.Lost, 0, 2, "Lost in the post! Back to the start...")]
    [InlineData(LevelOutcome.Playing, 0, 2, "")]
    public void Result(LevelOutcome outcome, int shifts, int par, string expected) =>
        Assert.Equal(expected, HudText.Result(outcome, shifts, par));
}
