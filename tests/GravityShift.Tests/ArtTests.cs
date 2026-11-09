using System.Runtime.CompilerServices;
using GravityShift.Levels;

namespace GravityShift.Tests;

public class ArtTests
{
    private static string Wwwroot([CallerFilePath] string here = "") =>
        Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(here)!, "../../src/GravityShift.Web/wwwroot")
        );

    public static TheoryData<string> Textures() => [.. Art.All];

    [Theory]
    [MemberData(nameof(Textures))]
    public void EveryTextureIsCommitted(string path) =>
        Assert.True(
            File.Exists(Path.Combine(Wwwroot(), path)),
            $"Missing {path}; run tools/generate_art.py."
        );

    [Fact]
    public void WallTile_MarksTheSidesFacingTheRoom()
    {
        var level = LevelParser.Parse("art", ["#####", "#..G#", "#.#.#", "#P..#", "#####"]);

        // The pillar in the middle is open on every side.
        Assert.Equal(
            Art.OpenUp | Art.OpenRight | Art.OpenDown | Art.OpenLeft,
            Art.WallTile(level, 2, 2)
        );
        // The floor below the room is open only upwards; corners touch no room cell.
        Assert.Equal(Art.OpenUp, Art.WallTile(level, 2, 4));
        Assert.Equal(0, Art.WallTile(level, 0, 0));
        // The left wall faces right; outside the map counts as wall.
        Assert.Equal(Art.OpenRight, Art.WallTile(level, 0, 2));
    }

    [Fact]
    public void LevelSession_UsesTheArt()
    {
        var bot = new Bot(LevelCatalog.All[0]);
        var world = bot.Session.World;

        Assert.Equal(
            Art.Parcel,
            world.GetComponent<Yaeger.Graphics.Sprite>(world.GetEntity("parcel")).TexturePath
        );
        Assert.Equal(
            Art.Walls,
            world
                .GetComponent<Yaeger.Graphics.Tilemap>(world.GetEntity("walls"))
                .Tileset.TexturePath
        );
    }
}
