# Session Handoff

Last updated: 2026-06-05.

## Stop State

- Last work slice: recreated the ignored 25-log Tenhou slice, reran `benchmark-discard`, and
  recorded defense-context benchmark findings.
- Expected tracked worktree after this implementation is committed: clean.
- Do not continue into larger local benchmarks, deeper defense-feature expansion, or external
  baseline work unless the user starts a new work session and asks for it.

## Current State

- Branch: `main`.
- Raw Tenhou data, generated reports, and model artifacts are local-only and ignored by git.
- The core Tenhou path parses XML directories, reconstructs through decoded calls, extracts discard
  and call examples, and records parse failures with `--skip-errors`.
- Discard examples now carry active riichi seats, per-seat river count snapshots, ordered per-seat
  rivers, riichi declaration turns/event indices, and a true seat-relative discard index in
  addition to flattened visible counts.
- `benchmark-discard` trains and scores five baselines on one deterministic split: frequency,
  raw-count linear, shanten-aware linear, risk-context linear, and defense-context linear.
- Benchmark JSON reports include source metadata, split settings, model metadata, shanten summaries,
  ablation lift over raw counts, risk context, and defense context, plus held-out `eval_analysis` by
  shanten impact, tile family, rough round event phase, seat-relative turn phase, active opponent
  riichi, and actual-discard genbutsu status.
- Latest local 25-log benchmark shows `discard-linear-defense-context-v0` helps active-riichi,
  genbutsu, and shanten-worsening buckets but does not yet improve aggregate eval accuracy over
  `discard-linear-risk-context-v0`.

## Working Rules

- Start every session with `git status --short` and inspect current files before relying on this
  document.
- Make small commits for each coherent change. Update `IDEAS.md` with progress, benchmark results,
  and next targets as work lands.
- Do not revert or overwrite user changes. If the worktree is dirty, distinguish current-task files
  from unrelated edits before patching.
- Keep raw Tenhou XML, SQLite databases, generated reports, model artifacts, and bytecode out of
  git. Use ignored paths under `data/raw/`, `runs/`, and `models/`.
- Live ladder automation remains out of scope unless a platform gives explicit permission. The
  project is a replay-analysis and research toolkit.
- Prefer simple, tested, dependency-free code until there is a clear reason to add heavier ML
  dependencies.

## Implicit Assumptions

- The local benchmark metrics below are aggregate-only notes from ignored local Tenhou data. They
  are not reproducibility proof unless the same local sample exists or is regenerated with the
  runbook.
- `benchmark-discard` comparisons are only comparable when the split seed, eval fraction, epochs,
  learning rate, and data slice are held fixed.
- `eval_analysis.by_shanten_delta` is based on the actual supervised discard action, not on the
  model's predicted discard.
- The original rough phase buckets still use `DiscardExample.event_index`. The newer
  `by_seat_turn_phase` buckets use the discarding player's own discard index: 0-5 early, 6-11
  middle, and 12+ late.
- `ReconstructionState.visible_tiles()` currently flattens dora indicators, all visible discards,
  visible meld tiles, and the perspective player's hand into one count vector. Per-seat river count
  snapshots and ordered rivers now exist separately, but they do not preserve separate meld ownership.
- `ReconstructionState.apply_reach()` treats `TenhouReach step=1` as an active riichi declaration.
  `step=2` is treated as payment/score metadata and does not create a second declaration.
- `discard-linear-risk-context-v0` is the first coarse risk model. It uses active-riichi flags,
  candidate counts in active-riichi/opponent/self/all rivers, and an active-riichi unseen-count
  feature.
- `discard-linear-defense-context-v0` adds first explicit defense signals: genbutsu, basic suji,
  basic kabe, one-chance, whether the candidate appears before/after opponent riichi, and active
  riichi elapsed-discard counts. These are deliberately simple deterministic features, not a full
  defense engine.
- `DiscardLinearModel` uses explicit feature profiles. Keep `discard-linear-raw-count-v0`,
  `discard-linear-v1`, `discard-linear-risk-context-v0`, and
  `discard-linear-defense-context-v0` stable; add new model kinds rather than silently changing an
  existing profile.

## Local Artifacts

Expected ignored local paths when local Tenhou data is available:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-smoke
data/raw/tenhou/xml/4p-hanchan-25
runs/discard-benchmark-tenhou-25-report.json
runs/inspect-tenhou-25-report.json
runs/
models/
```

Do not commit those paths. Record only aggregate counts and metrics in docs.

## Verification

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m compileall -q src tests
git diff --check
```

Useful local benchmark command:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-25 \
  --epochs 3 \
  --eval-fraction 0.2 \
  --split-seed tenhou-25-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-25-report.json \
  --source-label tenhou-4p-hanchan-25 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-25 --players 4 --length h --limit 25" \
  --source-date 2026-current-year
```

Latest documented 25-log result with risk-context and defense-context profiles:

- Examples: 11,855 total, 9,484 train, 2,371 eval.
- Parse failures: 0.
- Frequency eval accuracy: 0.3037.
- Raw-count linear eval accuracy: 0.3830.
- Shanten-aware linear eval accuracy: 0.4757.
- Shanten-aware eval lift over raw-count linear: +0.0928.
- Risk-context linear eval accuracy: 0.4829.
- Risk-context eval lift over shanten-aware linear: +0.0072.
- Defense-context linear eval accuracy: 0.4825.
- Defense-context eval lift over risk-context linear: -0.0004.
- Defense-context train lift over risk-context linear: +0.0043.
- Risk-context vs defense-context active-riichi bucket: 0.4734 -> 0.4911 on 395 eval examples.
- Risk-context vs defense-context actual-genbutsu bucket: 0.5030 -> 0.5636 on 165 eval examples.
- Risk-context vs defense-context shanten-worsening bucket: 0.0769 -> 0.1183 on 169 eval
  examples.
- Defense context regressed no-active-riichi examples from 0.4848 to 0.4808 and non-genbutsu
  examples from 0.4814 to 0.4764, which erased the targeted defense gains in aggregate accuracy.

## Next Tasks

1. Add finer held-out defense diagnostics before changing model features: split reports by actual
   discard suji, kabe, one-chance, and whether the actual discard was visible before or after an
   opponent riichi declaration.
2. Rerun the same 25-log benchmark with the same split seed and record only aggregate and bucketed
   metrics in docs; verify which non-riichi or non-genbutsu cases are dragging aggregate accuracy.
3. Refine deterministic defense features behind a new model kind instead of changing
   `discard-linear-defense-context-v0`: improve suji/kabe definitions, add sotogawa-style outside
   tiles, and include live terminal/honor pressure.
4. Add richer table context once fine-grained defense buckets are useful: opponent meld ownership,
   dora pressure, score/placement pressure, ippatsu timing, and tsumogiri after riichi.
5. Scale beyond the 25-log local slice only after the defense-context report remains stable and
   useful on the small slice.
6. Revisit external baselines after the local supervised benchmark is less fragile: build `mortal`
   locally and decide how to compare offline without live ladder automation.
