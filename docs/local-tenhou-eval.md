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
```

## Run Kenjaku

```bash
SOURCE_COMMAND="houou-logs export data/raw/tenhou/db/2024.db data/raw/tenhou/xml/4p-hanchan"

PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/raw/tenhou/xml/4p-hanchan

PYTHONPATH=src python3 -m kenjaku benchmark-discard data/raw/tenhou/xml/4p-hanchan \
  --epochs 3 \
  --eval-fraction 0.2 \
  --report runs/discard-benchmark-local-report.json \
  --source-label tenhou-4p-hanchan-local \
  --source-date 2026-current-year \
  --source-command "$SOURCE_COMMAND --players 4 --length h --limit 100"

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
shanten-aware linear model, and the risk-context linear model on the same split. The `ablation`
block records the shanten-aware lift over raw counts and the risk-context lift over the
shanten-aware model. Each model also includes `eval_analysis`, which breaks held-out accuracy down
by actual discard shanten impact, discarded tile family, rough round event phase, and seat-relative
turn phase.

Commit neither the exported XML nor the generated model/report artifacts unless a later release
review explicitly clears the artifact for redistribution.
