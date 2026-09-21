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

The probe needs the `love` executable. If it reports `LÖVE runtime not found`, it
has created no playtest root and the native/human route is **NOT RUN** until a
LÖVE 11.5 runtime is available; do not substitute a normal game launch.

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

Press **Ctrl+Q** to quit through the normal save-on-quit handler. If that save
cannot be committed, the game stays open.

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
   resulting assignment. H opens workforce controls. Crew, F4, Region,
   expedition and School panels are live: opening them must not pause the colony.
4. New frontiers begin under fog. Black cells are unexplored; dim terrain is only
   last known. The docked shuttle lights the first work area. Build a **Torch**
   through the Delegate panel using one real metal on a floor or an empty block
   directly beside a solid terrain/completed wall face. Verify it reveals more
   terrain without exposing an unvisited area behind solid walls. Shift-dragging
   the camera into darkness must not reveal it.
5. Right-click a currently lit local encounter and use Survey. F4 opens personal field notes;
   record whether unknown descriptions stay neutral and whether another worker
   remains untrained. A hidden/dark ecological change is not personal evidence.
6. Press Shift+F7 or select Region. Choose **Prepare craft** at Home Planet.
   Select passengers, choose cargo targets, Apply changes, wait for physical
   loading, Assemble crew, and Launch only when the displayed manifest is
   ready. The panel must show the bound revision, cargo, and crew.
7. Keep the global clock running while the craft travels. Region should show
   transit, and home work must continue while the moon is viewed. At landing,
   select the owned outpost, inspect people/cargo, and use the expedition panel
   to physically unload cargo.
8. Press F5, use **Ctrl+Q** to save and close the game, then use the exact `--resume` command. Press N,
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
| Save/history/layout | F5/Ctrl+Q/resume does not duplicate state; archive has no future record; Shift-drag pans the camera; left-drag selection batch-delegates only after a right-click; Cozette text, long blocked reasons, Escape, focus, scrolling and global pause remain usable. |
| G01 geometry/fog | A settler is visibly half a build block wide and one full block tall; its top cell selects it, a just-above cell does not. Panels stay live, black cells remain unknown, remembered terrain does not update off-screen, and a torch consumes one metal. |

## G02 safe excavation, ropes, and tools

This is a new-frontier-only route. It uses the same isolated root and does not
upgrade old campaign histories.

1. Right-click a visible block and choose **Delegate tool bench**. Watch four
   stone and two metal arrive before construction completes. Right-click the
   completed bench and choose **Fabricate pickaxe** or **Fabricate rope coil**;
   the status reports real delivered metal and 120/60 work actions.
2. Designate a vertical downward dig that would remove a miner's support. The
   designated block remains pending instead of making the worker fall. With no
   coil, the worker reports **Unsafe descent — rope required**. With a loose coil
   at a reachable anchor, right-click the visible anchor and choose **Unfurl
   rope downward**. Watch a real coil get fetched and a finite climb lane appear.
   From a lower ledge, choose **Unfurl rope upward** to throw the same kind of
   physical line toward an upper climb route. `L` selects downward rope and
   `Shift+L` selects upward rope for drag designation. New current-frontier
   controls no longer offer ladder construction; pre-existing ladders still work.
3. Select the Crew panel while the colony runs. Each G02 settler shows steady or
   panicked stress. Trigger a controlled existing danger and confirm a panicked
   person abandons ordinary work to escape; do not expect panic to make a miner
   continue a deliberate unsafe dig.
4. Compare a pickless mining job with a miner who has autonomously collected a
   loose pickaxe. The pick speeds soft terrain to two work units and rock/ore to
   three, while it remains that person's physical equipment through travel.
5. In Region → Prepare craft, the **TOOLS / ACTUAL CUSTODY** section lists tool
   IDs already aboard. Press **Load** beside a loose local tool; it consumes one
   ordinary cargo slot and is physically hauled. The same ID appears at the
   destination after actual launch/landing/unload flow.
6. Demolition charge remains the existing single explosive. Build it through the
   normal Delegate action, select it, and use **T** only when its worker has an
   escape path. It arms after ordinary field work; it is never a free grenade.

Record the worker, tool and rope IDs, the blocked reason if any, cargo slots, and
the actual effect of a panic or blast. These checks remain **NOT RUN** until a
human completes them in a native isolated LÖVE session.

