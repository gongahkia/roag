# Session Handoff

Last updated: 2026-06-05.

## Stop State

- Last work slice: implemented finer defense diagnostics, added `discard-linear-defense-context-v1`,
  enriched discard examples with table context, reran 25-log and 100-log Tenhou benchmarks, and
  completed local Mortal reconnaissance.
- Expected tracked worktree after this implementation is committed and pushed: clean.
- Do not promote `discard-linear-defense-context-v1` as the default path yet. It improves the
  25-log slice but regresses on the 100-log slice.

## Current State

- Branch: `main`.
- Raw Tenhou data, generated reports, model artifacts, external checkouts, and Mortal build outputs
  are local-only and ignored by git.
- The core Tenhou path parses XML directories, reconstructs through decoded calls, extracts discard
  and call examples, and records parse failures with `--skip-errors`.
- Discard examples now carry active riichi seats, per-seat river counts, ordered rivers, riichi
  declaration turns/event indices, seat-relative discard index, meld ownership snapshots, dora
  indicators, last-discard tsumogiri flags, and ippatsu-active flags.
- `benchmark-discard` trains and scores six baselines on one deterministic split: frequency,
  raw-count linear, shanten-aware linear, risk-context linear, defense-context linear, and
  defense-context-v1 linear.
- Benchmark JSON reports include source metadata, split settings, model metadata, shanten summaries,
  ablation lifts, and held-out `eval_analysis` by shanten impact, tile family, rough event phase,
  seat-relative turn phase, active opponent riichi, actual-discard genbutsu/suji/kabe/one-chance,
  and actual-discard visibility before/after opponent riichi.
- Local Mortal checkout/build reconnaissance is recorded in `docs/external-baselines.md`.

## Working Rules

- Start every session with `git status --short` and inspect current files before relying on this
  document.
- Make small commits for each coherent change. Update `IDEAS.md` with progress, benchmark results,
  and next targets as work lands.
- Do not revert or overwrite user changes. If the worktree is dirty, distinguish current-task files
  from unrelated edits before patching.
- Keep raw Tenhou XML, SQLite databases, generated reports, model artifacts, external checkouts,
  build outputs, and bytecode out of git. Use ignored paths under `data/raw/`, `runs/`, and
  `models/`.
- Live ladder automation remains out of scope unless a platform gives explicit permission. The
  project is a replay-analysis and research toolkit.
- Prefer simple, tested, dependency-free code until there is a clear reason to add heavier ML
  dependencies.

## Implicit Assumptions

- Local benchmark metrics are aggregate-only notes from ignored local Tenhou data. They are not
  reproducibility proof unless the same local sample exists or is regenerated with the runbook.
- `benchmark-discard` comparisons are only comparable when the split seed, eval fraction, epochs,
  learning rate, and data slice are held fixed.
- `eval_analysis.by_shanten_delta` is based on the actual supervised discard action, not on the
  model's predicted discard.
- `by_round_event_phase` uses `DiscardExample.event_index`. `by_seat_turn_phase` uses the
  discarding player's own discard index: 0-5 early, 6-11 middle, and 12+ late.
- `discard-linear-risk-context-v0` is the strongest current aggregate model on the 100-log slice.
  It uses active-riichi flags, candidate counts in active-riichi/opponent/self/all rivers, and an
  active-riichi unseen-count feature.
- `discard-linear-defense-context-v0` adds deterministic defense signals: genbutsu, basic suji,
  basic kabe, one-chance, whether the candidate appears before/after opponent riichi, and active
  riichi elapsed-discard counts.
- `discard-linear-defense-context-v1` is exploratory. It adds finer active-opponent fractions,
  sotogawa-style outside tiles, terminal/honor live pressure, dora/indicator flags, ippatsu timing,
  last-tsumogiri-after-riichi context, and opponent meld-tile pressure.
- `DiscardLinearModel` uses explicit feature profiles. Keep `discard-linear-raw-count-v0`,
  `discard-linear-v1`, `discard-linear-risk-context-v0`, `discard-linear-defense-context-v0`, and
  `discard-linear-defense-context-v1` stable; add new model kinds rather than silently changing an
  existing profile.
- Mortal is AGPL-3.0-or-later. Keep any future comparison behind a neutral data/subprocess boundary
  unless the project intentionally accepts that license boundary.

## Local Artifacts

