using System.Numerics;

namespace GravityShift.Gravity;

/// <summary>
/// The four directions gravity can point. Declared so that each successive value is the
/// player's current left wall (see <see cref="GravityDirectionExtensions.ShiftToLeftWall"/>).
/// </summary>
public enum GravityDirection
{
    Down,
    Left,
    Up,
    Right,
}

public static class GravityDirectionExtensions
{
    /// <summary>Unit vector pointing the way things fall.</summary>
    public static Vector2 ToVector(this GravityDirection direction) =>
        direction switch
        {
            GravityDirection.Down => -Vector2.UnitY,
            GravityDirection.Left => -Vector2.UnitX,
            GravityDirection.Up => Vector2.UnitY,
            GravityDirection.Right => Vector2.UnitX,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
        };

    /// <summary>
    /// Shifts gravity a quarter turn so the wall on the player's (on-screen) left becomes the
    /// new floor. On screen, the room appears to rotate counter-clockwise.
    /// </summary>
    public static GravityDirection ShiftToLeftWall(this GravityDirection direction) =>
        (GravityDirection)(((int)direction + 1) % 4);

    /// <summary>
    /// Shifts gravity a quarter turn so the wall on the player's (on-screen) right becomes the
    /// new floor. On screen, the room appears to rotate clockwise.
    /// </summary>
    public static GravityDirection ShiftToRightWall(this GravityDirection direction) =>
        (GravityDirection)(((int)direction + 3) % 4);

    /// <summary>Flips gravity 180° so the ceiling becomes the new floor.</summary>
    public static GravityDirection Flip(this GravityDirection direction) =>
        (GravityDirection)(((int)direction + 2) % 4);

    /// <summary>
    /// World-space direction that appears as "right" on screen once the camera has rotated so
    /// that gravity points straight down. Walking input is applied along this axis.
    /// </summary>
    public static Vector2 ScreenRight(this GravityDirection direction)
    {
        var g = direction.ToVector();
        return new Vector2(-g.Y, g.X);
    }

    /// <summary>
    /// <see cref="Yaeger.Graphics.Camera2D.Rotation"/> (radians) that makes this gravity
    /// direction point straight down on screen.
    /// </summary>
    public static float CameraRotation(this GravityDirection direction)
    {
        var g = direction.ToVector();
        return MathF.Atan2(g.X, -g.Y);
    }
}
