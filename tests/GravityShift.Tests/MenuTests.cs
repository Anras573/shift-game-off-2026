using System.Numerics;
using GravityShift.Levels;
using GravityShift.Menus;
using GravityShift.Systems;
using Yaeger.Input;

namespace GravityShift.Tests;

/// <summary>Title → level select → playing → complete, driven by keys and clicks.</summary>
public class MenuTests
{
    private const float Frame = 1f / 60f;

    private static readonly LevelData Quick = LevelParser.Parse(
        "Quick",
        ["#####", "#...#", "#P..G", "#####"],
        par: 1
    );

    private static readonly LevelData Other = LevelParser.Parse(
        "Other",
        ["#####", "#...#", "#..PG", "#####"],
        par: 1
    );

    private static (GravityShiftGame Game, FakeInput Input) Start()
    {
        var input = new FakeInput();
        return (
            new GravityShiftGame(input, levels: [Quick, Other], startScreen: GameScreen.Title),
            input
        );
    }

    [Fact]
    public void Title_EnterOpensLevelSelect_EscQuits()
    {
        var (game, input) = Start();
        var quit = false;
        game.QuitRequested += () => quit = true;

        Assert.Equal(GameScreen.Title, game.Screen);
        Tap(game, input, Keys.Escape);
        Assert.True(quit);

        Tap(game, input, Keys.Enter);
        Assert.Equal(GameScreen.LevelSelect, game.Screen);
    }

    [Fact]
    public void Title_ShiftKeysDoNothingToTheBackdrop()
    {
        var (game, input) = Start();

        Tap(game, input, Keys.E);
        Run(game, input, 1f);

        Assert.Equal(GameScreen.Title, game.Screen);
        Assert.Equal(0, game.Shifts);
        Assert.Equal(LevelOutcome.Playing, game.Session.Outcome);
    }

    [Fact]
    public void LevelSelect_MovesWrapsAndPreviewsTheSelection()
    {
        var (game, input) = Start();
        Tap(game, input, Keys.Enter);

        Tap(game, input, Keys.Down);
        Assert.Equal(1, game.LevelIndex);
        Assert.Equal("Other", game.Session.Level.Name); // the backdrop is the selection

        Tap(game, input, Keys.Down); // wraps
        Assert.Equal(0, game.LevelIndex);
        Tap(game, input, Keys.Up);
        Assert.Equal(1, game.LevelIndex);

        Tap(game, input, Keys.Enter);
        Assert.Equal(GameScreen.Playing, game.Screen);
        Assert.Equal("Other", game.Session.Level.Name);
    }

    [Fact]
    public void Playing_EscGoesBackToTheLevelSelect_ThenTitle()
    {
        var (game, input) = Start();
        Tap(game, input, Keys.Enter);
        Tap(game, input, Keys.Enter);
        Assert.Equal(GameScreen.Playing, game.Screen);

        Tap(game, input, Keys.Escape);
        Assert.Equal(GameScreen.LevelSelect, game.Screen);
        Tap(game, input, Keys.Escape);
        Assert.Equal(GameScreen.Title, game.Screen);
    }

    [Fact]
    public void DeliveringTheLastLevel_ShowsTheCompleteScreen()
    {
        var (game, input) = Start();
        Tap(game, input, Keys.Enter);
        Tap(game, input, Keys.Down);
        Tap(game, input, Keys.Enter); // play the last level

        Tap(game, input, Keys.E);
        RunUntil(game, input, () => game.Screen == GameScreen.Complete);

        Tap(game, input, Keys.Enter);
        Assert.Equal(GameScreen.LevelSelect, game.Screen);
        Assert.Equal(0, game.LevelIndex);
    }

    [Fact]
    public void Text_LevelSelectMarksTheSelectionAndBestScores()
    {
        var lines = MenuText.LevelSelect([Quick, Other], 1, i => i == 0 ? 1 : null);
        var rows = lines.Where(l => l.Level is not null).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("   1. Quick   par 1   ✓ 1", rows[0].Text);
        Assert.Equal("▶ 2. Other   par 1", rows[1].Text);
        Assert.True(rows[1].Selected);
    }

    [Fact]
    public void Text_CompleteSumsBestShiftsAndPar()
    {
        var lines = MenuText.Complete([Quick, Other], i => i == 0 ? 1 : 3);

        Assert.Contains(lines, l => l.Text == "Total shifts 4  ·  total par 2");
        Assert.Contains(lines, l => l.Text == "1 of 2 levels at or under par");
    }

    [Fact]
    public void Scale_ShrinksMenusToFitShortScreens()
    {
        var lines = MenuText.LevelSelect(LevelCatalog.All, 0, _ => null);

        Assert.Equal(1f, MenuOverlay.Scale(lines, new Vector2(1280, 800)));

        // A landscape phone: every line fits above the touch buttons along the bottom.
        var scale = MenuOverlay.Scale(lines, new Vector2(844, 390));
        var height = lines.Sum(l => l.Size + 8f) * scale + 2 * 28f;
        Assert.True(height <= 390f - 110f, $"{height}px at scale {scale}");
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
        for (var t = 0f; t < 6f && !condition(); t += Frame)
        {
            game.Update(Frame);
            input.EndFrame();
        }
        Assert.True(condition(), "Timed out.");
    }
}
