namespace GravityShift.Levels;

/// <summary>
/// The built-in levels, in play order. Each one has a scripted play-through in
/// <c>LevelSolutionTests</c>, and a minimum shift count found with a grid solver. Rooms are 16×16 so they stay fully on screen at any
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
            ],
            par: 1
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
            ],
            par: 2
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
            ],
            par: 2
        ),
        // Teaches: shifting mid-air. Walk off the ledge and shift right while falling;
        // shifting on the ledge sends you into the shredders.
        LevelParser.Parse(
            "Return to Sender",
            [
                "################",
                "#.P...........X#",
                "#####.........X#",
                "#.............X#",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#..............G",
                "#.............X#",
                "#XXXXXXXXXXXXXX#",
                "################",
            ],
            par: 1
        ),
        // Fly along the corridor under the box; the far wall lines you up with its opening.
        LevelParser.Parse(
            "This Side Up",
            [
                "################",
                "#.....#........#",
                "#.....######...#",
                "#.....#GG..#...#",
                "#.....#....#...#",
                "#.....#....#...#",
                "#.....#..###...#",
                "#.....#........#",
                "#.....#........#",
                "#.....#######..#",
                "#..............#",
                "#XXXXX.........#",
                "#..............#",
                "#..............#",
                "#P.............#",
                "################",
            ],
            par: 2
        ),
        // Mid-air drop: walk down to the gap between the shelves, fly left and shift down
        // over the wide chute. Dropping too early lands on the lower shelf.
        LevelParser.Parse(
            "Express Post",
            [
                "################",
                "#P.............#",
                "####...........#",
                "#..............#",
                "#..............#",
                "#XXXXXXXXXXXXX.#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#........XXXXXX#",
                "#..............#",
                "#..............#",
                "#..............#",
                "#X.......XXXXXX#",
                "#X.......XXXXXX#",
                "##GGGGGGG#######",
            ],
            par: 3
        ),
        // Rise into the box, then work round inside it; the walls and the box lid shred.
        LevelParser.Parse(
            "Shredder Alley",
            [
                "################",
                "#X............X#",
                "#X.##########.X#",
                "#X.#XXXXXXXX#.X#",
                "#X.#........#.X#",
                "#X.#........#.X#",
                "#X.#...GG...#.X#",
                "#X.#...##...#.X#",
                "#X.#........#.X#",
                "#X.#........#.X#",
                "#X.####..####.X#",
                "#X............X#",
                "#X............X#",
                "#X......P.....X#",
                "################",
                "################",
            ],
            par: 3
        ),
        // Finale: a sorting maze of ledges and pockets; at least four shifts.
        LevelParser.Parse(
            "Dead Letter Office",
            [
                "################",
                "#......#.......#",
                "#.####.#.#####.#",
                "#.#XX#.#.#.G.#.#",
                "#.#..#...#.G.#.#",
                "#.#..#####.G.#.#",
                "#.#..........#.#",
                "#.######.#####.#",
                "#........#XXXX.#",
                "#........#.....#",
                "#XXXX....#.....#",
                "#.########.###.#",
                "#..........#X#.#",
                "#..........#.#.#",
                "#P.........#.#.#",
                "################",
            ],
            par: 4
        ),
    ];
}
