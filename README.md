# kenjaku

Open-source riichi mahjong AI research agent and replay-analysis toolkit.

Kenjaku currently focuses on a compliant local Tenhou workflow: parse XML logs, reconstruct
decision points, train small supervised baselines, export neutral decision snapshots, and compare
local prediction artifacts. Live ladder automation is intentionally out of scope unless a platform
grants explicit permission.

The repo is not yet a transformer agent, RL system, Sanma implementation, browser demo, or full
scoring engine. See `IDEAS.md` and `docs/session-handoff.md` for the research roadmap and running
handoff notes.

## Current State

- Tested tile/action/state primitives, 4-player Tenhou XML parsing, exact Tenhou meld decoding,
  shanten calculation, and draw/discard/call reconstruction.
- Supervised discard, call/pass, and riichi/pass example builders over local Tenhou XML.
- Dependency-free frequency and linear baselines with deterministic train/eval splits, calibration
  reports, feature summaries, and local-only disagreement diagnostics.
- Neutral decision snapshot JSONL export plus prediction JSONL comparison.
- Local-only Mortal reconnaissance documented behind an AGPL-safe subprocess/data boundary.
- A minimal PyTorch discard MLP baseline for validating the next supervised-learning path.

## Quickstart

Use a supported Python version: `>=3.11,<3.14`.

```bash
python3.13 -m pip install -e ".[dev]"
PYTHONPATH=src python3.13 -m unittest discover -s tests
PYTHONPATH=src python3.13 -m compileall -q src tests
python3.13 -m ruff check .
PYTHONPATH=src python3.13 -m kenjaku --version
```

If you are working from this checkout without installing the package, keep `PYTHONPATH=src`.

## Fixture Smokes

```bash
PYTHONPATH=src python3.13 -m kenjaku inspect-tenhou data/fixtures/tenhou

PYTHONPATH=src python3.13 -m kenjaku benchmark-discard \
  data/fixtures/tenhou --epochs 3 --models fast \
  --report runs/fixture-discard-benchmark.json \
  --disagreements runs/fixture-disagreements.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-call \
  data/fixtures/tenhou --models fast --example-limit 1 \
  --example-limit-strategy balanced --profile-stages \
  --feature-cache runs/fixture-call-feature-cache.json \
  --call-threshold-source train-best \
  --report runs/fixture-call-benchmark.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi \
  data/fixtures/tenhou --riichi-threshold-source train-best \
  --report runs/fixture-riichi-benchmark.json

PYTHONPATH=src python3.13 -m kenjaku train-discard-mlp \
  data/fixtures/tenhou --epochs 1 --batch-size 4 --device cpu \
  --report runs/fixture-discard-mlp.json
```

## Decision Snapshots

Decision snapshots are the neutral interchange format for external baselines. They include stable
`row_id` values, legal actions, observed actions, reconstruction fields, and a minimal mjai-style
event prefix. Terminal outcome labels are opt-in to avoid leaking future information into inference
snapshots.

```bash
PYTHONPATH=src python3.13 -m kenjaku export-decision-snapshots \
  data/fixtures/tenhou --output runs/decision-snapshots.jsonl --limit 20

PYTHONPATH=src python3.13 -m kenjaku produce-decision-predictions \
  runs/decision-snapshots.jsonl --strategy echo-actual \
  --output runs/decision-predictions.jsonl

PYTHONPATH=src python3.13 -m kenjaku decision-snapshot-compare \
  runs/decision-snapshots.jsonl runs/decision-predictions.jsonl
```

Stub prediction strategies are for protocol tests only. Real Mortal comparison should remain behind
a subprocess/data boundary and requires legally usable weights.

## Local Tenhou Data

Raw game logs, exported XML, model artifacts, feature caches, and reports are local-only. Keep them
under ignored paths such as `data/raw/`, `runs/`, and `models/`. Do not commit downloaded Tenhou
logs or processed datasets that can reconstruct restricted source logs.

The local runbook is in `docs/local-tenhou-eval.md`; data constraints are in `docs/data-policy.md`.

Current useful local commands:

```bash
PYTHONPATH=src python3.13 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 --split-seed tenhou-500-v0 \
  --epochs 30 --learning-rate 0.05 \
  --models call_linear_v1,call_linear_v1_calibrated,call_linear_v1_weighted \
  --include-weighted --call-positive-weight 1.0 \
  --example-limit 20000 \
  --example-limit-strategy balanced --call-threshold-source train-best \
  --profile-stages \
  --feature-cache runs/call-features-tenhou-500-balanced-limit20000-v1.json \
  --report runs/call-benchmark-tenhou-500-balanced-limit20000-v1-report.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 --split-seed tenhou-500-v2 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v2-report.json
```

## Prioritized Next Work

1. Keep call/pass work focused on cache-safe scaling beyond the 20k balanced cap and on comparing
   calibrated versus positive-weight policies.
2. Keep `riichi_linear_calibrated` with train-best thresholds as the baseline. Fixed threshold
   `0.25` improves riichi recall but is not the best balanced policy on the current 500-log splits.
3. Use decision snapshot prediction producers for external-baseline protocol work; do not import
   or copy AGPL baseline code.
4. Use discard disagreement tags before adding another feature profile.
5. Add richer supervised learning only after the PyTorch fixture path stays reproducible.
