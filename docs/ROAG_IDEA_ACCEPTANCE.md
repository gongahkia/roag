# Current expansion and integration acceptance

## Engineering verdict

The approved enemies/classes/items/rooms/terrains/environment work and **50
additional boons** are implemented and verified. There are 103 boons, including
the four existing achievement unlocks; a normal unearned New Run freezes 99 IDs.
Fifteen classes, seven two-floor biomes and existing finale/progression remain.
No balance, scheduler, base actor or reward-price changes were made in this pass.
Human acceptance and native-hardware performance remain outstanding.

All 15 original asset files remain byte-identical. Kenney 1-Bit and the 13
approved monochrome counterparts remain the sole look; native coverage is
599/599. Studio authoring remains in sibling Paint. Gameplay snapshots, frozen
floor state, save/profile contracts, stable IDs and presentation-only F5 remain.

## Verified gates

| Gate | Result |
| --- | --- |
| Starting headless baseline | 647/647 |
| Fifty-boon complete gate | 832/832; 585.22 s; peak LuaJIT RSS 155376 KiB |
| Final integration gate | **837/837**; 784.45 s; peak RSS 156608 KiB |
| Focused added-boon/playback cases | 190/190 |
| Nine established gameplay GUI harnesses | Three isolated rounds, 27/27 |
| Fourteen additional GUI groups | Three rounds, 42/42 group invocations; class group exercises all fifteen classes |
| Earlier approved mechanics through actual game keys | 36 paired Save & Quit/Continue cases × three rounds = 216 GUI processes; wider-window paired round adds 72 |
| Generated native environment families | Nine families × three rounds = 27; second round uses a 1280×800 window with the existing fixed layout |
| Environment refresh stress | 20 refreshes per centered view, 540 total; world RGBA8 pixels identical; no image/texture accumulation |
| New-boon mechanics | Actual GUI: eight patterns, seven forms, six controls, eight cadence and twelve support boons; native pixels for all fifty icons and eight last-roll cues |
| Persistence | Existing disk groups plus new boon scenarios pass; original pre-addition executable writes legacy save, current executable resumes exact events/state |
| Current-pool ordinary-action strategy corpus | Four Victories / two Defeats; every trace replays exactly |

The longer final headless run overlapped GUI/corpus work. These elapsed times
are execution records, not a matched simulation-speed comparison. Counts above
distinguish group invocations, paired scenarios and individual processes.

The legal Adventurer corpus uses seeds 9, 21 and 50 with the existing guided
offers and a first-offer policy. The latter uses the already generated first
offer without a strength ranking or extra gameplay RNG. Seeds 9/21 win under
both; seed 50 dies at the first-stage elite under both. Seed 9 guided: 1070
actions, 25 HP, world time 106780, three paid purchases/six chests, no deaths.
Seed 9 first-offer: 981 actions, 31 HP, world time 95517, three paid purchases/six
chests, no deaths. No teleport, free boon, invulnerability or weakened enemy is
used in these legal runs. Mechanics, gallery and crowd fixtures are assisted.

## Presentation correction and measurements

A real prepared action produced 764 events and **219 playback beats / 45.19 s**.
Mixed event kinds were splitting the intended cascade. They now share a bounded
beat only within the same causal chain, timestamp and floor: **four beats /
1.44 s**, retaining every displayed event in order. Windups, enemy attack
impacts, movement, other roots/times/floors and separate submitted actions keep
their boundaries. Damage numbers retain per-victim totals and critical flags.
The HUD summarizes dense proc chains, prioritizes player damage, and again
reports multiple visible locked attacks. Effective speed, active boon wards and
separate labeled last-roll history fix previously observed readability gaps.

Quiet headless samples (12 actions; chain five actions):

| Prepared scenario | Action CPU p50 / p95 / max ms | Max events / display backlog |
| --- | --- | --- |
| Natural generated seed 21 | 2.543 / 7.820 / 7.820 | 21 / 0.160 s |
| 30 added actors | 3.785 / 15.548 / 15.548 | 61 / 0.080 s |
| 100 added actors | 10.024 / 19.022 / 19.022 | 185 / 0.080 s |
| New-boon synergy, DOT and delayed Echo | 4.560 / 14.098 / 14.098 | 764 / 1.440 s |

Actual 180-frame LÖVE samples on **Fedora WSL, LÖVE 11.5, OpenGL 4.6 Mesa
26.2.4 llvmpipe / LLVM 22.1.8**:

| Scene | Frame p50 / p95 / p99 ms | Draw CPU p95 ms | Draw calls / texture bytes |
| --- | --- | --- | --- |
| Generated | 12.91 / 47.12 / 60.00 | 28.86 | 294 / 6699800 |
| New-boon chain | 29.47 / 64.87 / 102.24 | 55.21 | 802 / 6699800 |
| 40 added actors | 31.35 / 63.77 / 81.67 | 57.72 | 828 / 6699800 |
| 100 added actors | 32.85 / 67.12 / 92.69 | 55.09 | 949 / 6699800 |

These small software-renderer samples do not establish native 60 FPS or a GPU
speedup. No SpriteBatch/renderer rewrite was introduced. Explicit full-art F5
rebuilds are measurable work (roughly 100–210 ms in the recorded environment
views); after collection they plateau at scene-dependent 18–25 images and
13.4–20.1 MB texture memory. They do not change the active run or its geometry.

