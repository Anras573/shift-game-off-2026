using GravityShift.Levels;
using Yaeger.Graphics;

namespace GravityShift.Menus;

/// <summary>One line of a menu screen. <see cref="Level"/> makes it clickable.</summary>
public readonly record struct MenuLine(
    string Text,
    int Size,
    Color Color,
    int? Level = null,
    bool Selected = false
);

/// <summary>The menus' wording and layout, kept apart from rendering so it is easy to test.</summary>
public static class MenuText
{
    public static readonly Color Accent = new(255, 214, 92);
    public static readonly Color Body = new(230, 230, 240);
    public static readonly Color Dim = new(150, 154, 175);

    public static IReadOnlyList<MenuLine> Title() =>
        [
            new("GRAVITY SHIFT", 52, Accent),
            new("Dead Letter Office", 26, Body),
            new("", 14, Body),
            new("You're a parcel on the night shift.", 18, Dim),
            new("Shift gravity to fall into the glowing chute.", 18, Dim),
            new("Mind the shredders. No shifting in purple zones.", 18, Dim),
            new("", 14, Body),
            new("Press Enter (or tap ↑) to start", 22, Accent),
            new("A/D walk · Q/E shift gravity · W flip · R restart · M mute", 16, Dim),
        ];

    public static IReadOnlyList<MenuLine> LevelSelect(
        IReadOnlyList<LevelData> levels,
        int selected,
        Func<int, int?> best
    )
    {
        var lines = new List<MenuLine> { new("Choose a level", 32, Accent), new("", 8, Body) };
        for (var i = 0; i < levels.Count; i++)
        {
            var isSelected = i == selected;
            lines.Add(
                new(
                    LevelRow(i, levels[i], best(i), isSelected),
                    20,
                    isSelected ? Accent : Body,
                    Level: i,
                    Selected: isSelected
                )
            );
        }
        lines.Add(new("", 8, Body));
        lines.Add(new("↑/↓ choose · Enter play · Esc back", 16, Dim));
        return lines;
    }

    public static string LevelRow(int index, LevelData level, int? best, bool selected) =>
        $"{(selected ? "▶ " : "   ")}{index + 1}. {level.Name}"
        + (level.Par > 0 ? $"   par {level.Par}" : "")
        + (best is { } b ? $"   ✓ {b}" : "");

    public static IReadOnlyList<MenuLine> Complete(
        IReadOnlyList<LevelData> levels,
        Func<int, int?> best
    )
    {
        var delivered = Enumerable.Range(0, levels.Count).Where(i => best(i) is not null).ToList();
        var shifts = delivered.Sum(i => best(i)!.Value);
        var par = delivered.Sum(i => levels[i].Par);
        var atPar = delivered.Count(i => best(i) <= levels[i].Par);
        return
        [
            new("All parcels delivered!", 40, Accent),
            new("The night shift is over.", 22, Body),
            new("", 14, Body),
            new($"Total shifts {shifts}  ·  total par {par}", 20, Body),
            new($"{atPar} of {levels.Count} levels at or under par", 20, Body),
            new("", 14, Body),
            new("Press Enter for the level select", 22, Accent),
        ];
    }
}
