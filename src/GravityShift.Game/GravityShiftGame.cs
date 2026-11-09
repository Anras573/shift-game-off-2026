using GravityShift.Audio;
using GravityShift.Hud;
using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Graphics;
using Yaeger.Input;
using Yaeger.Platform;

namespace GravityShift;

/// <summary>
/// Top-level game: owns level progression, the shift counter and the HUD, and delegates each
/// frame to the current <see cref="LevelSession"/>. Platform hosts (desktop / browser)
/// construct one of these with their own render surface and input, then call
/// <see cref="Update"/> and <see cref="Render"/> every frame.
/// </summary>
public sealed class GravityShiftGame
{
    private readonly IInputState _input;
    private readonly IRenderSurface? _renderSurface;
    private readonly IViewport? _viewport;
    private readonly IReadOnlyList<LevelData> _levels;
    private readonly int?[] _bestShifts;
    private readonly HudOverlay? _hud;
    private readonly bool _reducedMotion;
    private readonly GameSounds? _sounds;

    // Set while the result banner is showing; the level changes when the timer runs out.
    private float _resultTimer;
    private int _nextLevel;

    public GravityShiftGame(
        IInputState input,
        IRenderSurface? renderSurface = null,
        IViewport? viewport = null,
        IReadOnlyList<LevelData>? levels = null,
        ITextRenderSurface? textSurface = null,
        IFontHandle? hudFont = null,
        bool reducedMotion = false,
        GameSounds? sounds = null
    )
    {
        _reducedMotion = reducedMotion;
        _sounds = sounds;
        _input = input;
        _renderSurface = renderSurface;
        _viewport = viewport;
        _levels = levels ?? LevelCatalog.All;
        if (_levels.Count == 0)
            throw new ArgumentException("At least one level is required.", nameof(levels));
        _bestShifts = new int?[_levels.Count];

        if (
            renderSurface is not null
            && viewport is not null
            && textSurface is not null
            && hudFont is not null
        )
            _hud = new HudOverlay(renderSurface, textSurface, viewport, hudFont);

        Session = CreateSession(0);
        _sounds?.StartMusic();
    }

    public int LevelIndex { get; private set; }
    public int LevelCount => _levels.Count;
    public LevelSession Session { get; private set; }

    /// <summary>Shifts used so far in the current attempt.</summary>
    public int Shifts => Session.Gravity.ShiftCount;

    /// <summary>The result banner currently showing, or empty while playing.</summary>
    public string Banner { get; private set; } = "";

    /// <summary>Fewest shifts the current level has been delivered in this session.</summary>
    public int? BestShifts(int levelIndex) => _bestShifts[levelIndex];

    /// <summary>The HUD, when the host provided text rendering.</summary>
    public HudOverlay? Hud => _hud;

    /// <summary>Sound effects and music, when the host provided audio.</summary>
    public GameSounds? Sounds => _sounds;

    /// <summary>Raised when a level starts (including restarts).</summary>
    public event Action<LevelData>? LevelStarted;

    /// <summary>Raised once when a level ends: how it ended and how many shifts it took.</summary>
    public event Action<LevelData, LevelOutcome, int>? LevelEnded;

    public void Update(float deltaTime)
    {
        if (_input.WasKeyPressed(Keys.M))
            _sounds?.ToggleMute();
        _sounds?.Update(deltaTime);

        if (_input.WasKeyPressed(Keys.R))
        {
            LoadLevel(LevelIndex);
            return;
        }

        // Keeps the camera easing while the banner shows; the level itself is frozen.
        Session.Update(deltaTime);

        if (Banner.Length > 0)
        {
            _resultTimer -= deltaTime;
            if (_resultTimer <= 0f)
                LoadLevel(_nextLevel);
        }
        else if (Session.Outcome != LevelOutcome.Playing)
        {
            EndLevel(Session.Outcome);
        }

        UpdateHud();
    }

    public void Render()
    {
        Session.Render();
        _hud?.Render();
    }

    public void LoadLevel(int index)
    {
        Banner = "";
        Session = CreateSession(index);
        UpdateHud();
        LevelStarted?.Invoke(Session.Level);
    }

    private void EndLevel(LevelOutcome outcome)
    {
        var level = Session.Level;
        if (outcome == LevelOutcome.Delivered)
        {
            _bestShifts[LevelIndex] = Math.Min(_bestShifts[LevelIndex] ?? int.MaxValue, Shifts);
            _nextLevel = (LevelIndex + 1) % _levels.Count;
            _resultTimer = Tuning.DeliveredPause;
        }
        else
        {
            _nextLevel = LevelIndex;
            _resultTimer = Tuning.FailedPause;
        }

        Banner = HudText.Result(outcome, Shifts, level.Par);
        _sounds?.LevelEnded(outcome);
        LevelEnded?.Invoke(level, outcome, Shifts);
    }

    private void UpdateHud() =>
        _hud?.Set(
            HudText.Title(LevelIndex, _levels.Count, Session.Level.Name),
            HudText.Counter(Shifts, Session.Level.Par, _bestShifts[LevelIndex]),
            Banner
        );

    private LevelSession CreateSession(int index)
    {
        LevelIndex = index;
        var session = new LevelSession(
            _levels[index],
            _input,
            _renderSurface,
            _viewport,
            _reducedMotion
        );
        if (_sounds is { } sounds)
        {
            session.GravityShift.Shifted += sounds.Shifted;
            session.GravityShift.ShiftLocked += sounds.ShiftLocked;
            session.Landed += sounds.Landed;
        }
        return session;
    }
}
