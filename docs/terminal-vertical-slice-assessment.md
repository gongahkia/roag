# Terminal vertical slice assessment

## Outcome

The Python terminal pivot is a complete engineering vertical slice. A player
can create a seeded household, prepare and leave Jomon, accept/refuse/alter a
generated material request, traverse Hearthford and its river edge, fight,
negotiate, or evade through terrain, acquire and deliver or lose goods, return
or die, save, reload, and revisit the changed world. Enjoyment remains the
project owner's subjective playtest gate.

The verified pre-pivot commit is `de1c1e86c3ce6e962ddd547f57830c636052b870`.
Local branch `archive/web-v19` and annotated tag `jomon-web-v19-final` both
resolve to it. The requested expected start, `614d6fd`, was not the actual
clean `HEAD`.

## Verification

- `python -m unittest discover -s tests -v`: 22 tests passed on Python 3.14.7.
- `python -m compileall -q jomon tests`: passed on Python 3.14.7.
- `python3.11 -m unittest discover -s tests -v`: 22 tests passed.
- `python3.11 -m compileall -q jomon tests`: passed.
- `git diff --check`: passed before the documentation milestone.
- A running Python 3.11 process had only standard-input/output/error PTY file
  descriptors and no socket descriptors. Source inspection found no network,
  subprocess, telemetry, or external-service imports.

Manual PTY play used `python -m jomon` at 100x32 and 80x24. It covered new
world and readable seed entry; all three preparation stations; the gangplank;
contact accept, refuse, and altered-repair choices; the southern flood-control
interaction and positional evasion; visible pressure reaching critical; the
late-delivery market effect; physical return; explicit save; continue; a
second departure showing the lowered crossing, completed objective, contact
outcome, and changed market; guarded direct combat with readable intent and a
finite field dressing; and permanent death with cargo loss and household
succession. Targeted saved states were used to reach combat and lethal defeat
quickly, while their resolution was driven through the curses controls.

The live PTY was resized from 100x32 to 70x20 and back to 80x24. The minimum
size message rendered without a crash and the full interface resumed after
resize. Confirmed quits restored the terminal alternate screen and cursor.

Not manually verified here: macOS terminals, WSL terminals, every terminal
emulator's colour capabilities, an injected uncaught exception, or a
project-owner-paced 15–30 minute enjoyment assessment. `curses.wrapper()` owns
normal and exceptional restoration, but the exception path was not manually
induced.

## Deliberate limits

The slice has one authored regional topology, one persistent contact, one
active shortage, two compact danger profiles (a human obstruction and a flood
crossing), and one objective after it is completed or failed. A later
expedition exposes prior consequences but does not generate a second request.
Capture, rescue, a wider economy, autonomous populations, generic abilities,
and save migrations remain excluded.

The smallest improvement justified by PTY play is to stop routine `You move.`
messages from displacing meaningful intent, pressure, and consequence messages
in the short event panel. This is a presentation correction, not a new system.
