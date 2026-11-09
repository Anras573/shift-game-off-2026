using GravityShift;
using GravityShift.Audio;
using Yaeger.Audio;
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

// Pixel art: nearest-neighbour sampling keeps the 16x16 tiles crisp.
renderer.Textures.DefaultSampling = TextureSampling.Pixelated;
var textRenderer = new TextRenderer(window);
using var fonts = new FontManager();
var hudFont = fonts.Load(
    Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "DejaVuSans-Bold.ttf")
);

// Without an audio device the window falls back to a silent context, so this always works;
// a missing or broken sound file just means playing without sound.
using var audio = new NativeAudioOutput(window.AudioContext);
GameSounds? sounds = null;
try
{
    sounds = await GameSounds.LoadAsync(audio);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Sound disabled: {ex.Message}");
}

var game = new GravityShiftGame(
    new NativeInputState(),
    renderer,
    window,
    textSurface: textRenderer,
    hudFont: hudFont,
    sounds: sounds
);

game.LevelStarted += level => Console.WriteLine($"Level: {level.Name} (par {level.Par})");
game.LevelEnded += (level, outcome, shifts) =>
    Console.WriteLine($"{level.Name}: {outcome} in {shifts} shift(s)");

Keyboard.AddKeyDown(Keys.Escape, window.Close);
window.OnUpdate += delta => game.Update((float)delta);
window.OnRender += _ => game.Render();

Console.WriteLine(
    "Gravity Shift: A/D or ←/→ walk · Q/E shift gravity · W/↑/Space flip · R restart · M mute"
);
window.Run();
