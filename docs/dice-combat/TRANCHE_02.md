# Tranche 02: tangible dice, unfolding and responsive feedback

The existing custom-dice trial now presents its actual physical dice as shaded
pixel cubes. Results tumble and land, inspection unfolds one die into a six-face
cross net, and the intermission editor transforms the one confirmed physical
face before refolding. Combat rules and the two-fight build are unchanged.

## Launch and controls

```sh
./roag.sh --dice-trial --trial-seed=202
make dice-trial
```

All Tranche 01 action controls remain: arrows/WASD use Footwork, F/2 selects
Weapon, 1/2/3 selects a role, directions or a target click commit, and an enemy
click/bump uses the rolled Weapon. Cube hit areas stay fixed during motion.

**I** opens/closes the net. While inspecting, **1/2/3** chooses a die and
**left/right** focuses a physical face. The three role tabs and six settled
panels also accept clicks. `f1`–`f6` label physical faces, not action shortcuts;
`*` and an inner outline identify the physical face that produced this result.
Names, descriptions, costs, counts and starting-boon details stay outside the
moving faces. Escape refolds immediately when asked, even during unfolding.

The intermission uses the same net: 1/2/3 chooses a die, left/right chooses its
physical slot, up/down chooses a compatible replacement, Enter confirms, K skips.
The corner tag and “Proposed” line distinguish a candidate from the committed
face. Counts show the proposed distribution. Selection is deferred until the
net is settled when clicking moving panels; keyboard selection remains precise.
Inspection, candidates and cancellation do not write the physical die or roll.

The trial menu adds **M Reduced motion** (also clickable). It is session-local,
including replay, and is never saved into normal preferences. Reduced motion
shows the true result immediately, opens/closes nets immediately, and removes
new tumble, lift, kick and expanding-ring motion. Existing combat presentation
continues to use its established settings. Disabling the existing combat-effects
preference also reduces these new cosmetics.

## Moving evidence

- [Actual running-game playthrough with captured game audio](evidence/tranche02/playthrough.mp4)
- [Short actual-frame rolling GIF](evidence/tranche02/roll.gif)
- [Settled combat UI](evidence/tranche02/opening.png)
- [Unfolded inspection](evidence/tranche02/inspection.png)
- [Unfolded editor and proposed counts](evidence/tranche02/intermission.png)
- [Single-action preview](evidence/tranche02/preview.png)
- [Victory](evidence/tranche02/victory.png)
- [Native-input transcript](evidence/tranche02/native-input.log)

The approximately 36-second MP4 is continuous footage of the real 1024×640
LÖVE renderer, trimmed at the start and encoded with H.264/AAC. It includes action
use, all three dice rotating, repeated results rolling again, inspection of all
three dice, refolding, the physical face replacement, entry into fight two,
Rhythm/Bloom feedback, victory and replay. The GIF is a short excerpt of captured
frames, not a separately animated mockup. The fixed window is nonresizable in
`conf.lua`; no resize support was added or claimed.

## Physical identity and cosmetic ownership

`src/dice_trial.lua` remains the authority for configurations, physical rolled
indices, revisions and face replacement. It was not edited in this tranche.
`src/dice_presentation.lua` owns copied configurations/results and cosmetic clocks.
It imports no gameplay engine or RNG. Each revision starts one tumble, including
identical results; unchanged revisions and menu/inspection changes do not.
Older revisions are ignored. Replay constructs a fresh cosmetic model, so old
clocks and event IDs cannot affect its replacement.

`src/dice_view.lua` projects the six faces of one cube. Physical indices have
fixed normals: 1 front, 2 right, 3 bottom, 4 left, 5 top, 6 back in the unrotated
coordinate convention. Result-dependent orientation brings the authoritative
index to the large visible face. Every side face reads its own configured slot,
including duplicates. Cosmetic orientation does not set an attack direction.
The matching cross net is:

```text
       f5
   f4  f1  f2
       f3
       f6
```

The same slot lookup drives the cube, net, editor, current-result marker and
confirmed-edit animation. The editor keeps the old glyph for the first 120 ms,
then switches that slot to the authoritative replacement, with a short emphasis.
The other five slots retain their identities. Three small cubes show fight
two's real fresh results while this net refolds. Early input can finish that
opening reveal without also using an unseen result.

