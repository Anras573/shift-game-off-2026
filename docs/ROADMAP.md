# Jam roadmap

Deadline: **December 1, 2026, 13:37 PST**. Aim to have something submittable a week early.

## Week 1 (Nov 2–8): core loop ✅ / in progress
- [x] Repo, engine submodule, desktop + web hosts, CI
- [x] Gravity shift (Q/E), flip (W), walking, camera rotation
- [x] Goal / hazard / out-of-bounds rules, restart, level progression
- [x] Three tutorial levels, each with a headless play-through test
- [ ] Publish a first web build to itch.io (draft/private page) and check it loads in the iframe
- [ ] Decide: allow shifting mid-air, or only when on the floor?

## Week 2 (Nov 9–15): content and feel
- [ ] 8–12 levels (consider moving to Tiled via `TiledMapLoader`)
- [ ] New pieces: conveyor belts (kinematic `PlatformPath`), crates the parcel can push,
      fragile crates that break on hard landings, "stamp" zones that lock gravity
- [ ] Screen shake on landings (`CameraShake`), squash/stretch tween on the parcel
- [ ] Shift counter / par per level

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
