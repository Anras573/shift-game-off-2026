using GravityShift;
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
var game = new GravityShiftGame(new NativeInputState(), renderer, window);

game.LevelStarted += level => Console.WriteLine($"Level: {level.Name}");
game.LevelEnded += (level, outcome) => Console.WriteLine($"{level.Name}: {outcome}");

Keyboard.AddKeyDown(Keys.Escape, window.Close);
window.OnUpdate += delta => game.Update((float)delta);
window.OnRender += _ => game.Render();

Console.WriteLine(
    "Gravity Shift: A/D or ←/→ walk · Q/E shift gravity · W/↑/Space flip · R restart"
);
window.Run();
