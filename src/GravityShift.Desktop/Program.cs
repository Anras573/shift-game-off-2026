using GravityShift;
using Yaeger.Font;
using Yaeger.Input;
using Yaeger.Platform;
using Yaeger.Rendering;
using Yaeger.Windowing;

using var window = Window.Create(
    new WindowSettings
    {
        Title = "Gravity Shift: Dead Letter Office",
        Width = 1280,
        Height = 800,
    }
);
var renderer = new Renderer(window);
var textRenderer = new TextRenderer(window);
using var fonts = new FontManager();
var hudFont = fonts.Load(
    Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "DejaVuSans-Bold.ttf")
);

var game = new GravityShiftGame(
    new NativeInputState(),
    renderer,
    window,
    textSurface: textRenderer,
    hudFont: hudFont
);

game.LevelStarted += level => Console.WriteLine($"Level: {level.Name} (par {level.Par})");
game.LevelEnded += (level, outcome, shifts) =>
    Console.WriteLine($"{level.Name}: {outcome} in {shifts} shift(s)");

Keyboard.AddKeyDown(Keys.Escape, window.Close);
window.OnUpdate += delta => game.Update((float)delta);
window.OnRender += _ => game.Render();

Console.WriteLine(
    "Gravity Shift: A/D or ←/→ walk · Q/E shift gravity · W/↑/Space flip · R restart"
);
window.Run();