## Failures retained and limits

Old class-screen, pre-shader color and first-event-only cue assertions were
updated to the actual current interface without dropping their gameplay gates.
The old boon harness produced two nil-index callback errors; a third run exited
139 with only a shell segmentation-fault message. The logs do **not** prove a
common cause. That harness now hands screenshots to protected update code,
checks the complete displayed group and still asserts exact gated snapshots.
Ten isolated repetitions plus the full three-round matrix pass. Historical
unexplained native crash/hang risk remains open; it is not declared fixed.

No physical keyboard, mouse, Wacom or controller was operated. GUI inputs and
screenshots are callback driven. A larger window uses the existing fixed game
layout; this is not a claim of new responsive UI behavior.

## Reproduce and inspect

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3
sh tests/idea_acceptance_gui_smoke.sh enemy
sh tests/idea_acceptance_gui_smoke.sh class
sh tests/idea_acceptance_gui_smoke.sh duelist
sh tests/idea_acceptance_gui_smoke.sh items
sh tests/idea_acceptance_gui_smoke.sh terrain
sh tests/idea_environment_gui_smoke.sh
sh tests/idea_boon_disk_resume.sh
sh tests/idea_weapon_form_disk_resume.sh
luajit tests/idea_profile.lua
sh tests/profile_gui.sh
make sprite-audit-check
git diff --check
```

Evidence is retained under `/var/tmp/roag-*`: final full log
`roag-final-integration-full-20261019.log`; GUI result tables
`roag-final-protected-game-gui-rounds-20261019/results.tsv` and
`roag-final-current-extra-gui-results.tsv`; earlier-mechanics logs
`roag-idea-acceptance-*-round{1,2,3}-20261019.log`; centered environment log
`roag-final-environment-centered-gui-rounds-20261019.log`; legal traces
`roag-build-corpus-JpDuyy`; native profiles `roag-final-native-profile-20261019`
and `roag-final-crowd100-profile-20261019`. The failed 139 round remains in
`roag-final-playback-game-gui-rounds-20261019/3-boon_gui_smoke.log`.

## Human playtest handoff

Spend 10–20 minutes on controls, readability and one biome; finish a full run
separately if needed. Check:

1. New Run/class choice, eight-direction aiming, bump, partial/full Dash,
   cancel, slides and input gating. Try actual keyboard/mouse/controller.
2. A corridor ranged fight: see locked red cells, choose cover/side steps,
   understand damage/death; fast committed strikes need not grant a reaction.
3. Buy a chest, compare Common/Uncommon choices, inspect boon magnitudes,
   effective speed, wards, DOT, proc chains and last dice history.
4. Use one room service/utility; inspect secrets and optional terrain safely.
5. Save & Quit, restart, Continue the same class/build, then F5 and verify the
   run remains intact. Check Warden/finale and achievements during a longer run.

Feedback: OS/native-or-WSLg; input device; class/seed/action if known; expected
versus observed; screenshot/log; whether effects-off changes perceived stutter;
whether target tiles, proc summaries, chest choices and death reasons are clear.
Physical Paint/tablet checks belong to the separate editor project.

## Production repository cleanup

The follow-up audit removed 76 confirmed obsolete artifacts: retired ROAG Studio
and multi-pack trial guides, old comparison/menu screenshots, disconnected E05/
E06 wrappers, unused mixed-pack GUI scripts, the unconsumed theme renderer and
obsolete HTML embedding builders. Unreachable theme branches were removed from
`main.lua`; the fixed 1-Bit drawing path remains the same. World/audio readers,
validated external pack contracts, current tests and original asset bytes stay.

The surviving review HTML, raw feedback, templates and source inventories are
intentional approval provenance, not unfinished production UI. Their hash
contracts still pass. The live catalogue and pixel gallery remain useful tools.
Older milestone reports are explicitly historical; current audio/Mac instructions
no longer advertise editor/publisher commands absent from ROAG.

Room compilation now uses `tools/room_data.py` / `tools/export_rooms.lua` rather
than extracting JSON from review HTML. A real rebuild is byte-identical for all
105 runtime definitions. `make room-expansion-check` is read-only; an isolated
stale-output fixture fails without rewriting it. The 700 biome/seed pairs pass
with all 105 templates sampled, zero fallback and zero repairs.

Only README's Controls H2 text changed, retaining its Key/Action table. Prefix
and suffix SHA-256 checks prove every byte outside that section is unchanged.
Its four previously missing screenshot paths now contain actual native game
captures (including prepared chest/build scenes, not a claim of legal travel).
All retained Markdown/HTML file targets and static Lua imports resolve.

The post-cleanup full gate passes **837/837**, **637.43 s**, peak RSS
**160832 KiB**, exit 0. Log: `/var/tmp/roag-production-cleanup-headless-20261019.log`.
These are verification records, not a matched performance comparison.

Post-cleanup callback gates pass for menus, fixed-art native pixels, corrupt-art
fallback, Save/Continue and dotted attack previews. **20/20 menu screenshots are
exactly RGBA-identical** before and after theme-branch removal. Full logs and
comparison results are `/var/tmp/roag-production-cleanup-*20261019*`.
No extra visual-juice proposal is implemented by this cleanup; those await the
user's decision in chat. The historical native crash/hang and physical/native
acceptance limits above remain unchanged.
