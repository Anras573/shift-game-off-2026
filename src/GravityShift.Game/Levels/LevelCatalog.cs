namespace GravityShift.Levels;

/// <summary>
/// The built-in levels, in play order. Rooms are 16×16 so they stay fully on screen at any
/// camera rotation. Swap this for Tiled (<c>TiledMapLoader</c>) once level count grows.
/// </summary>
public static class LevelCatalog
{
    public static IReadOnlyList<LevelData> All { get; } =
    [
        // Teaches: E makes the right wall the floor. Then walk "right" (up) to the chute.
        LevelParser.Parse(
            "First Day on the Night Shift",
            [
                "################",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............G",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#P.............#",
                "################",
            ]
        ),
        // Teaches: W flips to the ceiling. Shifting toward the shredders (E) is fatal.
        LevelParser.Parse(
            "Mind the Shredder",
            [
                "################",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#PXXXXXXXXXXXG.#",
                "################",
            ]
        ),
        // Teaches: chaining shifts. W up the corridor, across, Q onto the right wall,
        // walk down, W across to the box, Q up into it.
        LevelParser.Parse(
            "Sorting Office",
            [
                "################",
                "#..............#",
                "#.######.......#",
                "#.#G...#.......#",
                "#.#....#.......#",
                "#.#....#.......#",
                "#.#....#.......#",
                "#.#.####.......#",
                "#.#............#",
                "#.#............#",
                "#.#............#",
                "#.#............#",
                "#.#............#",
                "#.#............#",
                "#P#XXXXXXXXXXXX#",
                "################",
            ]
        ),
    ];
}
