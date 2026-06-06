# Local Tenhou Evaluation

This runbook validates Kenjaku on local Tenhou XML without turning the repository into a log
mirror. Keep databases, exported XML, model artifacts, and reports in ignored paths.

## Source Constraints

- Tenhou logs must not be redistributed.
- `houou-logs` should run as one download session at a time.
- Exported XML belongs under `data/raw/`, not in git.

Reference: https://github.com/Apricot-S/houou-logs

## Local Layout

```bash
mkdir -p data/raw/tenhou/db data/raw/tenhou/xml/4p-hanchan models runs
```

## Fetch A Small Sample

Use either a manually downloaded Tenhou archive or the maintained fetch path.

```bash
# Archive path. Keep the zip under data/raw/ or outside the repo.
houou-logs import data/raw/tenhou/db/2024.db data/raw/tenhou/scraw2024.zip

# Current-year path. This observes houou-logs' own FileIndex throttling.
houou-logs fetch data/raw/tenhou/db/current-year.db
```

Download a deliberately small validation sample first.

```bash
houou-logs download data/raw/tenhou/db/2024.db --players 4 --length h --limit 100
houou-logs validate data/raw/tenhou/db/2024.db
houou-logs export data/raw/tenhou/db/2024.db data/raw/tenhou/xml/4p-hanchan \
  --players 4 --length h --limit 100
houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-500 \
  --players 4 --length h --limit 500
```

## Run Kenjaku

```bash
SOURCE_COMMAND="houou-logs export data/raw/tenhou/db/2024.db data/raw/tenhou/xml/4p-hanchan"

PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/raw/tenhou/xml/4p-hanchan

PYTHONPATH=src python3 -m kenjaku benchmark-discard data/raw/tenhou/xml/4p-hanchan \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --models fast \
  --eval-fraction 0.2 \
  --report runs/discard-benchmark-local-report.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/discard-benchmark-local-report.json

PYTHONPATH=src python3 -m kenjaku benchmark-discard data/raw/tenhou/xml/4p-hanchan \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --eval-fraction 0.2 \
  --disagreements runs/discard-disagreements-local.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/discard-disagreements-local.json --examples 2 --tags

PYTHONPATH=src python3 -m kenjaku benchmark-call data/raw/tenhou/xml/4p-hanchan \
  --eval-fraction 0.2 \
  --include-weighted \
  --call-positive-weight 2.0 \
  --report runs/call-benchmark-local-report.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

PYTHONPATH=src python3 -m kenjaku benchmark-call data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v0 \
  --epochs 5 \
  --models fast \
  --example-limit 5000 \
  --call-threshold-source train-best \
  --report runs/call-benchmark-tenhou-500-fast-limit5000-epochs5-train-best-v0-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 500"

PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/raw/tenhou/xml/4p-hanchan \
  --eval-fraction 0.2 \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --report runs/riichi-benchmark-local-report.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v0 \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v0-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 500"

PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/call-benchmark-local-report.json \
  runs/riichi-benchmark-local-report.json

PYTHONPATH=src python3 -m kenjaku export-decision-snapshots data/raw/tenhou/xml/4p-hanchan \
  --decision-types discard,call,riichi \
  --limit 1000 \
  --output runs/decision-snapshots-local.jsonl \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

PYTHONPATH=src python3 -m kenjaku decision-snapshot-summary \
  runs/decision-snapshots-local.jsonl

PYTHONPATH=src python3 -m kenjaku train-discard-linear data/raw/tenhou/xml/4p-hanchan \
  --epochs 5 \
  --eval-fraction 0.2 \
  --output models/discard-linear-local.json \
  --report runs/discard-linear-local-report.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"
```

The benchmark report scores the frequency baseline, a raw hand/visible-count linear model, the
shanten-aware linear model, the risk-context linear model, the defense-context linear model, and the
defense-context-v1 linear model on the same split. The `ablation` block records the shanten-aware
lift over raw counts, the risk-context lift over the shanten-aware model, the defense-context lift
over risk context, and the defense-context-v1 lift over defense context. Each model also includes
`eval_analysis`, which breaks held-out accuracy down by actual discard shanten impact, discarded
tile family, rough round event phase, seat-relative turn phase, active opponent riichi,
actual-discard genbutsu/suji/kabe/one-chance status, and whether the actual discard was visible
before or after an opponent riichi declaration. Linear model reports also include weight and feature
activation summaries. Use `--models fast` for repeated sweeps that only need frequency,
shanten-aware, risk-context, and defense-context results. `--disagreements` writes capped local
examples where risk/defense model predictions differ, including legal-candidate logits for
debugging; it requires the risk, defense, and defense-v1 models.

Use `benchmark-report-summary` to compare multiple ignored discard, call, and riichi benchmark JSON
reports without copying raw logs or full report artifacts into git. Use
`disagreement-report-summary --examples N` to
compare capped disagreement artifacts and print representative stored examples; add `--tags` to
also count deterministic `defense_signal`, `efficiency_like`, `close_logit`, active-riichi, and
safe-tile labels; add `--tag TAG` to render only matching stored examples. `benchmark-call` scores
pass-allowed frequency, legal-call-only frequency, `call-linear-v0`, and additive
`call-linear-v1` baselines, and reports overall accuracy, balanced accuracy, pass recall, call
recall, per-action recall, a fixed-threshold `call_linear_v1_calibrated` policy variant, and
report-only call/pass threshold calibration. Use `--models fast --example-limit N` for bounded call
comparisons on larger slices and `--call-threshold-source train-best` to evaluate a train-selected
calibrated threshold without using eval-selected diagnostics as policy. Add `--include-weighted` to
compare the default threshold policy against `call_linear_v1_weighted`. `benchmark-riichi` builds a
conservative riichi/pass dataset and scores both the frequency floor and `riichi-linear-v0`,
including the fixed-threshold or train-selected `riichi_linear_calibrated` policy variant, optional
`riichi_linear_weighted`, and report-only riichi/pass threshold calibration.

Use `export-decision-snapshots` to write local-only JSONL rows for discard/call/riichi decisions.
Rows include Kenjaku reconstruction fields, legal actions, the observed action, and a minimal
`mjai_events` prefix; this is the neutral handoff format for future offline Mortal comparison. Use
`decision-snapshot-summary` to validate snapshot JSONL counts before handing the file to a future
external baseline comparator.

Commit neither the exported XML nor the generated model/report artifacts unless a later release
review explicitly clears the artifact for redistribution.