`src/dice_ui.lua` connects explicit selection, successful commands and revision
changes to this model. Existing stale revisions, playback locking, held-key
release, multiclick protection and callback-batch guards remain. Target
validation and forecasts still use the existing rules. Names and descriptions
always read the authoritative result copied for this revision, never the face
that happens to point forward halfway through tumbling.

`main.lua` adds narrow adapters for dt, the current playback event group, hero
playback position, cosmetics-enabled preference, and dice audio. Only committed,
visibility-filtered events reach the new feedback; forecasts never call these
adapters. Arena dimensions, combat playback durations, damage packets, sprites,
particles, camera handling and input semantics remain owned by the existing code.
No camera shake was added for rolls.

## Timing and response

| Presentation | Timing |
| --- | --- |
| Hover lift | Approximately 60 ms response, at most 3 pixels |
| Selection press/rebound | 80 ms |
| Use acknowledgement | 90 ms; Quick Step 60 ms |
| Hero action icon/name echo | 150 ms; Quick Step 100 ms |
| Three-die roll | 200 ms including settle, with 12 ms stagger per die |
| Net unfold / refold | 180 / 150 ms |
| Confirmed face emphasis | 240 ms, followed by refold |

Normal rolls start when the real result revision changes, concurrently with
combat playback. When playback finishes, any remaining alternate-face animation
ends immediately; there is **no extra roll-only input lock**. Opening/replay and
fight-two entry may use the short opening reveal. A key/click used to skip an
unseen opening reveal is consumed and cannot spend that result set. The editor
emphasis/refold can be interrupted once its real results are readable.

An eight-action sequence was measured before and after in the real GUI, at the
same world-time boundaries 70, 140, 290, 390, 540, 640, 740 and 810. Accepted-input
to playback-ready median was **888.9 ms before / 887.5 ms after**. Individual
values are in [the comparison](evidence/tranche02/readiness-comparison.json).
Additional roll-only blocked time was **0 ms on all eight actions**. A separate
headless test compares the existing playback clocks directly and also finds
zero added blocked time. These are internal readiness measurements, not claims
about physical display latency or human reaction time; most elapsed time is the
existing enemy/combat playback, and the small before/after difference is noise.

## Combat feedback and sound

A successful use compresses its chosen die and echoes that action's icon/name
on the hero at action start. Quick Step's acknowledgement is brisker. Strike,
Sweep, Pierce and Heavy Strike retain existing attack footprints and real hit
feedback; the action echo distinguishes the used face without creating a second
impact or sound. Heavy Strike retains its real stronger damage/impact processing.

Actual guard application/absorption produces protection annotations. Rhythm's
cue distinguishes actual movement priming from a benefiting hit, and positive
secondary damage is labelled with its actual amount. Bloom annotates the actual
credited kill/burst location and positive burst damage. The legitimate seed-202
journey includes a real 2-damage Guard absorption at combat action 11. None of
these annotations invent a hit, effect or damage packet; previews emit none.

`src/dice_audio.lua` synthesizes four short original PCM cues: coordinated
clatter (150 ms), landing (75 ms), fold (85 ms) and replacement (130 ms). Fixed
oscillators/tap arithmetic use no random stream. `src/audio_cues.lua` registers
the four UI IDs, and `src/studio_audio_runtime.lua` lazily installs their transient
sources through the existing mixer and voice limits, at gain 0.35. The default
published/fallback audio pack is unchanged. Existing attack/boon audio remains
its sole owner; no duplicate attack or boon sound is emitted.

The actual GUI checked effects mute, quarter effects volume (a real source
volume of 0.0875), and master mute; headless audio tests cover the same shared
mixer and unavailable-audio fallback. Preference values were changed only through
the disposable observer's existing mixer API for this check, not written into
player settings. Normal game interactions were native keyboard/mouse events.
The retained video has a verified non-silent audio track. The first capture's
track was silent because OpenAL chose native PipeWire and ignored PULSE_SINK;
forcing `ALSOFT_DRIVERS=pulse` corrected the isolated recording route.

## Verification

Relevant initial baseline was established from the current **working tree**, not
only HEAD: dice 21/0, card 36/0, Boggle 31/0. It included the prior tranche's
uncommitted implementation. A full snapshot was preserved under
`/tmp/roag-presentation-baseline`. A user commit appeared during the task; it was
preserved. Comparisons continued against that initial snapshot.

