using GravityShift.Levels;
using GravityShift.Systems;
using Yaeger.Input;
using Yaeger.Platform;

namespace GravityShift;

/// <summary>
/// Top-level game: owns level progression and delegates each frame to the current
/// <see cref="LevelSession"/>. Platform hosts (desktop / browser) construct one of these
/// with their own render surface and input, then call <see cref="Update"/> and
/// <see cref="Render"/> every frame.
/// </summary>
public sealed class GravityShiftGame
{
    private readonly IInputState _input;
    private readonly IRenderSurface? _renderSurface;
    private readonly IViewport? _viewport;
    private readonly IReadOnlyList<LevelData> _levels;

    public GravityShiftGame(
        IInputState input,
        IRenderSurface? renderSurface = null,
        IViewport? viewport = null,
        IReadOnlyList<LevelData>? levels = null
    )
    {
        _input = input;
        _renderSurface = renderSurface;
        _viewport = viewport;
        _levels = levels ?? LevelCatalog.All;
        if (_levels.Count == 0)
            throw new ArgumentException("At least one level is required.", nameof(levels));

        Session = CreateSession(0);
    }

    public int LevelIndex { get; private set; }
    public LevelSession Session { get; private set; }

    /// <summary>Raised when a level starts (including restarts), e.g. to update a title label.</summary>
    public event Action<LevelData>? LevelStarted;

    /// <summary>Raised when a level ends, with how it ended.</summary>
    public event Action<LevelData, LevelOutcome>? LevelEnded;

    public void Update(float deltaTime)
    {
        if (_input.WasKeyPressed(Keys.R))
        {
            LoadLevel(LevelIndex);
            return;
        }

        Session.Update(deltaTime);

        switch (Session.Outcome)
        {
            case LevelOutcome.Delivered:
                LevelEnded?.Invoke(Session.Level, Session.Outcome);
                LoadLevel((LevelIndex + 1) % _levels.Count);
                break;
            case LevelOutcome.Shredded:
            case LevelOutcome.Lost:
                LevelEnded?.Invoke(Session.Level, Session.Outcome);
                LoadLevel(LevelIndex);
                break;
        }
    }

    public void Render() => Session.Render();

    public void LoadLevel(int index)
    {
        Session = CreateSession(index);
        LevelStarted?.Invoke(Session.Level);
    }

    private LevelSession CreateSession(int index)
    {
        LevelIndex = index;
        return new LevelSession(_levels[index], _input, _renderSurface, _viewport);
    }
}
