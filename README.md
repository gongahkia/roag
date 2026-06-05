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
  data/fixtures/tenhou --epochs 5 --report runs/discard-benchmark.json \
  --disagreements runs/discard-disagreements.json
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/fixtures/tenhou --epochs 5 --models fast --report runs/discard-benchmark-fast.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary runs/discard-benchmark.json
PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/discard-disagreements.json --examples 2 --tags
PYTHONPATH=src python3 -m kenjaku benchmark-call \
  data/fixtures/tenhou --report runs/call-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-riichi \
  data/fixtures/tenhou --report runs/riichi-benchmark.json
```

CLI commands accept one or more Tenhou XML files or directories. Keep real downloaded logs outside
git, then point the CLI at those local paths. Reports and model artifacts belong under ignored
local directories such as `runs/` and `models/`. `benchmark-discard` reports frequency,
raw-count linear, shanten-aware linear, risk-context linear, defense-context linear, and
defense-context-v1 linear baselines on the same deterministic split, including held-out error
analysis by shanten impact, tile family, round phase, seat-relative turn phase, active opponent
riichi, actual-discard genbutsu/suji/kabe/one-chance status, and pre/post-riichi visibility.
Use `--l2` to apply linear-model L2 regularization, `--disagreements` to write local-only model
disagreement diagnostics, `--models fast` to skip slower ablation anchors during iteration,
`benchmark-report-summary` to compare ignored benchmark reports, and
`disagreement-report-summary` to aggregate, tag, and render capped disagreement examples.
`benchmark-call` provides supervised call/pass baselines on existing call examples, including
selective `call-linear-v0` and richer additive `call-linear-v1` models plus imbalance-aware
accuracy and recall metrics. `benchmark-riichi` provides conservative supervised riichi/pass
reports for both the frequency floor and `riichi-linear-v0`.

For continuation context, see `docs/session-handoff.md`.

## Data

Raw game logs and derived training datasets are local-only. See `docs/data-policy.md` and
`data/README.md` before adding fixtures or downloader scripts.
