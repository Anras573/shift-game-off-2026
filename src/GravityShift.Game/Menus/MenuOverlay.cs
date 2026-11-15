using System.Numerics;
using Yaeger.ECS;
using Yaeger.Graphics;
using Yaeger.Platform;
using Yaeger.Systems;

namespace GravityShift.Menus;

/// <summary>
/// Draws a menu screen over the (dimmed) level: a column of text lines from the top-left, a
/// highlight bar behind the selected line, and click/tap hit-testing. Like the HUD it has its
/// own world with no camera, so sprites and text are both placed in normalized device
/// coordinates.
/// </summary>
public sealed class MenuOverlay
{
    private const float Margin = 28f;
    private const float LineGap = 8f;

    private static readonly Color DimColor = new(6, 7, 14, 190);
    private static readonly Color HighlightColor = new(255, 214, 92, 45);

    private readonly World _world = new();
    private readonly UnifiedRenderSystem _render;
    private readonly IViewport _viewport;
    private readonly IFontHandle _font;
    private readonly Entity _dim;
    private readonly Entity _highlight;
    private readonly List<Entity> _labels = [];
    private IReadOnlyList<MenuLine> _lines = [];
    private (float Top, float Bottom)[] _rows = [];

    public MenuOverlay(
        IRenderSurface renderSurface,
        ITextRenderSurface textSurface,
        IViewport viewport,
        IFontHandle font
    )
    {
        _viewport = viewport;
        _font = font;
        _render = new UnifiedRenderSystem(renderSurface, textSurface, _world, viewport);

        _dim = _world.CreateEntity();
        _world.AddComponent(_dim, new Transform2D(Vector2.Zero, scale: new Vector2(2f)));
        _world.AddComponent(_dim, new Sprite(IRenderSurface.SolidTexturePath, DimColor));
        _world.AddComponent(_dim, new RenderLayer(0));

        _highlight = _world.CreateEntity();
        _world.AddComponent(_highlight, new Transform2D(Vector2.Zero, scale: Vector2.Zero));
        _world.AddComponent(
            _highlight,
            new Sprite(IRenderSurface.SolidTexturePath, HighlightColor)
        );
        _world.AddComponent(_highlight, new RenderLayer(1));
    }

    public IReadOnlyList<MenuLine> Lines => _lines;

    public void Show(IReadOnlyList<MenuLine> lines)
    {
        _lines = lines;
        while (_labels.Count < lines.Count)
        {
            var label = _world.CreateEntity();
            _world.AddComponent(label, new Text(" ", _font, 16, MenuText.Body));
            _world.AddComponent(label, new Transform2D(Vector2.Zero));
            _labels.Add(label);
        }

        for (var i = 0; i < _labels.Count; i++)
        {
            var line = i < lines.Count ? lines[i] : new MenuLine("", 16, MenuText.Body);
            var text = _world.GetComponent<Text>(_labels[i]);
            // An empty string would skip the label entirely on some surfaces; a space is invisible.
            text.Content = string.IsNullOrEmpty(line.Text) ? " " : line.Text;
            text.Color = line.Color;
            _world.AddComponent(_labels[i], text);
        }
    }

    /// <summary>The level of the clickable line under a point in NDC (y up), if any.</summary>
    public int? LevelAt(Vector2 ndc)
    {
        Layout();
        var height = MathF.Max(_viewport.Size.Y, 1f);
        var y = (1f - ndc.Y) / 2f * height;
        for (var i = 0; i < _lines.Count && i < _rows.Length; i++)
            if (_lines[i].Level is { } level && y >= _rows[i].Top && y < _rows[i].Bottom)
                return level;
        return null;
    }

    public void Render()
    {
        Layout();
        _render.Render(clear: false);
    }

    /// <summary>Rough width of one character as a fraction of the font size (DejaVu Sans Bold).</summary>
    private const float CharWidth = 0.62f;

    /// <summary>Screens shorter than this are landscape phones, with touch buttons along the bottom.</summary>
    private const float ShortScreen = 500f;

    /// <summary>Height kept clear for those buttons.</summary>
    private const float TouchStrip = 110f;

    /// <summary>
    /// Scale for a screen: shrinks until the longest line fits the width and every line fits
    /// the height (minus the touch-button strip on landscape phones).
    /// </summary>
    internal static float Scale(IReadOnlyList<MenuLine> lines, Vector2 size)
    {
        var height = size.Y < ShortScreen ? size.Y - TouchStrip : size.Y;
        var tall = lines.Sum(l => l.Size + LineGap);
        var wide = lines.Max(l => l.Text.Length * l.Size * CharWidth);
        var fitHeight = (height - 2 * Margin) / MathF.Max(tall, 1f);
        var fitWidth = (size.X - 2 * Margin) / MathF.Max(wide, 1f);
        return Math.Clamp(MathF.Min(fitHeight, fitWidth), 0.35f, 1f);
    }

    private void Layout()
    {
        var size = Vector2.Max(_viewport.Size, Vector2.One);
        var pixel = new Vector2(2f / size.X, 2f / size.Y);
        var scale = Scale(_lines, size);

        _rows = new (float, float)[_lines.Count];
        var y = Margin;
        for (var i = 0; i < _lines.Count; i++)
        {
            var fontSize = Math.Max(1, (int)MathF.Round(_lines[i].Size * scale));
            var gap = LineGap * scale;
            _rows[i] = (y - gap / 2f, y + fontSize + gap / 2f);
            y += fontSize;

            var text = _world.GetComponent<Text>(_labels[i]);
            if (text.FontSize != fontSize)
            {
                text.FontSize = fontSize;
                _world.AddComponent(_labels[i], text);
            }
            // Text sits on its position (the HUD places lines the same way).
            _world.AddComponent(
                _labels[i],
                new Transform2D(new Vector2(-1f + Margin * pixel.X, 1f - y * pixel.Y), scale: pixel)
            );
            y += gap;
        }

        var selected = -1;
        for (var i = 0; i < _lines.Count; i++)
            if (_lines[i].Selected)
                selected = i;
        var bar =
            selected < 0
                ? new Transform2D(Vector2.Zero, scale: Vector2.Zero)
                : Bar(_rows[selected], size, pixel);
        _world.AddComponent(_highlight, bar);
    }

    private static Transform2D Bar((float Top, float Bottom) row, Vector2 size, Vector2 pixel)
    {
        var width = MathF.Min(size.X - Margin, 720f);
        var center = new Vector2(
            -1f + (Margin / 2f + width / 2f) * pixel.X,
            1f - (row.Top + row.Bottom) / 2f * pixel.Y
        );
        return new Transform2D(center, scale: new Vector2(width, row.Bottom - row.Top) * pixel);
    }
}