Expected ignored local paths when local Tenhou data and external baselines are available:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-25
data/raw/tenhou/xml/4p-hanchan-100
data/raw/external/mortal
runs/discard-benchmark-tenhou-25-report-v1.json
runs/discard-benchmark-tenhou-100-report.json
runs/inspect-tenhou-25-report.json
runs/inspect-tenhou-100-report.json
runs/
models/
```

Do not commit those paths. Record only aggregate counts and metrics in docs.

## Verification

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m compileall -q src tests
PYTHONPATH=src python3 -m kenjaku benchmark-discard data/fixtures/tenhou \
  --epochs 1 --eval-fraction 0.25 --split-seed fixed --skip-errors
git diff --check
```

Useful local benchmark commands:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-25 \
  --epochs 3 \
  --eval-fraction 0.2 \
  --split-seed tenhou-25-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-25-report-v1.json \
  --source-label tenhou-4p-hanchan-25 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-25 --players 4 --length h --limit 25" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-100-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year
```

## Latest Benchmarks

25-log local result, split `tenhou-25-v0`:

- Dataset: 25 XML files, 255 rounds, 11,855 discard examples, 3,185 call examples, zero parse
  failures; 9,484 train / 2,371 eval.
- Eval accuracy: frequency 0.3037, raw-count linear 0.3830, shanten-aware linear 0.4757,
  risk-context linear 0.4829, defense-context linear 0.4825, defense-context-v1 linear 0.4884.
- Defense-context-v1 lift: +0.0059 over defense-context and about +0.0055 over risk-context.
- Risk -> defense -> v1 selected buckets: active-riichi 0.4734 -> 0.4911 -> 0.5038 on 395
  examples; actual-genbutsu 0.5030 -> 0.5636 -> 0.5818 on 165; actual-suji 0.2840 -> 0.4074 ->
  0.4074 on 81; one-chance 0.4475 -> 0.4591 -> 0.4864 on 257; post-riichi-visible 0.4308 ->
  0.4769 -> 0.5231 on 65; shanten-worsening 0.0769 -> 0.1183 -> 0.1302 on 169.

100-log local result, split `tenhou-100-v0`:

- Dataset: 100 XML files, 1,039 rounds, 50,276 discard examples, 13,435 call examples, zero parse
  failures; 40,221 train / 10,055 eval.
- Eval accuracy: frequency 0.2985, raw-count linear 0.4135, shanten-aware linear 0.4867,
  risk-context linear 0.4919, defense-context linear 0.4881, defense-context-v1 linear 0.4840.
- Risk-context remains the best aggregate model on this larger slice. Defense-context is -0.0038
  vs risk-context, and v1 is another -0.0041 vs defense-context.
- Risk -> defense -> v1 selected buckets: active-riichi 0.5056 -> 0.4979 -> 0.4920 on 1,687
  examples; actual-genbutsu 0.5952 -> 0.5851 -> 0.5927 on 793; actual-suji 0.3531 -> 0.3995 ->
  0.3892 on 388; one-chance 0.4455 -> 0.4592 -> 0.4555 on 1,091; post-riichi-visible 0.5460 ->
  0.5638 -> 0.5579 on 337; shanten-worsening 0.1028 -> 0.1285 -> 0.1221 on 778.

Mortal local baseline reconnaissance:

- Ignored checkout: `data/raw/external/mortal`.
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`.
- `cargo build -p libriichi --lib --release` passed locally in 37.70s.
- `cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture` passed:
  28 Rust unit tests plus doctests reported OK.
- `cp target/release/libriichi.dylib mortal/libriichi.so` followed by
  `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded.

## Next Tasks

1. Stabilize defense modeling on the 100-log slice before scaling: inspect learned weights,
   per-feature activation rates, and confusion examples where v0/v1 lose against risk-context.
2. Expose and benchmark safer training controls for high-dimensional linear profiles: wire the
   existing `l2` fit parameter through the CLI/report path, then try feature normalization,
   learning-rate sweeps, and epoch sweeps while preserving existing model kind semantics.
3. Improve defense features only behind a new profile after the diagnostic pass: better suji/kabe
   definitions, per-opponent danger gradients, dora-adjacent danger, and score/placement pressure.
4. Add a small report-summary CLI for ignored benchmark JSON files so future docs can be updated
   without ad hoc extraction snippets.
5. Define an offline Mortal comparison boundary: start with a Tenhou XML to `mjai` decision-snapshot
   exporter, then compare Kenjaku decisions to a Mortal-compatible inference path only when weights
   are available and legally usable.
6. Add first supervised call/riichi decision baselines from the existing call examples once discard
   risk/defense diagnostics stop moving.
7. Scale to larger local slices, such as 500 logs, only after the 100-log defense regression is
   understood.
