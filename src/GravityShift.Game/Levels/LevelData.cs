using System.Numerics;

namespace GravityShift.Levels;

/// <summary>A grid cell, with row 0 at the <em>top</em> of the level (matching Yaeger's <c>Tilemap</c>).</summary>
public readonly record struct Cell(int Column, int Row);

/// <summary>A parsed level: plain data, no ECS. Turned into entities by <see cref="LevelSession"/>.</summary>
public sealed class LevelData
{
    public required string Name { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Row-major solidity grid; <c>true</c> means wall.</summary>
    public required bool[] Solid { get; init; }

    /// <summary>
    /// Fewest shifts that solve the level (from <c>tools/level_solver.py</c>, proven by
    /// <c>LevelSolutionTests</c>). 0 means not set.
    /// </summary>
    public int Par { get; init; }

    public required Cell Spawn { get; init; }
    public required IReadOnlyList<Cell> Goals { get; init; }
    public required IReadOnlyList<Cell> Hazards { get; init; }

    /// <summary>Conveyor belt tiles (solid) and the world direction each one pushes.</summary>
    public IReadOnlyDictionary<Cell, Vector2> Belts { get; init; } =
        new Dictionary<Cell, Vector2>();

    /// <summary>Gravity-lock cells: no shifting while the parcel overlaps one.</summary>
    public IReadOnlyList<Cell> Locks { get; init; } = [];

    public bool IsSolid(int column, int row) => Solid[row * Width + column];

    /// <summary>The belt direction at <paramref name="cell"/>, or null if it isn't a belt.</summary>
    public Vector2? BeltAt(Cell cell) =>
        Belts.TryGetValue(cell, out var direction) ? direction : null;

    /// <summary>The grid cell containing a world position (may be outside the level).</summary>
    public Cell CellAt(Vector2 world)
    {
        var local = (world - Origin) / Tuning.TileSize;
        return new Cell((int)MathF.Floor(local.X), Height - 1 - (int)MathF.Floor(local.Y));
    }

    /// <summary>World size of the whole level.</summary>
    public Vector2 WorldSize => new Vector2(Width, Height) * Tuning.TileSize;

    /// <summary>World position of the level's bottom-left corner. Levels are centred on the origin.</summary>
    public Vector2 Origin => -WorldSize / 2f;

    /// <summary>World-space centre of a cell.</summary>
    public Vector2 CellCenter(Cell cell) =>
        Origin + new Vector2(cell.Column + 0.5f, Height - cell.Row - 0.5f) * Tuning.TileSize;
}
