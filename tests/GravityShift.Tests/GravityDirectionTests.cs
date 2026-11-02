using System.Numerics;
using GravityShift.Gravity;

namespace GravityShift.Tests;

public class GravityDirectionTests
{
    [Theory]
    [InlineData(GravityDirection.Down, GravityDirection.Left)]
    [InlineData(GravityDirection.Left, GravityDirection.Up)]
    [InlineData(GravityDirection.Up, GravityDirection.Right)]
    [InlineData(GravityDirection.Right, GravityDirection.Down)]
    public void ShiftToLeftWall_MakesTheOnScreenLeftWallTheFloor(
        GravityDirection from,
        GravityDirection expected
    )
    {
        Assert.Equal(expected, from.ShiftToLeftWall());
        // The new gravity points at what was "screen left" before the shift.
        AssertClose(-from.ScreenRight(), expected.ToVector());
    }

    [Theory]
    [InlineData(GravityDirection.Down)]
    [InlineData(GravityDirection.Left)]
    [InlineData(GravityDirection.Up)]
    [InlineData(GravityDirection.Right)]
    public void ShiftToRightWall_UndoesShiftToLeftWall(GravityDirection direction)
    {
        Assert.Equal(direction, direction.ShiftToLeftWall().ShiftToRightWall());
        AssertClose(direction.ScreenRight(), direction.ShiftToRightWall().ToVector());
    }

    [Theory]
    [InlineData(GravityDirection.Down)]
    [InlineData(GravityDirection.Left)]
    [InlineData(GravityDirection.Up)]
    [InlineData(GravityDirection.Right)]
    public void Flip_ReversesGravity(GravityDirection direction)
    {
        AssertClose(-direction.ToVector(), direction.Flip().ToVector());
        Assert.Equal(direction, direction.Flip().Flip());
    }

    [Theory]
    [InlineData(GravityDirection.Down)]
    [InlineData(GravityDirection.Left)]
    [InlineData(GravityDirection.Up)]
    [InlineData(GravityDirection.Right)]
    public void CameraRotation_MapsGravityToScreenDown(GravityDirection direction)
    {
        // Camera2D's view applies RotationZ(-Rotation).
        var view = Matrix4x4.CreateRotationZ(-direction.CameraRotation());
        AssertClose(-Vector2.UnitY, Vector2.Transform(direction.ToVector(), view));
        AssertClose(Vector2.UnitX, Vector2.Transform(direction.ScreenRight(), view));
    }

    private static void AssertClose(Vector2 expected, Vector2 actual) =>
        Assert.True(
            Vector2.Distance(expected, actual) < 1e-5f,
            $"Expected {expected}, got {actual}"
        );
}
