# Agent development workflow

`./dev` is the shared entry point for people and coding agents. It uses the real content loader, simulation, test suite and MonoGame client. The workflow requires no LLM service, API credentials or daemon and performs no automatic Git actions. An explicitly invoked optional playtest adapter can use an installed Codex CLI/model. Read [DEVELOPMENT.md](DEVELOPMENT.md), [PROJECT_STATE.md](PROJECT_STATE.md) and the user-approved scope before editing.

## Orient and choose checks

```sh
./dev agent help
./dev agent doctor
./dev agent context
./dev agent impact
./dev agent impact --base HEAD~1
./dev agent impact src/Vagabonds.Client/WorldGame.cs
```

Agent commands return `{schemaVersion, ok, command, result}` JSON; errors contain `error`. Doctor reports the SDK and optional graphical prerequisites without installing anything. Context reports source/Git identity, changed paths, invariants and recent tasks/runs. A missing or broken local `AGENTS.md` link is reported; tracked guidance is `docs/DEVELOPMENT.md`.

Impact uses `devtools/areas.json` to suggest profiles, documents and native scenes. Unknown paths recommend `full`; suggestions require judgment about the actual behavior changed. An empty working tree does not prove a task was verified: use a base revision to inspect committed work.

## Durable verification

```sh
./dev agent verify --profile quick
./dev agent verify --profile full --detach
./dev agent runs list
./dev agent runs show RUN_ID
./dev agent runs tail RUN_ID --step core-tests --lines 40
./dev agent runs cancel RUN_ID
./dev agent runs retry RUN_ID
```

A detached run survives a terminal or agent-session closing. Keep the returned run ID and inspect it instead of relaunching. Runs associated with the same task, profile and scene selection reconnect while live. One verifier runs at a time per checkout; additional runs queue to prevent shared build-output races. Cancellation terminates the verifier's active command process group, including descendants. Cancelling a queued run leaves the active run alone. If a worker dies with a live command, cancel that interrupted run before retrying.

| Profile | Checks |
| --- | --- |
| `quick` | Release build, content validation, five declarative scenarios |
| `tools` | Python context, process lifecycle and task-evidence regressions |
| `core` | Release build, content validation, full .NET tests with TRX evidence |
| `assets` | Catalog freshness and catalog regressions |
| `world` | Release build, scenario suite, headless game, four-seed world/exploration/wildlife/situation audits |
| `native` | Release build, all eleven graphical mouse scenarios under Xvfb, recorded playtest suite |
| `playtest` | Release build and bounded recorded exploration/combat/ordinary-launch suite; no paid model calls |
| `full` | Tools, core, assets, world and native, with the build deduplicated |

`--scenes world world-cooperation` is accepted only with `native` or `full`. It explicitly narrows graphical coverage. A task requiring all native scenes cannot be completed with that subset.

Run artifacts live in ignored `artifacts/agent/runs/RUN_ID/`: `run.json`, per-step logs, test results, JSON/SVG audits and graphical reports/captures. Manifests retain command arguments, timeouts, exit codes, timestamps, process identities and SHA256 artifact checksums. `runs tail` returns at most 200 lines/16,000 characters. Scenario commands with `--output` return a small summary and retain the complete trace in the artifact.

Statuses distinguish `queued`, `running`, `passed`, `failed`, `blocked`, `cancelled`, `timed_out`, `interrupted` and `stale`. Missing native prerequisites are blocked, not passed. Empty TRX/native reports cannot pass. Source changes during execution make the run stale; source changes afterward make `fresh` false. Retry starts the entire selected profile in a new folder; it does not pretend unfinished checks passed or skip earlier steps.

Freshness hashes tracked and nonignored untracked file bytes, deletions, link targets and executable modes. Markdown and `docs/` are excluded so recording results or committing the same source does not invalidate checks. Documentation correctness still needs review. Git commit identity is also retained. Artifacts are checksummed for accidental loss/change, not cryptographically authenticated against someone intentionally rewriting the whole manifest.