```sh
make dice-test
make dice-presentation-test
make card-test
make boggle-test
python3 -B tools/build_classic_assets.py --check
git diff --check
```

| Check | Final result |
| --- | --- |
| Existing dice suite | 21 passed, 0 failed |
| New presentation suite | 8 passed, 0 failed |
| Relevant existing presentation/audio/persistence regressions | 70 passed, 0 failed; same 70/0 on the preserved baseline |
| Existing card / Boggle suites | 36/0 and 31/0 |
| Asset bindings | 599 checked against 67 unchanged source PNGs |
| Changed Lua/Python/shell parse checks, diff whitespace | Passed |
| Native full two-fight journey | Seed 202, Footwork f1 Step → Quick Step, victory at 2640 with 11 HP |
| Native lifecycle and input | Net cancellation, replay during net motion, held key, multiclick, replay/title passed |
| Native reduced motion | Immediate net, actual Footwork action, immediate results, replay passed |
| Other launch paths | Title, poker, fixed, Boggle and spectator remained live without Lua errors in timeboxed GUI smoke checks |

The eight new tests compare every complete gameplay snapshot at each action
boundary across full/reduced/completed cosmetics, over both fights. They also
cover all six settled cube orientations and net indices, duplicates, one-slot
editing, unchanged RNG under repeated inspection/candidates, repeated results,
stale revisions, stable net hit testing, input readiness, opening-skip semantics,
replay and audio mixing. The unchanged dice suite covers underlying combat,
forecast, input and persistence regressions.

The 70 existing cases are run by `tests/dice_presentation_regressions.lua`:
presentation settings, playback, motion, juice, persistence and audio. The prior
tranche's broad-suite result (817 passes, 14 missing retired-Kenney-asset failures)
is prior evidence in TRANCHE_01, **not a new broad-suite run for this tranche**.
Testing here targets the affected presentation modules; no unrelated art tests
were bypassed or changed.

Native automation uses `tests/dice_presentation_gui.sh` (disposable game/storage
observer) and `tests/dice_presentation_native.py` (xdotool). The observer never
submits a live combat action and compares real resulting snapshots against the
existing deterministic resolver. It also records internal readiness times.
Example with an isolated display:

```sh
Xvfb :98 -screen 0 1024x640x24 -nolisten tcp &
ROAG_DICE_GUI_OUTPUT=/tmp/roag-dice-visual DISPLAY=:98 SDL_VIDEODRIVER=x11 \
  tests/dice_presentation_gui.sh &
ROAG_DICE_GUI_OUTPUT=/tmp/roag-dice-visual DISPLAY=:98 \
  python3 tests/dice_presentation_native.py
```

`LOVE_BIN` and `XDOTOOL` can point at alternate runtime binaries. The native
runner includes a reduced-motion warmup action/replay before its full journey;
the retained main footage and the dedicated reduced-motion check together cover
both. Moving evidence was captured from Xvfb using ffmpeg x11grab at 60 fps. Audio
was routed to a temporary null sink's monitor, without recording other apps.

## Preservation and limits

README.md was never edited. Initial and final SHA-256:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```

The authoritative dice/game/trial/boon/scheduler/playback files and normal
presentation preferences are byte-identical to the initial working snapshot.
No faces, probabilities, costs, combat RNG, AI, boons, encounter positions or
face-edit rules changed. The game retains its Lua/LÖVE renderer and existing
icons; no physics engine or external assets were added.

All four seeded normal-player storage records in the GUI fixture retained their
hashes after the journey, net interactions, local reduced-motion toggle, replay
and title return. The real normal-player-storage fingerprint also matches its
initial value. [Preservation evidence](evidence/tranche02/preservation.json)
records both checks. Temporary capture devices and isolated GUI processes were
removed after verification.

No physical controller, subjective sound preference or human fun assessment was
performed. The game still has a fixed 1024×640 window. The intentionally small
face catalogue shares existing movement/weapon icons; names and physical labels
carry distinctions between related faces. Paid fishing/kiting balance remains
as documented in Tranche 01; this pass changes presentation only.

- Do the dice now feel tangible and satisfying to use?
- Is it easier to understand which result caused the action and its effects?
- Does the added presentation make me want to keep playing while preserving the pace?
