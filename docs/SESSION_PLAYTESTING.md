# Session recording and agent playtesting

Normal world play records diagnostics automatically. Use `./roag.sh` as usual; the terminal prints the local recording folder. No model starts and nothing is uploaded. Recording does not change save/content versions, randomness, movement, combat or autosave policy.

## Inspect a human session

```sh
./dev sessions list
./dev sessions report SESSION_ID
./dev sessions events SESSION_ID --limit 40
./dev sessions events SESSION_ID --after 120 --kind command
./dev agent context
```

Agent context includes three recent ordinary sessions. `report` returns compact identity, metrics, party/world status, markers, recent events and diagnostic signals; `--full` includes full stored snapshots. `events` returns actual bounded payloads. `--root DIR` selects a relocated recording root. These tools never open the selected player save or its chunk directory.

Use **Menu → Mark this moment** when something is confusing, slow or wrong. It closes the menu and retains a state snapshot and rendered screenshot, including during combat. The notice includes the session ID. Oversized screenshots are skipped with a diagnostic event.

Recordings include actual seed/content/generation/sample identity, selected slot, launch mode, executable/Core hashes and assembly versions; real command results and failures; travel/routes and UI interactions; combat actions/damage/statuses, outcomes and rewards; learned developments, work completions and journal events. Managed exceptions, including incompatible startup, retain bounded crash context. Native crashes, forced kills and power loss may leave an unfinalized recording without a stack.

Five-second performance samples include FPS, recent frame/update p95, worst frame and frames over 50 ms. Percentiles use at most 600 recent samples; interval counts and worst frame cover the whole interval. Performance samples also retain render/terrain wall-time p95, main-thread bytes allocated per update/draw frame and graphics draw submissions. These are CPU-side/driver submission diagnostics, not GPU timings; capture/export work after sampling is excluded. Cache misses record chunk-generation duration, including startup prefetch. Ten-second snapshots copy player-owned party/inventory/work, known discovery counts, battle state and current panel/notice/route/camera without generating terrain or consuming time/randomness.

```sh
./roag.sh --session-root /some/local/diagnostics
./roag.sh --no-recording
```

Default root: `~/.vagabonds/sessions/`. Each recording has four rotating 1 MiB JSONL files, one latest snapshot, eight rotating marker snapshots/screenshots (at most 1 MiB per screenshot) and bounded metadata/summaries. A 512-entry asynchronous queue prevents disk backlog blocking gameplay; dropped events are counted. Fatal context also has a separate bounded file. Launch/clean shutdown prune recognized closed history toward twenty sessions / 256 MiB. Active sessions and foreign directories are protected and excluded from that closed-history budget. Writer failure leaves the game running.

Preview scenes, `--ui-report`, frame-limited captures and headless audits retain their existing save isolation. Only explicit playtest mode opts its previews into recording. Legacy campaigns have launch/error metadata, without world-command instrumentation. This is diagnostic history, not a complete deterministic replay: rotation, queue loss and real elapsed-time choices limit reconstruction. Retain a save and its `.chunks` directory separately for exact state reproduction.

## Drive the rendered game

```sh
./dev playtest start --seed 42
# Keep the returned playtest ID:
./dev playtest observe PLAYTEST_ID
./dev playtest act PLAYTEST_ID --json '{"kind":"control","control":"panel:inventory"}'
./dev playtest act PLAYTEST_ID --json '{"kind":"control","control":"close"}'
./dev playtest wait PLAYTEST_ID --seconds 1
./dev playtest capture PLAYTEST_ID
./dev playtest mark PLAYTEST_ID --label 'This route felt unclear'
./dev playtest feedback PLAYTEST_ID --category navigation --severity warning --message 'Describe the observed issue'
./dev playtest report PLAYTEST_ID
./dev playtest review PLAYTEST_ID
./dev playtest stop PLAYTEST_ID
```

Commands return versioned JSON; command errors exit nonzero. `act` distinguishes **delivered** from gameplay success: inspect the next notice, panel, route, party and battle. `--observation N` rejects stale decisions. Unknown/disabled controls, hidden targets, duplicate IDs and invalid requests do not inject gameplay commands. `--file FILE` accepts action JSON instead of `--json`. Pending requests persist before delivery; after a controller disconnects, `observe` reconciles the acknowledged result exactly once. Unfinished actions block resubmission. Diagnostic capture/marking never advances travel or the simulation clock.

`start` builds the Release Client and opens a private Xvfb display/software GL. `--no-build` uses an existing build; `--visible` uses the current X11/WSLg display. The build lock prevents competing with a managed verifier. Default `world` is an ordinary generated start; named scenes such as `world-battle` and `world-encounters` are explicit fixtures, not evidence of reaching them during an expedition.

Every run has a fresh directory/save under `artifacts/playtests/`. Selected saves outside that directory and symlinked save paths are rejected. Explicit Save/Load/Save and quit affect only that slot. Controllers never edit save JSON, terrain, statistics or RNG. Ordinary launches have no control mailbox.

