using Yaeger.Audio;
using Yaeger.Platform;

namespace GravityShift.Tests;

/// <summary>Records what the game asks <see cref="IAudioOutput"/> to play.</summary>
internal sealed class FakeAudio : IAudioOutput
{
    private readonly List<string> _paths = [];

    public List<(string Path, float Gain, float Pitch)> Played { get; } = [];
    public FakeStream? Music { get; private set; }
    public IAudioMixer Mixer { get; } = new FakeMixer();

    public Task<SoundHandle> LoadAsync(string path)
    {
        _paths.Add(path);
        return Task.FromResult(new SoundHandle(_paths.Count));
    }

    public void Play(
        SoundHandle sound,
        float gain = 1,
        float pitch = 1,
        AudioGroup group = AudioGroup.Sfx
    ) => Played.Add((_paths[sound.Id - 1], gain, pitch));

    public IMusicStream OpenStream(string path) => Music = new FakeStream(path);

    public void Update(float deltaTime) { }

    public IEnumerable<string> PlayedPaths => Played.Select(p => p.Path);

    internal sealed class FakeStream(string path) : IMusicStream
    {
        public string Path { get; } = path;
        public bool Playing { get; private set; }
        public bool Looping { get; set; }
        public float Gain { get; set; }

        public void Play() => Playing = true;

        public void Pause() => Playing = false;

        public void Stop() => Playing = false;

        public void Dispose() { }
    }

    private sealed class FakeMixer : IAudioMixer
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;
    }
}
