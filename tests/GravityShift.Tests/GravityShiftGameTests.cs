using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Input;

namespace GravityShift.Tests;

/// <summary>Shift counting, result banners and level progression on the top-level game.</summary>
public class GravityShiftGameTests
{
    private const float Frame = 1f / 60f;

    // A tiny room: shift right (E) and the parcel falls straight into the chute.
    private static readonly LevelData Quick = LevelParser.Parse(
        "Quick",
        ["#####", "#...#", "#P..G", "#####"],
        par: 1
    );

    // Shift left (Q) and the parcel lands in a shredder.
    private static readonly LevelData Deadly = LevelParser.Parse(
        "Deadly",
        ["######", "#X...G", "#X.P.#", "######"],
        par: 1
    );

    [Fact]
    public void Delivering_ShowsResultThenAdvances()
    {
        var input = new FakeInput();
        var game = new GravityShiftGame(input, levels: [Quick, Deadly]);
        (LevelData Level, LevelOutcome Outcome, int Shifts)? ended = null;
        game.LevelEnded += (l, o, s) => ended = (l, o, s);

        Tap(game, input, Keys.E);
        RunUntil(game, input, () => game.Banner.Length > 0);

        Assert.Equal((Quick, LevelOutcome.Delivered, 1), ended);
        Assert.Equal("Delivered in 1 shift. Par!", game.Banner);
        Assert.Equal(1, game.BestShifts(0));
        Assert.Equal(0, game.LevelIndex); // still showing the result

        Run(game, input, Tuning.DeliveredPause + Frame);

        Assert.Equal(1, game.LevelIndex);
        Assert.Equal("", game.Banner);
        Assert.Equal(0, game.Shifts);
    }

    [Fact]
    public void Shredding_ShowsResultThenRestartsSameLevel()
    {
        var input = new FakeInput();
        var game = new GravityShiftGame(input, levels: [Deadly]);

        Tap(game, input, Keys.Q);
        RunUntil(game, input, () => game.Banner.Length > 0);

        Assert.Equal("Shredded! Back to the start...", game.Banner);
        Assert.Null(game.BestShifts(0));

        Run(game, input, Tuning.FailedPause + Frame);

        Assert.Equal(0, game.LevelIndex);
        Assert.Equal(LevelOutcome.Playing, game.Session.Outcome);
        Assert.Equal(0, game.Shifts);
    }

    [Fact]
    public void Restart_ResetsTheCounter_AndKeepsTheBest()
    {
        var input = new FakeInput();
        var game = new GravityShiftGame(input, levels: [Quick]);
        Tap(game, input, Keys.E);
        RunUntil(game, input, () => game.Banner.Length > 0);
        Run(game, input, Tuning.DeliveredPause + Frame); // the "all delivered" screen
        Tap(game, input, Keys.Enter); // to the level select
        Tap(game, input, Keys.Enter); // play it again

        Tap(game, input, Keys.W);
        Assert.Equal(1, game.Shifts);
        Tap(game, input, Keys.R);

        Assert.Equal(0, game.Shifts);
        Assert.Equal(1, game.BestShifts(0));
    }

    [Fact]
    public void RejectedShifts_AreNotCounted()
    {
        var input = new FakeInput();
        var game = new GravityShiftGame(input, levels: [Quick]);

        Tap(game, input, Keys.W);
        Tap(game, input, Keys.W); // still on cooldown

        Assert.Equal(1, game.Shifts);
    }

    private static void Tap(GravityShiftGame game, FakeInput input, Keys key)
    {
        input.Tap(key);
        game.Update(Frame);
        input.EndFrame();
    }

    private static void Run(GravityShiftGame game, FakeInput input, float seconds)
    {
        for (var t = 0f; t < seconds; t += Frame)
        {
            game.Update(Frame);
            input.EndFrame();
        }
    }

    private static void RunUntil(GravityShiftGame game, FakeInput input, Func<bool> condition)
    {
        for (var t = 0f; t < 5f && !condition(); t += Frame)
        {
            game.Update(Frame);
            input.EndFrame();
        }

        Assert.True(condition(), "Timed out.");
    }
}
