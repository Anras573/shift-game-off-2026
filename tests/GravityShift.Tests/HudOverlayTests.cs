using System.Numerics;
using GravityShift.Hud;
using Yaeger.Graphics;
using Yaeger.Platform;

namespace GravityShift.Tests;

public class HudOverlayTests
{
    [Fact]
    public void Render_DrawsOnTopWithoutClearing_InScreenSpace()
    {
        var surface = new RecordingSurface();
        var text = new RecordingText();
        var hud = new HudOverlay(surface, text, new FixedViewport(), new FontHandle("test"));

        hud.Set("1/8  First Day", "Shifts 0   Par 1", "");
        hud.Render();

        Assert.Equal([false], surface.BeginFrames); // never clears: the level pass already drew
        Assert.Equal(1, surface.EndFrames);
        Assert.Equal(Matrix4x4.Identity, surface.Camera); // screen-space, never rotated
        Assert.Contains("1/8  First Day", text.Drawn);
        Assert.Contains("Shifts 0   Par 1", text.Drawn);
    }

    [Fact]
    public void Labels_StartAtTheTopLeft_AndScalePixelsToScreen()
    {
        var text = new RecordingText();
        var hud = new HudOverlay(
            new RecordingSurface(),
            text,
            new FixedViewport(),
            new FontHandle("test")
        );

        hud.Set("Title", "Counter", "Banner");
        hud.Render();

        var title = text.Transforms["Title"];
        Assert.Equal(2f / 800f, title.M11, 6); // one layout pixel = 2/width in NDC
        Assert.True(title.M41 < -0.9f && title.M42 > 0.8f, $"Title at ({title.M41}, {title.M42})");
        Assert.True(text.Transforms["Counter"].M42 < title.M42);
        Assert.True(text.Transforms["Banner"].M42 < text.Transforms["Counter"].M42);
    }

    [Theory]
    [InlineData(1280, 1f)]
    [InlineData(720, 1f)]
    [InlineData(432, 0.6f)] // portrait phone
    [InlineData(200, 0.6f)] // never below 60%
    public void TextScale_ShrinksOnNarrowScreens(float width, float expected) =>
        Assert.Equal(expected, HudOverlay.TextScale(width), 3);

    [Fact]
    public void Render_UsesSmallerTextOnAPortraitPhone()
    {
        var text = new RecordingText();
        var hud = new HudOverlay(
            new RecordingSurface(),
            text,
            new FixedViewport(412, 915),
            new FontHandle("test")
        );

        hud.Set("Title", "Counter", "");
        hud.Render();

        Assert.True(text.Sizes["Title"] < 26, $"Title size {text.Sizes["Title"]}");
    }

    private sealed class FixedViewport(float width = 800, float height = 600) : IViewport
    {
        public Vector2 Size => new(width, height);
        public float PixelRatio => 1f;
    }

    private sealed class RecordingSurface : IRenderSurface
    {
        public readonly List<bool> BeginFrames = [];
        public int EndFrames;
        public Matrix4x4? Camera;

        public void BeginFrame() => BeginFrames.Add(true);

        public void BeginFrame(bool clear) => BeginFrames.Add(clear);

        public void EndFrame() => EndFrames++;

        public void FlushQueuedQuads() { }

        public void SetCamera(Matrix4x4 viewProjection) => Camera = viewProjection;

        public void SubmitQuad(Matrix4x4 transform, string texturePath, Vector4 color) { }

        public void SubmitQuad(
            Matrix4x4 transform,
            string texturePath,
            Vector2 uvMin,
            Vector2 uvMax,
            Vector4 color
        ) { }
    }

    private sealed class RecordingText : ITextRenderSurface
    {
        public readonly List<string> Drawn = [];
        public readonly Dictionary<string, Matrix4x4> Transforms = [];
        public readonly Dictionary<string, int> Sizes = [];

        public void DrawText(
            string text,
            Matrix4x4 transform,
            FontHandle font,
            int fontSize,
            Color color
        ) => Record(text, transform, fontSize);

        public void DrawText(
            string text,
            Matrix4x4 transform,
            IFontHandle font,
            int fontSize,
            Color color
        ) => Record(text, transform, fontSize);

        private void Record(string text, Matrix4x4 transform, int fontSize)
        {
            Drawn.Add(text);
            Transforms[text] = transform;
            Sizes[text] = fontSize;
        }
    }
}
