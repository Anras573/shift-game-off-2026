# Level design notes

Levels are 16×16 ASCII grids in `src/GravityShift.Game/Levels/LevelCatalog.cs`
(`#` wall, `.` empty, `P` start, `G` chute, `X` shredder). Keep rooms square so they stay on
screen at every camera rotation.

## Workflow

1. Sketch the room in `LevelCatalog.cs`, with `par:` set to the solver's minimum.
2. Run the solver to get the minimum shift count and spot shortcuts:

   ```bash
   python3 tools/level_solver.py "Level Name"
   ```

   It prints the cheapest solution, and the cheapest one that doesn't use a mid-air shift
   (`None` means the level *requires* shifting mid-air).
3. Add a real-physics play-through to `Solutions` in
   `tests/GravityShift.Tests/LevelSolutionTests.cs`. `EveryLevelHasAWorkingSolution` fails
   until you do, and it must finish within par (proving par is achievable in real physics).
   Prefer `WalkToWall` / `WalkInto` and wall stops over timed waits.
4. Play it in the browser build with `?level=N` (1-based), e.g.
   `http://localhost:5000/?level=6` after `dotnet run --project src/GravityShift.Web`.

The solver is a grid approximation: it treats the parcel as exactly one cell and allows a
mid-air shift after falling 4 cells (the 0.3 s cooldown). The test is the source of truth.

## Rules learned the hard way

These come from the numbers in `Tuning.cs` (tile 0.125, parcel 0.1 = 0.8 tiles, top speed
3.5 units/s = 28 tiles/s, shift cooldown 0.3 s ≈ 4 tiles of travel from rest).

- **Flight corridors entered by walking must be at least 2 tiles high.** Walking stops at
  any position, so the parcel can straddle two rows; in a 1-tile corridor it clips the edge.
  1-tile corridors are fine when a wall lines the parcel up (it's pressed against the ceiling
  or a side wall).
- **Narrow openings should be lined up by walls, not by timing.** Fly into a wall next to the
  opening, then shift into it (This Side Up, Sorting Office).
- **Mid-air drops after a shift need wide windows.** The cooldown means you've already
  covered ~4 tiles and are near top speed (28 tiles/s) before you can shift again, so a
  2-tile gap is a ~40 ms window. Aim for 7+ tiles (~200 ms) (Express Post).
- **Mid-air moments that need precision should start from walking off a ledge.** No cooldown
  and you start slow (Return to Sender).
- **Being next to a chute counts.** Resting against a wall that contains a `G` delivers the
  parcel, so chutes can sit in the outer wall.
- **Shredders are forgiving, chutes are generous.** Their triggers are 0.7 and 1.2 tiles, so
  brushing past a shredder diagonally is safe.

## Current levels

| # | Level | Min shifts | Mid-air required | Idea |
|---|---|---|---|---|
| 1 | First Day on the Night Shift | 1 | no | E makes the right wall the floor |
| 2 | Mind the Shredder | 2 | no | W flips; shifting towards the shredders is fatal |
| 3 | Sorting Office | 2 | no | Chaining shifts round a box |
| 4 | Return to Sender | 1 | yes | Walk off the ledge and shift while falling |
| 5 | This Side Up | 2 | no | A wall lines you up with the box opening |
| 6 | Express Post | 3 | yes | Fly between two shredder shelves and drop into the chute |
| 7 | Shredder Alley | 3 | no | Work round inside a box lined with shredders |
| 8 | Dead Letter Office | 4 | no | Finale maze of ledges and pockets |
