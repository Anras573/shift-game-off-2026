using GravityShift.Audio;
using GravityShift.Hud;
using GravityShift.Levels;
using GravityShift.Menus;
using GravityShift.Systems;
using Yaeger.Graphics;
using Yaeger.Input;
using Yaeger.Platform;

namespace GravityShift;

/// <summary>
/// Top-level game: owns the screens (title, level select, playing, complete), level
/// progression, the shift counter, the HUD and the menus, and delegates each frame to the
/// current <see cref="LevelSession"/>; in the menus that session is the backdrop. Platform hosts (desktop / browser)
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
    private readonly MenuOverlay? _menu;
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
        GameSounds? sounds = null,
        GameScreen startScreen = GameScreen.Playing
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
        {
            _hud = new HudOverlay(renderSurface, textSurface, viewport, hudFont);
            _menu = new MenuOverlay(renderSurface, textSurface, viewport, hudFont);
        }

        Session = CreateSession(0);
        _sounds?.StartMusic();
        Show(startScreen == GameScreen.Complete ? GameScreen.Title : startScreen);
    }

    public static readonly Keys[] PreviousKeys = [Keys.Up, Keys.Left, Keys.A, Keys.Q];
    public static readonly Keys[] NextKeys = [Keys.Down, Keys.Right, Keys.D, Keys.S, Keys.E];
    public static readonly Keys[] ConfirmKeys = [Keys.Enter, Keys.Space, Keys.W];
    public static readonly Keys[] BackKeys = [Keys.Escape, Keys.R];

    public GameScreen Screen { get; private set; }

    /// <summary>The menus, when the host provided text rendering.</summary>
    public MenuOverlay? Menu => _menu;

    /// <summary>Raised when Esc is pressed on the title screen; desktop closes the window.</summary>
    public event Action? QuitRequested;

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

        switch (Screen)
        {
            case GameScreen.Title:
                UpdateTitle(deltaTime);
                return;
            case GameScreen.LevelSelect:
                UpdateLevelSelect(deltaTime);
                return;
            case GameScreen.Complete:
                UpdateComplete(deltaTime);
                return;
        }

        if (_input.WasKeyPressed(Keys.Escape))
        {
            ShowLevelSelect(LevelIndex);
            return;
        }

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
            {
                if (_nextLevel < 0)
                    Show(GameScreen.Complete);
                else
                    LoadLevel(_nextLevel);
            }
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
        if (Screen == GameScreen.Playing)
            _hud?.Render();
        else
            _menu?.Render();
    }

    /// <summary>Starts playing a level.</summary>
    public void LoadLevel(int index)
    {
        Screen = GameScreen.Playing;
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
            // After the last level: the "all delivered" screen (-1) instead of a next level.
            _nextLevel = LevelIndex + 1 < _levels.Count ? LevelIndex + 1 : -1;
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

    private void UpdateTitle(float deltaTime)
    {
        Session.Idle(deltaTime);
        if (_input.WasKeyPressed(Keys.Escape))
            QuitRequested?.Invoke();
        else if (ConfirmKeys.Any(_input.WasKeyPressed) || Clicked())
        {
            _sounds?.MenuConfirm();
            ShowLevelSelect(LevelIndex);
        }
    }

    private void UpdateLevelSelect(float deltaTime)
    {
        Session.Idle(deltaTime);
        if (BackKeys.Any(_input.WasKeyPressed))
        {
            Show(GameScreen.Title);
            return;
        }

        if (Clicked() && _menu?.LevelAt(_input.MousePositionNdc) is { } clicked)
        {
            _sounds?.MenuConfirm();
            LoadLevel(clicked);
            return;
        }

        if (ConfirmKeys.Any(_input.WasKeyPressed))
        {
            _sounds?.MenuConfirm();
            LoadLevel(LevelIndex);
            return;
        }

        var step =
            PreviousKeys.Any(_input.WasKeyPressed) ? -1
            : NextKeys.Any(_input.WasKeyPressed) ? 1
            : 0;
        if (step != 0)
        {
            _sounds?.MenuMove();
            ShowLevelSelect((LevelIndex + step + _levels.Count) % _levels.Count);
        }
    }

    private void UpdateComplete(float deltaTime)
    {
        Session.Idle(deltaTime);
        if (
            ConfirmKeys.Any(_input.WasKeyPressed)
            || BackKeys.Any(_input.WasKeyPressed)
            || Clicked()
        )
        {
            _sounds?.MenuConfirm();
            ShowLevelSelect(0);
        }
    }

    private bool Clicked() => _input.WasMouseButtonPressed(MouseButton.Left);

    /// <summary>The level select, with <paramref name="index"/> selected and shown behind it.</summary>
    private void ShowLevelSelect(int index)
    {
        if (index != LevelIndex || Screen == GameScreen.Playing)
            Session = CreateSession(index);
        Show(GameScreen.LevelSelect);
    }

    private void Show(GameScreen screen)
    {
        Screen = screen;
        Banner = "";
        _menu?.Show(
            screen switch
            {
                GameScreen.Title => MenuText.Title(),
                GameScreen.LevelSelect => MenuText.LevelSelect(_levels, LevelIndex, BestShifts),
                GameScreen.Complete => MenuText.Complete(_levels, BestShifts),
                _ => [],
            }
        );
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
