namespace GravityShift.Levels;

/// <summary>
/// Parses the ASCII level format used in <see cref="LevelCatalog"/>.
/// </summary>
/// <remarks>
/// Legend:
/// <list type="bullet">
/// <item><c>#</c> wall</item>
/// <item><c>.</c> or space: empty</item>
/// <item><c>P</c> parcel (player) spawn, exactly one</item>
/// <item><c>G</c> delivery chute (goal), at least one</item>
/// <item><c>X</c> shredder (hazard)</item>
/// </list>
/// Rows are listed top to bottom and must all be the same length.
/// </remarks>
public static class LevelParser
{
    public static LevelData Parse(string name, IReadOnlyList<string> rows)
    {
        if (rows.Count == 0)
            throw new FormatException($"Level '{name}' has no rows.");

        var width = rows[0].Length;
        if (width == 0)
            throw new FormatException($"Level '{name}' has an empty first row.");

        var height = rows.Count;
        var solid = new bool[width * height];
        Cell? spawn = null;
        var goals = new List<Cell>();
        var hazards = new List<Cell>();

        for (var row = 0; row < height; row++)
        {
            var line = rows[row];
            if (line.Length != width)
                throw new FormatException(
                    $"Level '{name}' row {row} is {line.Length} wide, expected {width}."
                );

            for (var column = 0; column < width; column++)
            {
                var cell = new Cell(column, row);
                switch (line[column])
                {
                    case '#':
                        solid[row * width + column] = true;
                        break;
                    case '.':
                    case ' ':
                        break;
                    case 'P':
                        if (spawn is not null)
                            throw new FormatException($"Level '{name}' has more than one 'P'.");
                        spawn = cell;
                        break;
                    case 'G':
                        goals.Add(cell);
                        break;
                    case 'X':
                        hazards.Add(cell);
                        break;
                    default:
                        throw new FormatException(
                            $"Level '{name}' has unknown tile '{line[column]}' at ({column}, {row})."
                        );
                }
            }
        }

        if (spawn is null)
            throw new FormatException($"Level '{name}' has no 'P' spawn.");
        if (goals.Count == 0)
            throw new FormatException($"Level '{name}' has no 'G' goal.");

        return new LevelData
        {
            Name = name,
            Width = width,
            Height = height,
            Solid = solid,
            Spawn = spawn.Value,
            Goals = goals,
            Hazards = hazards,
        };
    }
}
