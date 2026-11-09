"""Synthesizes the game's sound effects and music into src/GravityShift.Web/wwwroot/audio.

    python3 tools/generate_audio.py

Needs numpy, plus ffmpeg (with libvorbis and aac) for the music: native streaming plays Ogg
Vorbis, and Safari can't decode Ogg, so the browser falls back to the .m4a beside it. Everything
is generated from code with a fixed random seed, so it's reproducible and licence-free. The files
are committed, so building the game doesn't need any of this. Swap any file for a recording with
the same name and the game picks it up.
"""
import os
import shutil
import subprocess
import tempfile
import wave

import numpy as np

RATE = 22050
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "src", "GravityShift.Web", "wwwroot", "audio")
rng = np.random.default_rng(1337)


def t(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def env(n, attack, release_curve=4.0):
    """Linear attack, exponential-ish release over n samples."""
    a = max(1, int(attack * RATE))
    e = np.ones(n)
    e[:a] = np.linspace(0, 1, a)
    e[a:] = np.exp(-release_curve * np.linspace(0, 1, n - a))
    return e


def lowpass(x, cutoff):
    """One-pole low-pass; cutoff may be an array (a sweep)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    alpha = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += alpha[i] * (x[i] - acc)
        y[i] = acc
    return y


def sweep(f0, f1, seconds):
    """Phase of a sine sweeping exponentially from f0 to f1 Hz."""
    f = f0 * (f1 / f0) ** (t(seconds) / seconds)
    return 2 * np.pi * np.cumsum(f) / RATE


def normalize(x, peak=0.9):
    return x * (peak / max(1e-9, np.max(np.abs(x))))


def write_wav(name, x):
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print(f"wrote audio/{name} ({len(x) / RATE:.2f}s)")


# ---------------- sound effects ----------------

def shift():
    """Whoosh: noise through a filter that sweeps up and back down."""
    d = 0.32
    n = len(t(d))
    shape = np.sin(np.pi * np.linspace(0, 1, n)) ** 1.5
    x = lowpass(rng.standard_normal(n), 300 + 2600 * shape) * shape
    x += 0.25 * np.sin(sweep(180, 420, d)) * shape  # a little tonal lift
    return normalize(x, 0.7)


def land():
    """Cardboard thud: a dropping low sine plus a short papery click."""
    d = 0.2
    n = len(t(d))
    body = np.sin(sweep(140, 55, d)) * env(n, 0.002, 7)
    click = lowpass(rng.standard_normal(n), 1800) * env(n, 0.001, 40)
    return normalize(body + 0.6 * click, 0.85)


def locked():
    """Denied: two short low square-ish buzzes."""
    out = []
    for _ in range(2):
        tt = t(0.09)
        sq = np.sign(np.sin(2 * np.pi * 110 * tt)) * 0.6 + np.sin(2 * np.pi * 220 * tt) * 0.4
        out += [lowpass(sq, 1500) * env(len(tt), 0.004, 2), np.zeros(int(0.04 * RATE))]
    return normalize(np.concatenate(out), 0.6)


def shred():
    """Crunch: grinding noise, chopped by the blades, over a low motor."""
    d = 0.7
    tt = t(d)
    n = len(tt)
    chop = 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 28 * tt))
    noise = lowpass(rng.standard_normal(n), 2500) * (0.4 + 0.6 * chop)
    motor = np.sign(np.sin(sweep(70, 45, d))) * 0.35
    return normalize((noise + motor) * env(n, 0.005, 3.5), 0.85)


def bell(freq, d):
    tt = t(d)
    partials = [(1, 1.0, 3), (2.0, 0.5, 5), (3.0, 0.25, 7), (4.2, 0.12, 9)]
    return sum(a * np.sin(2 * np.pi * freq * m * tt) * np.exp(-k * tt) for m, a, k in partials)


def deliver():
    """Ding-ding: two bell notes, a fifth apart, going up."""
    first = bell(1318.5, 0.9)  # E6
    second = bell(1975.5, 0.9)  # B6
    gap = int(0.12 * RATE)
    x = np.zeros(len(first) + gap)
    x[: len(first)] += first
    x[gap:] += second
    return normalize(x, 0.6)


def lost():
    """Falling whistle into the void."""
    d = 0.6
    n = len(t(d))
    return normalize(np.sin(sweep(900, 160, d)) * env(n, 0.01, 2.5), 0.5)


# ---------------- music: a slow night-shift loop ----------------

BPM = 84
BEAT = 60 / BPM
BARS = 16

# Am7 - Fmaj7 - Cmaj7 - G6, as MIDI notes (low voicings).
CHORDS = [[57, 60, 64, 67], [53, 57, 60, 64], [48, 55, 59, 64], [43, 52, 55, 59]]
BASS = [45, 41, 36, 43]
# A sparse vibraphone melody (A minor pentatonic), one entry per eighth note; None rests.
MELODY_A = [76, None, 72, None, 74, None, None, None, 72, None, 69, None, None, None, None, None]
MELODY_B = [None, None, 79, None, 76, None, 74, None, 72, None, None, 74, 76, None, None, None]


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def place(track, start_seconds, sound):
    """Adds sound at a time, wrapping its tail to the start so the loop is seamless."""
    i = int(round(start_seconds * RATE)) % len(track)
    end = i + len(sound)
    if end <= len(track):
        track[i:end] += sound
    else:
        k = len(track) - i
        track[i:] += sound[:k]
        track[: end - len(track)] += sound[k:]


def epiano(freq, d):
    tt = t(d)
    tone = np.sin(2 * np.pi * freq * tt) + 0.3 * np.sin(2 * np.pi * 2 * freq * tt) * np.exp(-6 * tt)
    tremolo = 1 + 0.15 * np.sin(2 * np.pi * 4.5 * tt)
    return tone * tremolo * env(len(tt), 0.02, 2.2)


def vibes(freq, d):
    tt = t(d)
    return (np.sin(2 * np.pi * freq * tt) + 0.2 * np.sin(2 * np.pi * 4 * freq * tt) * np.exp(-12 * tt)) * np.exp(-3 * tt)


def bass(freq, d):
    tt = t(d)
    x = np.sin(2 * np.pi * freq * tt) + 0.25 * np.sin(2 * np.pi * 2 * freq * tt)
    return x * env(len(tt), 0.01, 2.5)


def kick():
    return np.sin(sweep(110, 40, 0.25)) * env(len(t(0.25)), 0.002, 6)


def hat():
    n = len(t(0.05))
    x = rng.standard_normal(n)
    return (x - lowpass(x, 6000)) * env(n, 0.001, 8)


def music():
    total = BARS * 4 * BEAT
    track = np.zeros(int(round(total * RATE)))
    for bar in range(BARS):
        start = bar * 4 * BEAT
        chord = CHORDS[bar % 4]
        for note in chord:
            place(track, start, 0.09 * epiano(midi(note), 4 * BEAT))
        place(track, start, 0.35 * bass(midi(BASS[bar % 4]), 1.5 * BEAT))
        place(track, start + 2.5 * BEAT, 0.25 * bass(midi(BASS[bar % 4] + 7), 1.2 * BEAT))
        if bar >= 4:  # drums come in after the intro
            for beat in (0, 2):
                place(track, start + beat * BEAT, 0.4 * kick())
            for eighth in range(8):
                swing = 0.08 * BEAT if eighth % 2 else 0
                place(track, start + eighth * BEAT / 2 + swing, (0.05 if eighth % 2 else 0.08) * hat())
        if bar >= 8:  # melody in the second half; alternates two phrases
            phrase = MELODY_A if (bar // 2) % 2 == 0 else MELODY_B
            half = bar % 2
            for k, note in enumerate(phrase[half * 8 : half * 8 + 8]):
                if note is not None:
                    place(track, start + k * BEAT / 2, 0.12 * vibes(midi(note), 1.5))
    return normalize(track, 0.8)


def encode_music(x):
    ffmpeg = shutil.which("ffmpeg")
    if ffmpeg is None:
        raise SystemExit("ffmpeg is needed to encode the music (.ogg and .m4a)")
    with tempfile.TemporaryDirectory() as tmp:
        src = os.path.join(tmp, "music.wav")
        data = (np.clip(x, -1, 1) * 32767).astype("<i2")
        with wave.open(src, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(data.tobytes())
        for name, codec in (("music.ogg", ["-c:a", "libvorbis", "-q:a", "3"]), ("music.m4a", ["-c:a", "aac", "-b:a", "64k"])):
            subprocess.run(
                [ffmpeg, "-y", "-loglevel", "error", "-i", src, *codec, "-map_metadata", "-1", "-fflags", "+bitexact", os.path.join(OUT, name)],
                check=True,
            )
            print(f"wrote audio/{name} ({len(x) / RATE:.1f}s loop)")


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in {
        "shift.wav": shift,
        "land.wav": land,
        "locked.wav": locked,
        "shred.wav": shred,
        "deliver.wav": deliver,
        "lost.wav": lost,
    }.items():
        write_wav(name, fn())
    encode_music(music())


if __name__ == "__main__":
    main()
