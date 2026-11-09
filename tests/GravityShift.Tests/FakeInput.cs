using System.Numerics;
using Yaeger.Input;
using Yaeger.Platform;

namespace GravityShift.Tests;

/// <summary>Scriptable <see cref="IInputState"/> for headless gameplay tests.</summary>
internal sealed class FakeInput : IInputState
{
    private readonly HashSet<Keys> _held = [];
    private readonly HashSet<Keys> _pressedThisFrame = [];

    public void Hold(Keys key) => _held.Add(key);

    public void Release(Keys key) => _held.Remove(key);

    public void Tap(Keys key) => _pressedThisFrame.Add(key);

    private bool _clicked;

    /// <summary>A left click (or tap) this frame at <paramref name="ndc"/> (y up).</summary>
    public void Click(Vector2 ndc)
    {
        MousePositionNdc = ndc;
        _clicked = true;
    }

    public void EndFrame()
    {
        _pressedThisFrame.Clear();
        _clicked = false;
    }

    public bool IsKeyPressed(Keys key) => _held.Contains(key);

    public bool WasKeyPressed(Keys key) => _pressedThisFrame.Contains(key);

    public bool WasKeyReleased(Keys key) => false;

    public bool IsMouseButtonPressed(MouseButton button) => false;

    public bool WasMouseButtonPressed(MouseButton button) => _clicked && button == MouseButton.Left;

    public bool WasMouseButtonReleased(MouseButton button) => false;

    public Vector2 MousePosition => Vector2.Zero;
    public Vector2 MousePositionNdc { get; private set; }
    public float ScrollDelta => 0f;
}
