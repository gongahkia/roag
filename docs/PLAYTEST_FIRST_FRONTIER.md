# First frontier playtest

This guide keeps every playtest save, export, screenshot, log, Mesa cache and
generated fixture below one private directory. It does not alter the normal
`deepward_02` save identity or read a normal campaign slot.

## Start safely

From the checkout root, first verify the actual LÖVE save path without opening a
window:

```sh
bash tools/playtest.sh --check
```

The command prints `PLAYTEST_ROOT`, `EFFECTIVE_SAVE_DIRECTORY`, and the probe
report. Keep that root. It is deliberately not removed on exit.

For a new ordinary session:

```sh
bash tools/playtest.sh --new
```

The launcher prints a resume command. A marked root can only be resumed by its
own checkout and user:

```sh
bash tools/playtest.sh --resume /tmp/cosmonauts-v01.XXXXXXXX
```

The game displays `ISOLATED PLAYTEST` and the effective LÖVE save directory along
the top of the window. If that label/path is absent, stop: this is not an
isolated playtest. A malformed, symlinked, foreign-checkout, conflicting, or
already-populated fixture target is rejected rather than followed or overwritten.

## Route A — ordinary new frontier

This route uses no arranged supplies or knowledge. It checks discoverability and
the normal startup flow; it does not promise that every generated map is
survivable.

1. Start `--new`. Confirm the isolated label/path, then close the field manual
   with F1.
2. Press N. Press C until **New frontier campaign** is selected. Generate the
   chosen preview with Enter, then press Enter again to create it. Record the
   displayed seed/options.
3. Select a reachable map block with left click, then right-click it. The
   **DELEGATE** panel should offer context-appropriate ordinary work. Queue a
   dig or build order; Space runs the campaign and the worker/sidebar shows the
   resulting assignment. H opens workforce controls.
4. Right-click a local encounter and use Survey. F4 opens personal field notes;
   record whether unknown descriptions stay neutral and whether another worker
   remains untrained.
5. Press Shift+F7 or select Region. Choose **Prepare craft** at Home Planet.
   Select passengers, choose cargo targets, Apply changes, wait for physical
   loading, Assemble crew, and Launch only when the displayed manifest is
   ready. The panel must show the bound revision, cargo, and crew.
6. Keep the global clock running while the craft travels. Region should show
   transit, and home work must continue while the moon is viewed. At landing,
   select the owned outpost, inspect people/cargo, and use the expedition panel
   to physically unload cargo.
7. Press F5, close the game, then use the exact `--resume` command. Press N,
   select **Continue frontier campaign** with C, and Enter. Confirm the same
   people, craft, cargo, and history are present once only.

Record each checkpoint as PASS, FAIL, or NOT RUN. A failed expedition, death,
or blocked landing is a game outcome; unexplained control, accounting, identity,
or save errors are playtest findings.

## Route B — controlled discovery, school, and travel chain

The existing education scenario is an explicitly arranged developer fixture:
128x80 home terrain, three people, real material piles, a glass-reed specimen,
and normal campaign maps. Facts, records, tuition, school construction and craft
loading are still produced by actual effects, work, and recorded commands. It is
not an ordinary generated landing.

Generate its artifacts only below a fresh marked root:

```sh
bash tools/playtest.sh --fixtures /tmp/cosmonauts-v01.XXXXXXXX
```

This runs one 20,000-tick scenario under a 300-second child-only budget, 50% of
one CPU, a memory maximum of the smaller of 2 GiB and one quarter of currently
available memory, and a three-quarter `MemoryHigh` threshold. It writes these
canonical campaign files and an SHA-256 index under `fixtures/`:

- `initial` — no tested fact, school, record, tuition, or loaded craft.
- `partial-copy` — A is genuinely recording an identified fact.
- `partial-lesson` — B has genuine unfinished attended tuition.
- `transit` — A is aboard the actual outbound flight.
- `partial-record-study` — C is learning from the surviving home record.

Load exactly one into an empty isolated campaign slot, then resume:

```sh
bash tools/playtest.sh --load-fixture /tmp/cosmonauts-v01.XXXXXXXX partial-lesson
bash tools/playtest.sh --resume /tmp/cosmonauts-v01.XXXXXXXX
```

Inside the game, N → C → **Continue frontier campaign** opens the copied
campaign. A milestone is a test aid, not evidence that a human performed the
preceding actions.

For the complete controlled route, begin with `initial`, use the normal right
click Delegate controls to survey/study and build the school, select the school
to Record identity then operation, Teach B, and use Study record for C after A
travels. At each checkpoint inspect these visible/authoritative outcomes:

| Checkpoint | Expected result |
| --- | --- |
| Discovery | A observes, surveys, and studies; B/C do not gain A's fact. |
| School | Four stone and two metal are hauled; the new school is disabled and empty. |
| Record / teach | A recorder or an attended pair performs real work; clicking or pausing adds no progress. |
| Interruption | Disable/reassign a policy or allow a normal need to interrupt; both lesson reservations release and B retains only B's progress. |
| Travel | Preparation, assembly, revision-bound launch, maintenance/cargo, person IDs, and no source ghost worker are visible. |
| Continuity | A's facts/XP persist on Moon I; B remains knowledgeable at home; home records stay there. |
| Record study | C gains no eyewitness observations and learns only after work at the installed record. |
| Save/history/layout | F5/resume does not duplicate state; archive has no future record; Cozette text, long blocked reasons, Escape, focus, scrolling and global pause remain usable. |

## Evidence labels

`HEADLESS` means real simulation, commands, save, and replay without a window.
`MOCK UI` means the injected LÖVE adapter. `NATIVE RENDER / SCRIPTED WINDOW`
means the actual LÖVE process opened, but no person completed the route.
`HUMAN GAMEPLAY` requires a person to perform and record the above route. Do not
upgrade an automated result to a human result.
