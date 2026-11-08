using System.Numerics;
using GravityShift.Components;
using GravityShift.Feel;
using GravityShift.Gravity;
using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Input;

namespace GravityShift.Tests;

public class FeelTests
{
    private static LevelData FirstLevel => LevelCatalog.All[0];

    // A tall drop onto the floor, so landings are hard.
    private static readonly LevelData TallDrop = LevelParser.Parse(
        "TallDrop",
        [
            "#####",
            "#.P.#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...#",
            "#...G",
            "#####",
        ]
    );

    [Fact]
    public void HardLanding_FiresOnce_AndSquashesAndShakes()
    {
        var bot = new Bot(TallDrop);
        var landings = new List<float>();
        bot.Session.Landed += landings.Add;

        bot.Settle();

        Assert.Single(landings);
        Assert.True(landings[0] > 2.5f, $"Impact {landings[0]}");
        Assert.True(bot.Session.Feel.Squash > 0.15f, $"Squash {bot.Session.Feel.Squash}");
        Assert.True(
            bot.Session.Feel.Shake.Trauma > 0.2f,
            $"Trauma {bot.Session.Feel.Shake.Trauma}"
        );
    }

    [Fact]
    public void SquashAndShake_SettleBackToRest()
    {
        var bot = new Bot(TallDrop).Settle();

        bot.Wait(1.2f);

        Assert.InRange(bot.Session.Feel.Squash, -0.01f, 0.01f);
        Assert.Equal(0f, bot.Session.Feel.Shake.Trauma);
        Assert.Equal(Vector2.Zero, Camera(bot).Position);
    }

    [Fact]
    public void Falling_StretchesTheParcel()
    {
        var bot = new Bot(TallDrop);

        bot.WaitFor(c => c.Row >= 8); // mid-fall, near top speed

        Assert.True(bot.Session.Feel.Squash < -0.05f, $"Squash {bot.Session.Feel.Squash}");
    }

    [Theory]
    [InlineData("E")] // land on the right wall
    [InlineData("W")] // land on the ceiling
    [InlineData("EW")] // right wall, then flip across to land on the left wall
    public void SquashedParcel_KeepsItsFeetOnTheFloor(string shifts)
    {
        var bot = new Bot(FirstLevel).Settle();
        foreach (var shift in shifts)
            bot.Shift(shift == 'E' ? Keys.E : Keys.W).Settle();
        var world = bot.Session.World;
        var body = world.GetComponent<Transform2D>(world.GetEntity("player")).Position;
        var parcel = world.GetComponent<Transform2D>(world.GetEntity("parcel"));
        var down = bot.Session.Gravity.Direction.ToVector();

        Assert.True(bot.Session.Feel.Squash > 0.05f, "Expected a squash on landing");
        // Visual height is Scale.Y (the sprite is rotated so its local Y is "up" on screen).
        var bodyFeet = Vector2.Dot(body, down) + Tuning.PlayerSize / 2f;
        var parcelFeet = Vector2.Dot(parcel.Position, down) + parcel.Scale.Y / 2f;
        Assert.Equal(bodyFeet, parcelFeet, 4);
        Assert.True(parcel.Scale.X > parcel.Scale.Y, "Squashed means wider than tall");
    }

    [Fact]
    public void ReducedMotion_TurnsOffShake_ButKeepsTheSquash()
    {
        var bot = new Bot(TallDrop, reducedMotion: true).Settle();

        Assert.Equal(0f, bot.Session.Feel.Shake.Trauma);
        Assert.Equal(Vector2.Zero, Camera(bot).Position);
        Assert.True(bot.Session.Feel.Squash > 0.15f);
    }

    [Fact]
    public void RefusedShiftInALock_FlashesTheLockAndShakes()
    {
        var level = LevelParser.Parse(
            "Locked",
            ["########", "#.....G#", "#......#", "#P.LL..#", "########"]
        );
        var bot = new Bot(level).Settle().WalkInto(Keys.Right, column: 3);
        var world = bot.Session.World;
        var lockEntity = world.Query<GravityLock, Sprite>().First().Item1;
        var before = world.GetComponent<Sprite>(lockEntity).Tint;

        bot.TryShift(Keys.W);

        Assert.True(bot.Session.Feel.Shake.Trauma > 0f);
        Assert.NotEqual(before, world.GetComponent<Sprite>(lockEntity).Tint);

        bot.Wait(Tuning.LockFlashTime + 0.1f);
        Assert.Equal(before, world.GetComponent<Sprite>(lockEntity).Tint);
    }

    [Fact]
    public void Shredding_ShakesHard()
    {
        var bot = new Bot(LevelCatalog.All.Single(l => l.Name == "Mind the Shredder"));

        bot.Shift(Keys.E).Settle(); // straight into the shredders
        bot.Session.Update(1f / 60f); // feel reacts on the next frame

        Assert.Equal(LevelOutcome.Shredded, bot.Outcome);
        Assert.True(
            bot.Session.Feel.Shake.Trauma > 0.6f,
            $"Trauma {bot.Session.Feel.Shake.Trauma}"
        );
    }

    [Fact]
    public void Delivering_ShrinksTheParcelIntoTheChute()
    {
        var bot = new Bot(FirstLevel).Shift(Keys.E).Settle().Walk(Keys.Right, _ => false);
        Assert.Equal(LevelOutcome.Delivered, bot.Outcome);

        for (var i = 0; i < 30; i++)
            bot.Session.Update(1f / 60f);

        var parcel = bot.Session.World.GetComponent<Transform2D>(
            bot.Session.World.GetEntity("parcel")
        );
        Assert.True(parcel.Scale.X < Tuning.PlayerSize * 0.05f, $"Scale {parcel.Scale}");
    }

    [Fact]
    public void ScreenShake_IsDeterministic_AndScalesWithTraumaSquared()
    {
        var a = new ScreenShake();
        var b = new ScreenShake();
        a.AddTrauma(0.5f);
        b.AddTrauma(1f);
        a.Update(0.01f);
        b.Update(0.01f);
        var expected = new ScreenShake();
        expected.AddTrauma(0.5f);
        expected.Update(0.01f);

        Assert.Equal(expected.Offset, a.Offset);
        // Same noise phase; offset ratio follows the squared trauma ratio.
        var ratio = a.Offset.Length() / b.Offset.Length();
        var expectedRatio = MathF.Pow(a.Trauma / b.Trauma, 2f);
        Assert.Equal(expectedRatio, ratio, 3);
    }

    private static Camera2D Camera(Bot bot) =>
        bot.Session.World.GetComponent<Camera2D>(bot.Session.World.GetEntity("camera"));
}