## Movement responsiveness and short jumps

New campaigns move settlers every simulation tick and refresh an idle route more
quickly. They can jump a one- or two-cell horizontal gap only when the complete
body has clear overhead space and a supported landing at the same height. A
deeper/wider gap, low ceiling, liquid or hazard does not become jumpable: use a
rope or the existing safe route instead. Existing saved campaigns retain their
recorded movement cadence.

## G03 minds, memories, and relationships

Begin a **new** frontier campaign so its people have the G03 Mind state; old
campaigns deliberately keep their recorded behaviour. Press `H`, then press
`Tab` twice to open **Mind and relationships**. The colony should continue while
this page is open.

1. Read a person's current condition, work explanation, personal style, memory,
   personal aim, and relationship tone. These should be ordinary descriptions,
   not opaque score readouts.
2. Give a worker a normal mining or construction order they appear reluctant to
   perform. The order must remain active and safe, while their explanation can
   say they are working slowly or correcting a small mistake. They must not
   refuse the order or mine away their own support.
3. Let two free nearby colonists spend time together. Reopen Mind pages to see
   whether a positive conversation or disagreement affected their remembered
   relationship. Direction matters: one person can trust another more than the
   feeling is returned.
4. Complete a real study, teaching session, useful build, or journey. Confirm a
   personal memory or ambition changes only for the people who did the work.
5. Travel with a selected person, then inspect them at the destination. Their
   memory, relationships, ambition, condition, and work style must follow the
   same person. People left home must not socialize with them remotely.

Write down anything that feels arbitrary, too frequent, too slow, or hard to
understand. These checks remain **NOT RUN** until a person completes them in a
native isolated LÖVE session.

## G04 industry and automation

Begin a **new** frontier campaign. Industry adds no free parts: the first
Machine Components come from a built Tool bench using two metal each.

1. Build a Tool bench, make one **Machine Component**, then inspect it as a
   physical local resource. It should not appear in a global inventory.
2. Build a Small Solar Array, Power Pole, Battery, and Electric Lamp on a
   supported visible shelf. Inspect the pole and lamp while the colony is
   running: the lamp should say whether it is connected and powered; opening
   the inspector must not pause the clock.
3. Cover one solar panel column with terrain, then uncover it. Generation and
   battery charge should change on later simulation ticks. The lamp should
   make nearby terrain visible only while a living settler is present; it does
   not reveal a remote or empty site.
4. Build a Fabricator, set its recipe from its inspector, and watch actual
   metal enter its input buffer. When power and inputs exist, progress advances
   in whole productive ticks. Put a Conveyor facing an Industrial Bin at its
   output and watch one physical item move per eligible belt tick.
5. Place a Mining Rig beside a visible dig designation. It should work only
   that designation, leave ordinary miners subject to rope safety, and stop
   with a plain reason for no power, no reachable designated dig, full output,
   unsupported footing, or maintenance.
6. Let a Fabricator or Rig reach **Maintenance required**. A colonist must
   fetch a real component and work on it; power alone never resets wear.
7. Load a loose manufactured component or tool through Region → Prepare craft.
   It must consume one ordinary cargo slot and appear at the destination only
   after physical unloading.

Record pole/network state, battery charge, buffer contents, blocked reasons,
and any belt route that feels hard to read. These checks remain **NOT RUN**
until a person completes them in a native isolated LÖVE session.

## Evidence labels

`HEADLESS` means real simulation, commands, save, and replay without a window.
`MOCK UI` means the injected LÖVE adapter. `NATIVE RENDER / SCRIPTED WINDOW`
means the actual LÖVE process opened, but no person completed the route.
`HUMAN GAMEPLAY` requires a person to perform and record the above route. Do not
upgrade an automated result to a human result.

## G05 — contact and trade

Build a powered Signal Relay beside a solar/pole network and allow the campaign to advance. A signal appears after 300 powered ticks. Select a local, steady colonist as representative to begin contact; ordinary panels remain live while the 60-action audience progresses. Resolve the three concrete protocol actions and inspect that colonist's familiarity. After formal contact, build a Trade Depot, inspect an offer, load the requested local goods into its buffer, then dispatch with a powered Relay. The off-map courier arrives after 400 ticks, or waits if its original Depot is gone. A hostile result is diplomatic only in G05: combat is not implemented.
