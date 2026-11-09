# Gravity Shift: Dead Letter Office

My submission for [itch.io's Game Off 2026](https://itch.io/jam/game-off-2026) (theme: **SHIFT**),
built with the [Yaeger](https://github.com/Anras573/Yaeger) engine.

> You're a parcel lost in a sorting facility on the night shift. You can't jump. You can't
> climb. But you *can* shift gravity. Find the delivery chute and avoid the shredders.

## How to play

Reach the yellow delivery chute without touching the red shredders. Teal conveyor belts carry
you along them (only when they run along your current floor, so a belt in a wall becomes a
lift once that wall is the floor), and you can't shift while inside a purple gravity lock. Every gravity shift
counts: the HUD shows your shifts against the level's **par** (the fewest possible), and
your best for each level this session. Press R to retry for par.

## Controls

| Input | Action |
|-------|--------|
| A / D or ← / → | Walk along the current floor |
| Q (or Z) | Shift gravity: the **left** wall becomes the floor |
| E (or X) | Shift gravity: the **right** wall becomes the floor |
| W, ↑ or Space | Flip gravity: the **ceiling** becomes the floor |
| R | Restart the level (also skips the result banner) |
| Esc | Quit (desktop) |

Screen shake is off when your system asks for reduced motion, or with `?shake=0` in the URL.

On phones and tablets the browser build shows on-screen buttons instead: ◀ ▶ to walk, the
SHIFT arrows (← / → make that wall the floor, ↑ flips), restart and fullscreen. Several fingers
work at once. Add `?touch=1` to the URL to show them on a desktop too.

## Repository layout

```
engine/Yaeger/             Yaeger engine (git submodule, pinned to a commit)
src/GravityShift.Game/     All gameplay. References Yaeger.Core only, so it runs on both hosts.
  Hud/                     Shift counter / par / result banner, drawn in an overlay pass
  Feel/ + FeelSystem       Squash & stretch, screen shake, lock flash, delivery/shred animations
  Gravity/                 GravityDirection maths + per-level GravityState
  Levels/                  ASCII level format, parser and the level catalog
  Systems/                 Gravity shifting, walking, camera rotation, win/lose rules
  LevelSession.cs          One attempt at one level (its own World + systems)
  GravityShiftGame.cs      Level progression; what the hosts drive each frame
src/GravityShift.Desktop/  Native host (Silk.NET/OpenGL). Use for day-to-day development.
src/GravityShift.Web/      Blazor WebAssembly host (WebGL 2). This is what ships to itch.io.
tests/GravityShift.Tests/  xunit tests, including headless play-throughs of every level.
```

## Getting started

Requires the .NET 10 SDK.

```bash
git clone --recurse-submodules https://github.com/Anras573/shift-game-off-2026.git
cd shift-game-off-2026
```

(Already cloned without submodules? Run `git submodule update --init --recursive`.)

```bash
dotnet run --project src/GravityShift.Desktop      # play natively
dotnet run --project src/GravityShift.Web          # play in the browser (open the printed URL)
dotnet test tests/GravityShift.Tests               # run the tests
dotnet tool restore && dotnet csharpier format .   # format (CI runs `csharpier check`)
```

## Adding levels

Levels live in `src/GravityShift.Game/Levels/LevelCatalog.cs` as 16×16 ASCII grids:

| Char | Meaning |
|------|---------|
| `#` | Wall |
| `.` | Empty |
| `P` | Parcel spawn (exactly one) |
| `G` | Delivery chute / goal (one or more) |
| `X` | Shredder / hazard |
| `>` `<` `^` `v` | Conveyor belt (solid), pushing that way on screen |
| `L` | Gravity lock: no shifting while inside |

See [`docs/LEVEL_DESIGN.md`](docs/LEVEL_DESIGN.md) for the workflow and the design rules:
check the room with `python3 tools/level_solver.py "Name"`, add a play-through to
`LevelSolutionTests.cs` (required for every level), and playtest with `?level=N` in the
browser build.

Gameplay feel (gravity, walk speed, shift cooldown, camera easing) is all in `Tuning.cs`.

## Shipping to itch.io

- **CI** (`.github/workflows/ci.yml`) checks formatting, builds, tests, and uploads a playable
  `gravity-shift-web` artifact on every push and PR.
- **Release** (`.github/workflows/release.yml`) runs on `v*` tags or manually. It builds the web
  version plus self-contained Windows (`.zip`), Linux and macOS (`.tar.gz`, so the executable keeps
  its `+x` bit) desktop builds, packaged by `scripts/package-desktop.sh`. On a tag it also publishes
  a GitHub Release with all five downloads attached. To have it push straight to itch.io with
  butler, add:
  - repository secret `BUTLER_API_KEY` (from https://itch.io/user/settings/api-keys)
  - repository variable `ITCH_GAME`, e.g. `your-name/gravity-shift`

To release, **merge the PR first**, then tag the latest `main`:

```bash
git pull && git tag -a v0.4.0 -m "..." && git push origin v0.4.0
```

The release fails straight away (job `verify-tag`) if the tag isn't on the tip of `main`, or if
a pull request into `main` is still open (a tag pushed before merging sits on the old tip, so the
first check alone can't catch it). The error says how to move the tag. To release on purpose
with a PR open, run the Release workflow manually from the Actions tab.

On itch.io, set the project's kind to **HTML** and tick "This file will be played in the browser"
on the `html5` channel upload. For phones, also tick **Mobile friendly** in the embed options
(orientation: either works; portrait puts the controls under the room, landscape beside it). The web build uses a relative `<base href>`, so it works from
itch's sub-path.

## Updating the engine

```bash
git -C engine/Yaeger fetch origin main
git -C engine/Yaeger checkout origin/main
dotnet test tests/GravityShift.Tests   # make sure everything still works
git add engine/Yaeger && git commit -m "Bump Yaeger"
```

## Art

All textures (16×16 pixel tiles in `src/GravityShift.Web/wwwroot/textures`) are drawn in code
by `tools/generate_art.py` (needs Pillow). Change a colour or shape there and re-run:

```bash
python3 tools/generate_art.py
```

The PNGs are committed, so building the game doesn't need Python. Any PNG can be swapped for
hand-made art of the same size and layout: `walls.png` is a 4×4 sheet indexed by which sides
face the room (up 1, right 2, down 4, left 8), `belts.png` is horizontal then vertical, and
`background.png` holds two alternating tiles. The desktop build copies the same files.

## Third-party assets

- `src/GravityShift.Web/wwwroot/fonts/DejaVuSans-Bold.ttf`: DejaVu fonts, Bitstream Vera
  license (DejaVu changes are public domain). See `LICENSE-DejaVu.txt` next to it.

## Jam rules reminder

- Submissions close **December 1, 2026 at 13:37 PST**.
- Source must be in a public GitHub repo (this one).
- Theme: **SHIFT**: gravity shifts, on the night shift.
