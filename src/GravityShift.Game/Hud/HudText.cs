using GravityShift.Systems;

namespace GravityShift.Hud;

/// <summary>The HUD's wording, kept separate from rendering so it is easy to test and tweak.</summary>
public static class HudText
{
    public static string Title(int levelIndex, int levelCount, string name) =>
        $"{levelIndex + 1}/{levelCount}  {name}";

    public static string Counter(int shifts, int par, int? best) =>
        (par > 0 ? $"Shifts {shifts}   Par {par}" : $"Shifts {shifts}")
        + (best is { } b ? $"   Best {b}" : "");

    public static string Result(LevelOutcome outcome, int shifts, int par) =>
        outcome switch
        {
            LevelOutcome.Delivered when par > 0 && shifts < par =>
                $"Delivered in {Plural(shifts)}. Under par!",
            LevelOutcome.Delivered when par > 0 && shifts == par =>
                $"Delivered in {Plural(shifts)}. Par!",
            LevelOutcome.Delivered when par > 0 =>
                $"Delivered in {Plural(shifts)}. Par is {par}, press R to retry",
            LevelOutcome.Delivered => $"Delivered in {Plural(shifts)}!",
            LevelOutcome.Shredded => "Shredded! Back to the start...",
            LevelOutcome.Lost => "Lost in the post! Back to the start...",
            _ => "",
        };

    private static string Plural(int shifts) => shifts == 1 ? "1 shift" : $"{shifts} shifts";
}
