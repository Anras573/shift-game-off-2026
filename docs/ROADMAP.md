# Jam roadmap

Deadline: **December 1, 2026, 13:37 PST**. Aim to have something submittable a week early.

## Week 1 (Nov 2–8): core loop ✅ / in progress
- [x] Repo, engine submodule, desktop + web hosts, CI
- [x] Gravity shift (Q/E), flip (W), walking, camera rotation
- [x] Goal / hazard / out-of-bounds rules, restart, level progression
- [x] Three tutorial levels, each with a headless play-through test
- [x] Publish a first web build to itch.io (draft/private page) and check it loads in the iframe
- [x] Decided: shifting is allowed mid-air (only the cooldown limits it), for chained-shift levels

## Week 2 (Nov 9–15): content and feel
- [x] 8 levels, each with a real-physics play-through test and a solver check (`docs/LEVEL_DESIGN.md`)
- [ ] More levels using the new pieces below (Tiled via `TiledMapLoader` if the ASCII format gets limiting)
- [x] New pieces: conveyor belts and gravity locks, with 3 levels using them (11 total)
- [ ] More pieces: crates the parcel can push, fragile crates that break on hard landings
- [x] Game feel: squash & stretch, landing shake (off for reduced motion), lock flash,
      delivery/shred animations
- [x] Shift counter with par per level, best per session, result banner (HUD)

- [x] Mobile: on-screen touch controls (browser build), room fits portrait screens

## Week 3 (Nov 16–22): presentation
- [ ] Art: parcel sprite with a face that looks "down", tile set for walls/chutes/shredders
- [ ] Audio: shift whoosh, landing thud, shredder crunch, delivery ding, night-shift music
      (`IAudioOutput` works on both hosts)
- [ ] Title screen, level select, end screen (text via `Text` + `UnifiedRenderSystem`)
- [ ] Parallax background of the sorting facility

## Week 4 (Nov 23–Dec 1): polish and ship
- [ ] Playtesting, difficulty curve, bug fixes
- [ ] itch.io page: GIF, screenshots, controls, credits
- [ ] Tag `v1.0.0` → Release workflow → submit to the jam well before the deadline
