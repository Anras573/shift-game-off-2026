using System.Numerics;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Hud;

/// <summary>
/// Screen-space HUD: level title, shift counter and a result banner, drawn after the level in
/// an overlay pass (<c>Render(clear: false)</c>) from its own world, so it survives level
/// changes. Yaeger draws <c>Text</c> in screen space, so it never rotates with gravity.
/// </summary>
public sealed class HudOverlay
{
    private const int TitleSize = 26;
    private const int BodySize = 20;
    private const float Margin = 20f;
    private const float LineGap = 10f;

    private static readonly Color TitleColor = new(255, 214, 92);
    private static readonly Color BodyColor = new(230, 230, 240);
    private static readonly Color BannerColor = new(255, 255, 255);

    private readonly World _world = new();
    private readonly UnifiedRenderSystem _render;
    private readonly IViewport _viewport;
    private readonly Entity _title;
    private readonly Entity _counter;
    private readonly Entity _banner;

    public HudOverlay(
        IRenderSurface renderSurface,
        ITextRenderSurface textSurface,
        IViewport viewport,
        IFontHandle font
    )
    {
        _viewport = viewport;
        _render = new UnifiedRenderSystem(renderSurface, textSurface, _world, viewport);
        _title = CreateLabel(font, TitleSize, TitleColor);
        _counter = CreateLabel(font, BodySize, BodyColor);
        _banner = CreateLabel(font, BodySize, BannerColor);
    }

    public string TitleText => _world.GetComponent<Text>(_title).Content;
    public string CounterText => _world.GetComponent<Text>(_counter).Content;
    public string BannerText => _world.GetComponent<Text>(_banner).Content;

    public void Set(string title, string counter, string banner)
    {
        SetContent(_title, title);
        SetContent(_counter, counter);
        SetContent(_banner, banner);
    }

    public void Render()
    {
        Layout();
        _render.Render(clear: false); // on top of the level pass
    }

    // Text is laid out in pixels; scale pixels to NDC and stack lines from the top-left.
    private void Layout()
    {
        var size = Vector2.Max(_viewport.Size, Vector2.One);
        var pixel = new Vector2(2f / size.X, 2f / size.Y);
        var y = Margin + TitleSize;
        Place(_title, y, pixel);
        y += LineGap + BodySize;
        Place(_counter, y, pixel);
        y += LineGap * 2 + BodySize;
        Place(_banner, y, pixel);
    }

    private void Place(Entity entity, float pixelsFromTop, Vector2 pixel) =>
        _world.AddComponent(
            entity,
            new Transform2D(
                new Vector2(-1f + Margin * pixel.X, 1f - pixelsFromTop * pixel.Y),
                scale: pixel
            )
        );

    private Entity CreateLabel(IFontHandle font, int size, Color color)
    {
        var entity = _world.CreateEntity();
        _world.AddComponent(entity, new Text(" ", font, size, color));
        _world.AddComponent(entity, new Transform2D(Vector2.Zero));
        return entity;
    }

    private void SetContent(Entity entity, string content)
    {
        var text = _world.GetComponent<Text>(entity);
        if (text.Content == content)
            return;
        // An empty string would skip the label entirely on some surfaces; a space is invisible.
        text.Content = string.IsNullOrEmpty(content) ? " " : content;
        _world.AddComponent(entity, text);
    }
}
