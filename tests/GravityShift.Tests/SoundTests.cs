using GravityShift.Audio;
using GravityShift.Levels;
using Yaeger.Input;

namespace GravityShift.Tests;

public class SoundTests
{
    private const float Frame = 1f / 60f;

    private static readonly LevelData Quick = LevelParser.Parse(
        "Quick",
        ["#####", "#...#", "#P..G", "#####"],
        par: 1
    );

    private static readonly LevelData Deadly = LevelParser.Parse(
        "Deadly",
        ["######", "#X...G", "#X.P.#", "######"],
        par: 1
    );

    private static (GravityShiftGame Game, FakeInput Input, FakeAudio Audio) Start(LevelData level)
    {
        var audio = new FakeAudio();
        var sounds = GameSounds.LoadAsync(audio).GetAwaiter().GetResult();
        var input = new FakeInput();
        return (new GravityShiftGame(input, levels: [level], sounds: sounds), input, audio);
    }

    [Fact]
    public void Music_LoopsFromTheStart()
    {
        var (_, _, audio) = Start(Quick);

        Assert.Equal(GameSounds.MusicPath, audio.Music!.Path);
        Assert.True(audio.Music.Playing);
        Assert.True(audio.Music.Looping);
    }

    [Fact]
    public void Delivering_PlaysShiftThenDing()
    {
        var (game, input, audio) = Start(Quick);

        Tap(game, input, Keys.E);
        RunUntil(game, input, () => game.Banner.Length > 0);

        Assert.Equal(GameSounds.ShiftPath, audio.PlayedPaths.First());
        Assert.Equal(GameSounds.DeliverPath, audio.PlayedPaths.Last());
        Assert.Equal(1, audio.PlayedPaths.Count(p => p == GameSounds.DeliverPath));
    }

    [Fact]
    public void Shredding_Crunches()
    {
        var (game, input, audio) = Start(Deadly);

        Tap(game, input, Keys.Q);
        RunUntil(game, input, () => game.Banner.Length > 0);

        Assert.Contains(GameSounds.ShredPath, audio.PlayedPaths);
        Assert.DoesNotContain(GameSounds.DeliverPath, audio.PlayedPaths);
    }

    [Fact]
    public void RefusedShift_Buzzes()
    {
        var (game, input, audio) = Start(Quick);
        game.Session.Gravity.LockCount = 1;

        Tap(game, input, Keys.E);

        Assert.Equal([GameSounds.LockedPath], audio.PlayedPaths);
    }

    [Fact]
    public void HardLandings_AreLouderThanSoftOnes()
    {
        var audio = new FakeAudio();
        var sounds = GameSounds.LoadAsync(audio).GetAwaiter().GetResult();

        sounds.Landed(Tuning.LandSoundMinSpeed / 2); // a settle: silent
        sounds.Landed(1f);
        sounds.Landed(Tuning.TerminalVelocity);

        Assert.Equal(2, audio.Played.Count);
        Assert.True(audio.Played[1].Gain > audio.Played[0].Gain);
    }

    [Fact]
    public void M_TogglesMute()
    {
        var (game, input, audio) = Start(Quick);

        Tap(game, input, Keys.M);
        Assert.True(game.Sounds!.Muted);
        Assert.Equal(0f, audio.Mixer.MasterVolume);

        Tap(game, input, Keys.M);
        Assert.False(game.Sounds.Muted);
        Assert.Equal(1f, audio.Mixer.MasterVolume);
    }

    [Theory]
    [MemberData(nameof(Files))]
    public void EverySoundIsCommitted(string path) =>
        Assert.True(
            File.Exists(Path.Combine(Wwwroot(), path)),
            $"Missing {path}; run tools/generate_audio.py."
        );

    public static TheoryData<string> Files() =>
        [.. GameSounds.Effects, GameSounds.MusicPath, "audio/music.m4a"];

    private static string Wwwroot(
        [System.Runtime.CompilerServices.CallerFilePath] string here = ""
    ) =>
        Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(here)!, "../../src/GravityShift.Web/wwwroot")
        );

    private static void Tap(GravityShiftGame game, FakeInput input, Keys key)
    {
        input.Tap(key);
        game.Update(Frame);
        input.EndFrame();
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
