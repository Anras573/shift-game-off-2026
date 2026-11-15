namespace GravityShift.Menus;

/// <summary>Which screen <see cref="GravityShiftGame"/> is showing.</summary>
public enum GameScreen
{
    Title,
    LevelSelect,
    Playing,

    /// <summary>Shown after the last level is delivered.</summary>
    Complete,
}
