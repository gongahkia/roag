# P05 — First playable defense loop

Status: COMPLETE, reviewed 2026-11-07. Final P05 source checkpoint:
`60155b42` (clean worktree before this report). The final source hashes and
incremental patch from P04 are in `artifacts/colony-web/P05/final-source.json`
and `P05.patch`. P06 development remains in separate worktrees.

Players can now build walls, gates, towers, traps and guard posts, manufacture
ammunition, physically load magazines, rally or shelter founders, survive an
announced raid, rescue casualties, repair real damage and resume production.
The heart or all actual colonist deaths end the run; downing alone does not.

## Acceptance evidence

Both production mount paths, `/` and `/roag/`, completed a fresh seed-42 colony
journey entirely through visible controls. No stock, finished buildings, health,
raid timing or needs were injected. Each built four towers (three loaded and one
forward empty tower), a trap, post, wall and gate. Both saved an actual attack
windup at tick 12222, reloaded the page and matched uninterrupted continuation.

| Production path | Wave ended | Shots / kills | Heart | Forward tower after battle → repair | Natural care | Final founders |
| --- | --- | --- | --- | --- | --- | --- |
| `/` | 12644 | 12 / 5 | 100 | 50 → 100 | 4 rescues, 6 treatments | Six alive at full health, tick 14385 |
| `/roag/` | 12393 | 13 / 5 | 100 | 50 → 100 | 1 rescue, 4 treatments | Six alive at full health, tick 13551 |

Different UI command timings produce different legitimate battle outcomes.
Each saved continuation is compared against its own command/state history.
Workers consumed real collected wood to repair the forward tower. Restarting ore
production supplied the medicine chain needed for recovery.

The headless ordinary run prepared defenses by tick 8813, won at 12402 with the
heart intact, repaired the forward tower at 12807, reloaded it at 12858, and
restored all six founders to full health at 13055. It recorded one rescue and
three treatments. Its mid-wave and recovered JSON continuations matched for
250 ticks; 319 save checks and per-tick material accounting passed. A separate
ordinary colony with food/rest/care but no defenses lost its heart at tick 12354.
A labeled lifecycle fixture verified all-downed nonterminal state, subsequent
all-dead defeat and frozen terminal reload.

Focused tests cover physical ammunition, enclosure and blueprint breaching,
locked-cell impacts, solid corners and friendly blockers, target/source death,
exactly-once loot, cargo and care ownership during destruction, group orders,
strict malformed imports and retained prior simulation/work/needs behavior.

## Checks and coverage boundary

Using Node 24.21.0 / npm 11.19.0:

- Final local suite: **84 passed**, no failures or skips. Strict typecheck passed.
- Generation/RNG/replay, work and survival scenario suites passed. The separate
  `node scripts/defense-scenarios.mjs` passed the ordinary defense/loss comparison.
- Asset export checks and both production builds/distribution checks passed.
- The frozen full browser run at `e5e54809` passed **25 of 26** cases. The remaining
  movement test exported after its short route had already completed, invalidating
  the test's assumption that the actor was still on an edge. Its failure trace is
  retained; this was not hidden by a retry.
- The test now starts a fresh long route and pauses an observed edge. Independently,
  review found automatic guard-post rally targets copied an entire structure into
  a coordinate field, causing strict save rejection. The runtime now copies only
  `x` and `y`; a captured pre-fix run reproduces the rejection at tick 12001.
- Rebuilt production artifacts at `60155b42` passed **all four targeted cases**:
  corrected movement and automatic guard-post rally save/page-reload at each base.
  The other passing frozen cases are retained evidence. A second full-suite run
  after the coordinate fix is **not claimed**. No retries/skips were used.
- Successful browser journeys recorded no unexpected console/page/network errors.
  Fourteen focused screenshots were opened and inspected, including actual map
  telegraphs, damaged/repaired tower, restored founders and 390px controls.
- Preservation passed all **1,163 originals** before and after integration.

Command receipts, build hashes, browser output, original failure traces, downloads
and visual/source receipts are in `artifacts/colony-web/P05/acceptance-01/`,
`guardpost-browser-01/`, `guardpost-regression/`, `final-local/` and
`defense-validation/`. `browser-acceptance-summary.json` records the coverage
boundary and both fresh-colony outcomes. Earlier exploratory failures are retained.

## Rules and preservation

Warning/arrival are ticks 10800/12000. The first wave contains four ordinary
raiders and one breacher. Breachers pressure nearby towers with stable ties;
blocked raiders can destroy player obstructions, never natural rock or spawn
inside an enclosure. Melee requires actual adjacent bodies, not a reserved future
movement cell. Colony projectiles stop at friendly bodies without damaging them.

Tower/trap magazines hold 12/1 rounds. Physical reload jobs deliver 4/1 rounds;
ammunition is spent at launch even if the source dies before impact. Ordinary
workshop reserve accounting excludes loaded magazines. Shared structure removal
settles supplies, claims, jobs, attacks and navigation atomically. Raider deaths
produce one recoverable wood unit exactly once. Schema 4/content `colony-p05-1`
safely rejects older saves; no historical-save migration is claimed.

The same 27 validated sprite crops ship. Full research catalogs, atlases, archives
and the complete paid kit remain excluded from distribution. Patched `/roag/`
build totals: **1,350,240 raw bytes; 363,933 gzip; 294,436 Brotli**. Original artwork,
legacy sources, preservation manifest/archives and preceding reports/evidence
remain intact. Catalog and .NET suites were not repeated in this phase.

## Limits and next work

P05 has one wave and six founders; it does not claim endless balance, other browser
engines, hardware touch, large-colony performance, remote CI or public deployment.
The current interface still uses scrolling controls. The user-requested dedicated
sprite-pack/bitmap-font game HUD is scheduled as P09-HUD before release.

Run `cd web` and `fnm exec --using=24.21.0 npm run dev`; follow the construction
sequence in `web/README.md`. For evidence, choose a fresh `ROAG_EVIDENCE_DIR` before
running the scenario/browser scripts.

P05 is accepted. Continue internally to P06 under the user's existing orchestration
authorization. No legacy deletion, push or deployment has occurred. Vercel is the
selected release target; account/project details may be supplied later.
