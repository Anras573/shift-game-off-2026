using GravityShift.Levels;

namespace GravityShift.Tests;

public class LevelParserTests
{
    [Fact]
    public void Parse_ReadsWallsSpawnGoalsAndHazards()
    {
        var level = LevelParser.Parse("t", ["####", "#PG#", "#X.#", "####"]);

        Assert.Equal(4, level.Width);
        Assert.Equal(4, level.Height);
        Assert.True(level.IsSolid(0, 0));
        Assert.False(level.IsSolid(1, 1));
        Assert.Equal(new Cell(1, 1), level.Spawn);
        Assert.Equal([new Cell(2, 1)], level.Goals);
        Assert.Equal([new Cell(1, 2)], level.Hazards);
    }

    [Fact]
    public void CellCenter_PutsRowZeroAtTheTopAndCentresTheLevel()
    {
        var level = LevelParser.Parse("t", ["P.", ".G"]);
        var half = Tuning.TileSize / 2f;

        Assert.Equal(new System.Numerics.Vector2(-half, half), level.CellCenter(new Cell(0, 0)));
        Assert.Equal(new System.Numerics.Vector2(half, -half), level.CellCenter(new Cell(1, 1)));
    }

    [Theory]
    [InlineData("#G#")] // no spawn
    [InlineData("#P#")] // no goal
    [InlineData("PPG")] // two spawns
    [InlineData("PG|.")] // ragged rows
    [InlineData("P?G")] // unknown tile
    public void Parse_RejectsMalformedLevels(string rows) =>
        Assert.Throws<FormatException>(() => LevelParser.Parse("bad", rows.Split('|')));

    [Fact]
    public void Parse_ReadsBeltsAndLocks()
    {
        var level = LevelParser.Parse("t", ["><^v", "LPG#"]);

        Assert.Equal(System.Numerics.Vector2.UnitX, level.BeltAt(new Cell(0, 0)));
        Assert.Equal(-System.Numerics.Vector2.UnitX, level.BeltAt(new Cell(1, 0)));
        Assert.Equal(System.Numerics.Vector2.UnitY, level.BeltAt(new Cell(2, 0)));
        Assert.Equal(-System.Numerics.Vector2.UnitY, level.BeltAt(new Cell(3, 0)));
        Assert.Null(level.BeltAt(new Cell(3, 1)));
        Assert.False(level.IsSolid(0, 0)); // belts get their own colliders, not wall tiles
        Assert.Equal([new Cell(0, 1)], level.Locks);
    }

    [Fact]
    public void CellAt_IsTheInverseOfCellCenter()
    {
        var level = LevelCatalog.All[0];
        foreach (var cell in new[] { new Cell(0, 0), new Cell(7, 3), new Cell(15, 15) })
            Assert.Equal(cell, level.CellAt(level.CellCenter(cell)));
    }

    [Fact]
    public void Parse_StoresParAndRejectsNegativePar()
    {
        Assert.Equal(3, LevelParser.Parse("t", ["PG"], par: 3).Par);
        Assert.Equal(0, LevelParser.Parse("t", ["PG"]).Par);
        Assert.Throws<ArgumentOutOfRangeException>(() => LevelParser.Parse("t", ["PG"], par: -1));
    }

    [Fact]
    public void Catalog_LevelsAreFullyEnclosedExceptForGoals()
    {
        foreach (var level in LevelCatalog.All)
        {
            for (var column = 0; column < level.Width; column++)
            {
                AssertEdge(level, column, 0);
                AssertEdge(level, column, level.Height - 1);
            }
            for (var row = 0; row < level.Height; row++)
            {
                AssertEdge(level, 0, row);
                AssertEdge(level, level.Width - 1, row);
            }
        }
    }

    private static void AssertEdge(LevelData level, int column, int row) =>
        Assert.True(
            level.IsSolid(column, row) || level.Goals.Contains(new Cell(column, row)),
            $"'{level.Name}' has an open edge at ({column}, {row})."
        );
}