Managed workers currently require **Linux/WSL** and Python 3.10+. Native runs also require `xvfb-run`, `xdotool`, `ffmpeg` and a working SDL/OpenGL environment. Existing game, content, scenario and .NET test commands remain available on other supported platforms. No native Windows/macOS process-control claim is made.

Inspection commands return success when inspection succeeds, even when the inspected run failed; read `result.status`, `fresh` and `artifactErrors`. Synchronous `verify` exits nonzero for unsuccessful/stale verification. `task check` and `task complete` exit nonzero while requirements remain unsatisfied. Detached `verify` means launched/reconnected, not finished.

## Explicit requirements and handoff

For human playthroughs, `./dev sessions list/report/events` reads automatic local evidence. For an interactive agent tester, `./dev playtest start/observe/act/feedback/report/stop` drives an isolated rendered client; `playtest agent` accepts an explicit Codex model and bounded budget. Human sessions never start model requests. [SESSION_PLAYTESTING.md](SESSION_PLAYTESTING.md) documents storage bounds, schemas, inputs, screenshots, exact commands and the distinction between deterministic assertions and model opinions.

Create a small JSON specification outside the content directory:

```json
{
  "schemaVersion": 1,
  "id": "camp-feedback",
  "objective": "Make camp worker progress visible and accurate.",
  "requirements": [
    {"id":"rules","description":"Progress and cancellation use real saved work.","profiles":["core"]},
    {"id":"presentation","description":"Inspect the minimum-size camp panel.","profiles":["native"],"scenes":["world-camp"],"manualReview":true}
  ]
}
```

```sh
./dev agent task create --spec devtools/tasks/camp-feedback.json
./dev agent context --task camp-feedback
./dev agent task checkpoint --id camp-feedback --note 'Cancellation refund reproduced.' --next 'Fix the refund and add the scenario.'
./dev agent verify --profile core --task camp-feedback --detach
./dev agent task evidence --id camp-feedback --requirement rules --run RUN_ID --note 'The saved cancellation regression proves the reserved supplies are refunded once.'
./dev agent verify --profile native --scenes world-camp --task camp-feedback --detach
./dev agent task evidence --id camp-feedback --requirement presentation --run NATIVE_RUN_ID --note 'The camp mouse controls and state assertions passed.'
./dev agent task review --id camp-feedback --requirement presentation --run NATIVE_RUN_ID --artifact RELATIVE_CAPTURE_FROM_RUN --note 'Inspected this capture: worker portraits, progress and controls are readable at 1024x720.'
./dev agent task check --id camp-feedback
./dev agent task complete --id camp-feedback
```

These are examples: create a spec for the actual authorized task and use real run IDs/artifact paths. Requirements are not inferred from chat, and creating a task never creates an agent goal or commits/pushes code. Persisted records live in `artifacts/agent/tasks/`; retain that directory with the checkout between sessions. Specs can be tracked under `devtools/tasks/`. Existing specs are not silently replaced; record a scoped follow-up task when requirements change.

Attach evidence only after inspecting what it establishes. Completion checks every explicit requirement for fresh, successful, intact runs with the requested profiles and scenes. `manualReview` additionally requires an explanation tied to a real recorded artifact. A complete task becomes `needs-verification` when its runtime source changes. This does not establish design quality, arbitrary requirement coverage or visual readability automatically; those still require engineering judgment and actual image inspection.

The checked-in `agent-tooling` spec is the dogfooding task for this milestone.

## Tweak, reproduce and debug

```sh
./dev content inspect item.chainmail
./dev content impact ability.longsword
./dev content patch item.chainmail --patch artifacts/armor.patch.json
./dev content patch item.chainmail --patch artifacts/armor.patch.json --apply --expect SHA256_FROM_DRY_RUN
./dev content diff --against artifacts/content-before
./dev scenario run devtools/scenarios/gathering.json --output artifacts/gathering-trace.json
./dev scenario suite devtools/scenarios --output artifacts/scenarios.json
./dev diagnose save /path/to/explicitly-selected-save.json
./dev diagnose diff artifacts/before.json artifacts/after.json
```

