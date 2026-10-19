# Tranche 15 gameplay acceptance and playtest handoff

**Historical tranche report.** These measurements predate the later expansion,
fixed 1-Bit artwork and Studio extraction. Use [current acceptance](ROAG_IDEA_ACCEPTANCE.md)
for the active controls, test results and playback limits. Studio tooling now
belongs to the separate Paint project.

This tranche changes presentation pacing only. Consecutive committed enemy
windups display their locked cells in one 550 ms beat. Consecutive cues of the
same kind in a boon cascade share a beat capped at 360 ms. A `DamageTaken`
followed immediately by the same target's `Died` at the same simulated time
uses one 300 ms lethal beat. Every ordered event remains available to playback;
attack impacts, movement, deaths without a matching hit, and other tactical
boundaries remain separate. A gameplay key pressed while playback is busy now
shows that it was ignored and should be pressed again when ready. No key is
queued. The existing 80 ms tile slide, simulated time, RNG and save formats
are unchanged.

## Reproduce the measurements

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp luajit tests/tranche15_probe.lua 20
TMPDIR=/var/tmp luajit tests/finale_playthrough.lua 3 /var/tmp/roag-t15-legal.trace
TMPDIR=/var/tmp luajit tests/profile_headless.lua 30
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3 /var/tmp/roag-t15-game-gui
TMPDIR=/var/tmp sh tests/tranche15_gui_smoke.sh
TMPDIR=/var/tmp sh tests/profile_gui.sh
```

`tranche15_probe.lua` uses generated floors for seeds 1–20 in each biome. Its
distances are shortest **walkable tile** distances before combat, doors and
player decisions, so they describe geometry rather than playtime. It reports
first placed enemy, first paid chest, elite, corridor length, and cardinal
ranged lanes from placed Thornspitters and Canal Lancers. A lateral exit is an
immediately walkable neighboring tile along a lane; this geometric fact does
not guarantee the player receives a decision before a locked strike. The probe
also replays the ordinary seed-3 legal-action Victory trace and verifies an
exact authoritative snapshot match.

In the 20-seed corpus, Overgrown Ruins had first enemy/chest/elite p50
distances of 17/37/161 tiles; Fungal Hollows 17/35/163; Sunken Aqueduct
28/32/156; and Ashen Bastion 11/37/157. Corridor p50 length was 21–22 tiles
and p95 29–30, with maxima 33–36. There were 35 Overgrown and 157 Aqueduct
placed-enemy firing lanes reaching at least five tiles; none lacked a lateral
exit somewhere in the measured lane. Some individual lane cells had no
immediate lateral move (6/530 Overgrown steps and 145/1627 Aqueduct steps).
The long-corridor death remains a credible tactical/readability concern,
especially under strict scheduling; the sample does not establish a generator
defect. Enemy range, speed, damage, windup timing and map spacing were not
changed.

The guided seed-3 completion still reaches Victory in 991 ordinary submitted
actions with 5 HP. Of those, 857 are move actions and 561 are quiet moves
without combat, windup, pickup or boon activation. The guide does not use Dash
for travel, so these counts are not a human pacing measurement. Human feedback
is needed before changing corridor spacing or resource balance. The extreme
40-Legendary-stack Sweep profile is a synthetic stress build, not an ordinary
starting build.
On that route, the first recorded damage, credited kill and currency pickup
occurred at actions 10, 19 and 20. Currency first reached the five-coin chest
price at action 106; the paid choice was claimed at action 112. The first
elite was reached at action 298 and the next stage at 333. Stage-two and
stage-three elites fell at actions 654 and 963. These are one guided route's
waypoints, not a target difficulty curve.

On the same seed-3 trace, the longest calculated visual queue per action fell
from 2.46 to 1.95 seconds; p95 remained 1.41 seconds. An action with three
consecutive windups fell from 2.19 to 1.09 seconds. The synthetic high-stack
Sweep produced 114 authoritative events: its presentation fell from 76 items
and 16.94 seconds to 13 items and 3.64 seconds. This is a playback-duration
sum, not observed human waiting time or a graphics-frame benchmark. Ordered
events and the final legal-run trace bytes stayed identical.

The callback-driven E01 Studio smoke once asserted during a scripted stroke
because real WSLg cursor movement could end that stroke between synthetic
callbacks. The fixture now ignores unrelated OS motion only while its scripted
stroke is active; its pixel assertions are unchanged. A separate intermittent
E01 timeout without a callback error and an E03 generated-preview assertion
were also observed. E03 now includes its editor diagnostic in that assertion.
The final three-round Studio suite and ten later marked E01 repetitions passed,
but the earlier unexplained timeout and historical signal-139 reports are not
claimed fixed.

## 10–20 minute physical playtest

1. Start `love .` on the target OS. Record seed, OS, graphics renderer and
   whether it is native or WSLg. Move through the first rooms; try rapid taps,
   held keys, cardinal bump attacks, eight-direction Sword Strike, Sweep and
   full/partial Dash. Check slide/camera alignment and whether an ignored key
   during playback is obvious.
2. Approach a Thornspitter or Canal Lancer along a long corridor. Record where
   its cells first become visible, whether a lateral tile or cover is usable,
   its due-time cue, and whether its hit resolves before another decision.
   A fast locked attack may do so by design. Note any death that felt
   unexplained, with seed and approximate action/world time.
3. Fight enough enemies for currency, open a paid chest, cancel and reopen its
   offers, then buy a Common or Uncommon boon. Check that damage, Burning,
   chained activations, drops and automatic stat gains are understandable and
   that the input gate does not force a long wait after an ordinary hit.
4. Use the map, a door and any suspicious secret clue; note empty travel that
   feels repetitive. If time permits, fight the Warden, inspect its free chest
   and route choices. A full three-stage/Regent run is a separate longer test.
5. Save & Quit, relaunch and Continue. If using Studio art, publish a changed
   visual, press F5 during the resumed run and confirm that only presentation
   changes. Try Studio mouse drawing or Wacom-as-mouse only if the hardware is
   available; pressure/tilt is not supported by this workflow.

Feedback template: `seed / OS / renderer / native or WSLg / approximate action
or world time / biome and location / exact input / expected / observed /
screenshot or short clip / whether repeatable / enjoyment and pacing rating`.
Callback-driven GUI scripts and automated legal actions are engineering tests,
not substitutes for this physical acceptance pass.