The game holds simulation/dialogue/combat playback between decisions; **explicit waits** advance the normal frame/tick/animation handlers. This gives slower models time to inspect. `agentHold` reports this difference from continuous human input. The backend generates mouse press/release/drag states through the same Client input pipeline as human mouse input. Native focus/device delivery/timing remain separate `verify-native` coverage. UI reports include monotonic mouse-press receipts, exact targets and released states; native tests acknowledge those actual input edges instead of assuming a fixed-duration click was sampled.

Discrete automated gestures pin exploration camera/sprite interpolation to the held view until the mouse edges are delivered, so a resolved person cannot slide out from under its synthetic click. The held movement timer also stops accumulating between requests. Explicit waits resume interpolation; ordinary human input is unchanged. This additional test-mode hold is part of the reported automation limitations, not a claim of identical physical mouse timing.

| Action | Fields |
| --- | --- |
| `control` | Exact visible enabled `control` ID |
| `tile` | World `x`, `y`; optional `button: 3` for context |
| `actor` | Visible `actor` ID, using interpolated sprite bounds |
| `unit` | Displayed combat `unit` ID during an available party turn |
| `hover` | World `x`, `y`, returning actual inspection |
| `drag` | World `x`, `y`, `endX`, `endY` |
| `point` | Screen-pixel `x`, `y`, optional button |
| `wait` | `seconds`: 0.05–30 |
| `capture`, `mark`, `stop` | Inspect, mark (`label`), end the client |

Observations contain observed local tiles/visible actors, actual controls/reasons, player-owned party/pack/work, dialogue and combat presentation. Unplayed combat feedback/rewards and the next actor's skills are withheld during playback. A compact ASCII map and stable IDs help smaller models; screenshots retain the actual visuals. Unknown cells stay unknown. `observe --full` exposes the complete public UI report. Failed captures never reuse an older screenshot as new evidence.

The version-one local file protocol uses `request.json`, `response.json`, `observation.json`, `frame.png`. Requests supply `schemaVersion: 1`, unique `id`, expected `observation`, `kind` and its fields. Responses acknowledge delivery and identify the resulting observation. There is no HTTP listener or arbitrary-command handler.

Runs allow 1,000 actions and retain the latest 64 action/screenshot/observation bundles. Feedback keeps 64 notes and sixteen rotating evidence snapshots; `evidenceRetained` identifies rotated evidence. Recognized closed playtests are pruned at subsequent starts toward ten runs / 512 MiB. Idle clients exit after ten minutes without requests. Stop requests a clean exit before terminating only its recorded process identity/group if necessary.

## Assign a model

Any coding agent can use observe/act/feedback directly. An optional adapter runs a chosen Codex model:

```sh
./dev playtest start --seed 1337
./dev playtest agent PLAYTEST_ID \
  --model gpt-5.6-luna --reasoning low \
  --objective 'Recruit two companions, explore toward a dungeon, and report confusing interactions.' \
  --max-steps 60 --timeout 900
./dev playtest report PLAYTEST_ID
```

The exact model ID passes through unchanged; unavailable models never trigger substitution. The installed `codex exec` uses existing CLI authentication, ephemeral read-only sessions, screenshots/compact observations, short recent history and a strict JSON decision schema. Only validated mouse proposals reach the game. No game-side API key/SDK is needed. Calls use your account allowance only when this command is explicitly run.

The tester now retains its own bounded `player-memory.json`: player-observed places, readable Journal rows, notices, explored ground, prior attempts and an explicit goal/next step. The compact view preserves clues and marked destinations instead of losing them between decisions. Plan destinations must reference learned places; remembered ground never permits clicking a hidden tile. Public observations omit unplayed dialogue and undiscovered bypass locations. A new controller invocation for the same still-running session reuses this memory rather than forgetting the journey.

`./dev playtest review ID` returns the model's comments and reflection; `playtester-notes.md` presents them with evidence links. Each comment separates a concrete observation from the tester's first-person interpretation, expectation, optional suggestion, sentiment and confidence. Positive, negative and mixed impressions are useful; a repeated status such as “animation busy” is not a usability assessment. The prompt requests first impressions, new places/mechanics, confusion and periodic reflections. Closing decisions reserve an opportunity for a debrief about understanding, the world, friction and what to try next. Missing feedback or a missing debrief stays explicitly unassessed, and model completion never establishes enjoyment or a passed gameplay objective. Screenshots rotate under the existing bounds; `evidenceRetained` identifies retained anchors.

For a focused journey, use:

```sh
./dev playtest start --seed 42
./dev playtest agent PLAYTEST_ID --model gpt-5.6-luna --reasoning medium \
  --journey --max-steps 200 --timeout 1800 \
  --objective 'Recruit two companions, learn and visit a discovery, follow its linked dungeon and enter it. Explain what was clear, confusing or interesting about the world and navigation.'
./dev playtest review PLAYTEST_ID
```

