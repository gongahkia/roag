# External Baselines

This file records local-only baseline reconnaissance. Do not commit cloned upstream repositories,
downloaded model weights, generated simulator logs, or copied dynamic libraries.

## Mortal

Local checkout status on 2026-06-05:

- Repo: `https://github.com/Equim-chan/Mortal`
- Ignored checkout path: `data/raw/external/mortal`
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`
- License note: code is AGPL-3.0-or-later; keep integration boundaries explicit before reusing code.
- Upstream docs describe Mortal as a Rust `libriichi` simulator plus Python policy code with an
  `mjai` interface and 1v3 duplicate-mahjong evaluation.
- Trained weights are not part of the repo; config files expect local `*.pth` state files.

Source check on 2026-06-13:

- Mortal docs: `https://mortal.ekyu.moe/`
- Mortal duplicate-mahjong strength page: `https://mortal.ekyu.moe/perf/strength.html`
- Mortal Docker inference page: `https://mortal.ekyu.moe/user/docker.html`
- Mortal repository/license: `https://github.com/Equim-chan/Mortal`
- mjai-reviewer engine interface examples: `https://github.com/Equim-chan/mjai-reviewer`
- akochan repository: `https://github.com/critter-mj/akochan`
- akochan terms file: `https://github.com/critter-mj/akochan/blob/master/LICENSE`

Mortal remains AGPL-3.0-or-later and its documented Docker path requires model files supplied
separately. akochan has its own Japanese terms file rather than a standard permissive SPDX license;
do not copy or import akochan code/assets without a separate license review.

Local build checks that passed:

```bash
git clone --depth 1 https://github.com/Equim-chan/Mortal data/raw/external/mortal
cd data/raw/external/mortal
cargo build -p libriichi --lib --release
cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture
cp target/release/libriichi.dylib mortal/libriichi.so
PYTHONPATH=mortal python3 -c "import libriichi; print('ok')"
```

Observed local result:

- `cargo build -p libriichi --lib --release` finished successfully in 37.70s.
- `target/release/libriichi.dylib` was produced at about 3.1 MB on macOS.
- Rust tests passed: `28 passed; 0 failed`, plus doctests reported OK.
- `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded after copying the dylib to
  `mortal/libriichi.so`.

Practical comparison path:

1. Keep Mortal as an external, ignored baseline until Kenjaku has a policy that can act through a
   common interface.
2. Prefer a neutral decision/export layer over copying AGPL simulator code into Kenjaku.
3. Use `export-decision-snapshots` to produce local JSONL decision rows with stable `row_id` values
   and minimal `mjai_events` prefixes. Validate the export with `decision-snapshot-summary`, then
   use `produce-decision-predictions` only for stub protocol checks (`pass`, `first-legal`, or
   `echo-actual`). Compare Kenjaku rows against real prediction JSONL via
   `decision-snapshot-compare` only when a Mortal-compatible inference path and legally usable
   weights are available. Use `run-external-prediction-producer` for any real producer subprocess:
   Kenjaku sets `KENJAKU_SNAPSHOTS` and `KENJAKU_PREDICTIONS`, the producer writes prediction
   JSONL, and Kenjaku performs validation/comparison afterward.
4. For head-to-head evaluation, target duplicate-mahjong offline runs through a common simulator or
   a subprocess boundary; do not add live ladder automation.

Do not import Mortal code or wire AGPL modules into Kenjaku for prediction. A real external
producer should be a separate process that reads decision snapshots and writes
`{"row_id": "...", "predicted_action": {...}}` JSONL.

Generic producer smoke shape:

```bash
PYTHONPATH=src python3 -m kenjaku run-external-prediction-producer \
  runs/decision-snapshots-local.jsonl \
  --output runs/external-predictions-local.jsonl \
  --compare-report runs/external-predictions-local-compare.json \
  --command /path/to/producer
```

## TODO-304 External-Baseline Report

Use an offline shared-log protocol:

1. Export one decision-snapshot JSONL scenario set from local permitted logs. This is the shared
   evaluation set for Kenjaku, Mortal-compatible producers, and akochan-compatible producers.
2. Have each baseline write prediction JSONL rows keyed by `row_id` with a `predicted_action`
   payload. Real Mortal or akochan integrations must run as separate processes and provide legally
   usable code, weights, and runtime artifacts outside this repository.
3. Run `external-baseline-report` over the shared snapshots and every baseline prediction file. The
   report computes exact-action accuracy, call/riichi binary metrics, and Wilson 95% confidence
   intervals. The command requires at least 1,000 comparable decisions per baseline by default.

Local proof run on 2026-06-13 used the ignored 130-log Tenhou XML scenario set under
`data/raw/tenhou/xml/todo-101-deal-in-130`. The Mortal-compatible and akochan-compatible rows below
are deterministic protocol smoke baselines, not real engine-strength measurements.

```bash
mkdir -p runs/todo-304

PYTHONPATH=src python3 -m kenjaku export-decision-snapshots \
  data/raw/tenhou/xml/todo-101-deal-in-130 \
  --output runs/todo-304/shared-log-decision-snapshots.jsonl \
  --decision-types discard,call,riichi \
  --source-label todo-304-shared-log \
  --skip-errors

PYTHONPATH=src python3 -m kenjaku produce-decision-predictions \
  runs/todo-304/shared-log-decision-snapshots.jsonl \
  --strategy first-legal \
  --output runs/todo-304/kenjaku-first-legal.jsonl

PYTHONPATH=src python3 -m kenjaku produce-decision-predictions \
  runs/todo-304/shared-log-decision-snapshots.jsonl \
  --strategy pass \
  --output runs/todo-304/mortal-compatible-pass-smoke.jsonl

PYTHONPATH=src python3 -m kenjaku produce-decision-predictions \
  runs/todo-304/shared-log-decision-snapshots.jsonl \
  --strategy first-legal \
  --output runs/todo-304/akochan-compatible-first-legal-smoke.jsonl

PYTHONPATH=src python3 -m kenjaku external-baseline-report \
  runs/todo-304/shared-log-decision-snapshots.jsonl \
  --baseline kenjaku:first-legal=runs/todo-304/kenjaku-first-legal.jsonl \
  --baseline mortal-compatible:pass-smoke=runs/todo-304/mortal-compatible-pass-smoke.jsonl \
  --baseline akochan-compatible:first-legal-smoke=runs/todo-304/akochan-compatible-first-legal-smoke.jsonl \
  --report runs/todo-304/external-baseline-report.json
```

Observed report summary:

- Shared snapshots: 86,003 decisions; malformed snapshot rows: 0; `mjai_events` present: 86,003.
- Per-baseline comparable decisions: 86,003; missing predictions: 0; illegal predictions: 0.
- Default 1,000-decision gate: satisfied.
- Overall smoke accuracy for all three protocol baselines: 0.1930, Wilson 95% CI
  `[0.1904, 0.1957]`.

Replace the smoke prediction files with real external producer output to obtain a true Mortal or
akochan measurement without changing the report command or ranked-service automation boundary.