[CONTENT_AUTHORING.md](CONTENT_AUTHORING.md) describes merge patches, schemas and scenario operations. Patches dry-run by default, validate the whole candidate catalog and require a source checksum before applying. Save diagnostics never write the selected file or its chunk directory. They explain content incompatibility without bypassing the strict loader. Save diffs compare the supplied JSON files; chunk files are separate and can be compared explicitly. A manifest diff is not a full simulation-history diff.

A useful debugging loop is: capture seed/content identity and the failing behavior; write a small scenario; run it to retain the first failing step; patch the actual handler/content; rerun it and the relevant broader profile; inspect native captures for presentation changes; attach the resulting evidence and checkpoint the next action. Scenarios use ordinary commands and real handlers, with explicitly named diagnostic fixtures where setup is necessary. Save/reload uses an isolated temporary slot. They do not bypass collision, fog, turn order, work costs or compatibility checks.

For the launch message “World content differs from this build,” preserve the older save and select a fresh filename:

```sh
./vagabonds.sh --new --save "$HOME/.vagabonds/expedition.json"
# Subsequent launches of that slot:
./vagabonds.sh --load --save "$HOME/.vagabonds/expedition.json"
```

Choose another filename if that slot already contains progress you want to keep. `--new` authorizes replacing the selected slot on saving/quitting. This tooling milestone does not change the game's content hash or migrate incompatible worlds.

## Research and limits

- [Anthropic: effective harnesses for long-running agents](https://www.anthropic.com/research/effective-harnesses-for-long-running-agents) informed explicit criteria, durable handoffs and executable end-to-end verification.
- [Anthropic: writing effective tools for agents](https://www.anthropic.com/research/writing-tools-for-agents) informed bounded summaries, discoverable commands and evaluation on real workflows.
- [Python subprocess documentation](https://docs.python.org/3/library/subprocess.html) informed shell-free argument lists, retained exit codes, timeouts and process cleanup.
- [Microsoft VSTest CLI documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-vstest) informed retained TRX results.
- [RFC 7396](https://www.rfc-editor.org/info/rfc7396/) defines the JSON merge-patch semantics used for content edits.

The system runs locally and retains evidence; it does not schedule autonomous work, share tasks across machines, replace Git, run arbitrary shell text supplied by an LLM, or score whether gameplay is fun. Expand commands from repeated concrete development needs. At each substantial milestone, check whether discovery, reproduction and verification actually shortened the work; record gaps and improve the common entry point.

Executed acceptance results and platform limits are recorded in [VERIFICATION.md](VERIFICATION.md).


### Closing the combat presentation loop

The combat milestone dogfoods this workflow with `devtools/tasks/combat-presentation.json`. Its native checks now capture semantic UI JSON beside screenshots, synchronize short clicks against actual control response, freeze reported animation phases through focus loss, and compare normal/fast/skipped saved states. Use `combatPresentation.busy` to gate actions even when the live battle has already advanced or ended; `combatIntents` and displayed-unit snapshots explain what the player should see. Stable ability IDs and an independent profile hash make visual tuning traceable without changing save compatibility. See [COMBAT_PRESENTATION.md](COMBAT_PRESENTATION.md) for the field contract and reproducible commands.


## Party and encounter verification

The world profile now also runs `audit-expedition`, `audit-encounters` and `audit-counterplay`, retaining their JSON reports alongside the other world checks. Expedition coverage gaps remain explicit even when invariants pass. Encounter generation audits examine authored contexts and actual placements; counterplay reports compare real resolver outcomes under stated fixture conditions. These tools are deterministic diagnostics, not a substitute for human balance playtesting.

Native acceptance includes `world-progression` and `world-encounters`. Equipment clicks use item IDs from the current runtime UI report, including hidden/unowned signature gear and generated variants, rather than guessing indices from all authored items. Every test load requires an explicit success notice before comparing state; rejected temporary saves and their chunks are retained with diagnostic reports. This closes a false-positive path where load rejection preserved an apparently matching state. The scene registry requires eleven scenes for complete native coverage.

The impact map explicitly associates talent, encounter and reward changes with world/core checks and the progression, encounter, battle and boss native scenes. This prevents a content-only change from silently omitting the UI where its consequences appear.
