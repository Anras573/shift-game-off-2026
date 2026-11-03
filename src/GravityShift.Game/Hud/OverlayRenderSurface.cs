using System.Numerics;
using Yaeger.Platform;

namespace GravityShift.Hud;

/// <summary>
/// Lets a second <c>UnifiedRenderSystem</c> draw on top of an already-rendered frame:
/// <see cref="BeginFrame"/> is a no-op (the real one clears the screen), everything else is
/// forwarded. The HUD pass uses this with a camera-less world, so text is screen-space on both
/// the native and browser runtimes.
/// </summary>
public sealed class OverlayRenderSurface(IRenderSurface inner) : IRenderSurface
{
    public void BeginFrame() { }

    public void EndFrame() => inner.EndFrame();

    public void FlushQueuedQuads() => inner.FlushQueuedQuads();

    public void SetCamera(Matrix4x4 viewProjection) => inner.SetCamera(viewProjection);

    public void SubmitQuad(Matrix4x4 transform, string texturePath, Vector4 color) =>
        inner.SubmitQuad(transform, texturePath, color);

    public void SubmitQuad(
        Matrix4x4 transform,
        string texturePath,
        Vector2 uvMin,
        Vector2 uvMax,
        Vector4 color
    ) => inner.SubmitQuad(transform, texturePath, uvMin, uvMax, color);

    public Vector2 GetTextureSize(string path) => inner.GetTextureSize(path);
}