The journey tracks observed recruitment/discovery/underground checkpoints and offers **labelled walking assistance** using only current visible walkable/unoccupied cells and learned destinations. It submits the same ordinary mouse actions as a player. It does not reveal a hidden path, teleport, change generation or grant resources. A map marker identifies a destination; it does not automatically walk there. Route guidance helps test a complete journey, but its success does not prove unaided wayfinding is clear. Ordinary mode leaves navigation choices to the model. Both modes are model proxies for playtesting: simulated impressions help find issues but do not establish a human's enjoyment, aesthetic preference or accessibility experience. Held simulation, screenshot evidence and dummy audio remain important limits.

For a long world journey, add `--keep-open` to each bounded model invocation. After a time/step budget, inspect its review and continue with the same ID and another explicitly bounded `playtest agent` command; memory and prior impressions survive. Stop with `./dev playtest stop ID` once finished. Without `--keep-open`, the default controller deliberately closes its game and cannot continue that live world. Idle clients still exit after ten minutes, and the per-client 1,000-action limit remains in force. The controller does not silently buy additional model calls or restart itself when its budget ends.

An unmet micro-goal cannot end a journey successfully: early finishes receive a reminder of the measured missing checkpoints, then return `objective-incomplete` after repeated refusal to continue. Bounded `model-invocations.json` records earlier controller runs; an earlier/interim debrief cannot masquerade as the current final reflection. The notebook feeds prior impressions back to the model so it can revise its understanding. See [the milestone review](EXPERIENTIAL_PLAYTESTER_REVIEW.md) for dogfooding findings and interpretation guidance.

Once all three journey checkpoints are observed, the next model call is a read-only reflection: no further move or combat action is executed, even if the model proposes one. Invalid model replies consume the model budget but deliver no input; valid accompanying comments are preserved and the next call receives the rejection reason. Three consecutive invalid replies stop the loop. Schema constraints cover learned destination IDs, mouse buttons and wait ranges. Longer qualitative tests benefited from medium reasoning during dogfooding; the general CLI default remains low, and model/reasoning selection is always explicit in the run report.

Defaults: thirty decisions / ten minutes, sixty seconds per decision. The loop closes its client on completion/error; `--keep-open` deliberately leaves it held. `--dry-run` prints the invocation/limits without calling a model. `--codex-executable` chooses a trusted installed CLI or deterministic test stand-in. Bounded prompts/decisions/CLI events, reported token usage, linked feedback and an updated `model-report.json` remain inspectable. Status distinguishes completion, time/step budget, model/CLI error and client exit. A model finishing is not proof that assertions passed or that gameplay is fun. Account access and model quality require testing in your environment.

At the overall time boundary, the loop defers the next action unless its wait duration plus five seconds for input delivery and the resulting receipt fit. The report keeps that unexecuted action as `deferredAction` and returns `time-budget`, leaving no request to replay. Overall budget exhaustion during model thinking also returns `time-budget`; a per-decision timeout before that boundary remains an error. A `done=true` decision executes no action, so completion should follow an observation confirming the final action succeeded. These limits govern the model controller; an interrupted external controller can still leave a pending action that must be reconciled with `observe`.

## Regression suite

```sh
./dev playtest suite
./dev agent verify --profile playtest --detach
./dev agent runs show RUN_ID
```

The suite checks ordinary automatic recording through native OS mouse clicks, opt-out, and incompatible-startup crash capture/save preservation. It then exercises rendered travel/recruitment, separate party tiles, selection, timed building/digging, context menus, markers, save/load and staged combat damage. A deterministic Codex CLI stand-in drives the real game and feedback loop without paid model requests. The incompatible startup file is isolated; playable scenarios do not edit fixture saves/statistics/terrain.

Native/full managed profiles include this suite alongside existing X11 mouse acceptance. `playtest` is the shorter recorded-loop profile. Evidence requires nonempty passing assertions and both world/combat coverage; artifacts join normal source freshness/checksums. Failed native checks retain their isolated save/chunks beside screenshots and UI state. Linux/WSL drivers require Python 3.10+, .NET 10 and Xvfb; ordinary-launch acceptance needs `xdotool`, and full native acceptance also needs `ffmpeg`/SDL/OpenGL. Ordinary recording has no Python/X11/model dependency. Native Windows/macOS and subjective long-run balance are not established by these Linux checks.

## Research and tooling review

[OpenAI non-interactive documentation](https://learn.chatgpt.com/docs/non-interactive-mode) supports CLI automation, JSON events, structured output and existing authentication. Installed CLI help was checked. [GPT-5.6 Luna documentation](https://developers.openai.com/api/docs/models/gpt-5.6-luna) documents image input and low reasoning; account/CLI access is not inferred from the page.

Repeated ad-hoc launch/click/capture/log inspection now has one bounded, discoverable loop. Model judgments retain their own evidence and never satisfy deterministic assertions. Preserve distinctions among human timing, Client mouse-state automation, native OS input and controlled Core scenarios when adding tools or reporting results.
