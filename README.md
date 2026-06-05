# kenjaku

Open-source riichi mahjong AI research agent and replay-analysis toolkit.

The current milestone is Phase 0: build a small, tested Python core for tile/state/action
representation, then validate a compliant Tenhou log parsing path before any model training.

Live ladder automation is intentionally out of scope unless a platform grants explicit permission.
See `IDEAS.md` for the research roadmap and implementation log.

## Verify

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m kenjaku --version
PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/fixtures/tenhou/minimal_4p.xml
PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/fixtures/tenhou \
  --report runs/inspect-report.json
PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/fixtures/tenhou \
  --skip-errors --report runs/inspect-report.json
PYTHONPATH=src python3 -m kenjaku train-discard-baseline data/fixtures/tenhou/minimal_4p.xml
PYTHONPATH=src python3 -m kenjaku train-discard-linear data/fixtures/tenhou --epochs 5
PYTHONPATH=src python3 -m kenjaku train-discard-linear \
  data/fixtures/tenhou --report runs/linear-report.json
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/fixtures/tenhou --epochs 5 --report runs/discard-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary runs/discard-benchmark.json
```

CLI commands accept one or more Tenhou XML files or directories. Keep real downloaded logs outside
git, then point the CLI at those local paths. Reports and model artifacts belong under ignored
local directories such as `runs/` and `models/`. `benchmark-discard` reports frequency,
raw-count linear, shanten-aware linear, risk-context linear, defense-context linear, and
defense-context-v1 linear baselines on the same deterministic split, including held-out error
analysis by shanten impact, tile family, round phase, seat-relative turn phase, active opponent
riichi, actual-discard genbutsu/suji/kabe/one-chance status, and pre/post-riichi visibility.
Use `--l2` to apply linear-model L2 regularization, `--disagreements` to write local-only model
disagreement diagnostics, and `benchmark-report-summary` to compare ignored benchmark reports.

For continuation context, see `docs/session-handoff.md`.

## Data

Raw game logs and derived training datasets are local-only. See `docs/data-policy.md` and
`data/README.md` before adding fixtures or downloader scripts.
