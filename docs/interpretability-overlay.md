# Interpretability Overlay

`docs/media/interpretability-overlay.html` renders 100 discard decisions from a generated local
synthetic Tenhou-style export. The artifact contains only derived decision metadata and heuristic
audit scores; it does not render source XML paths or raw private replay data.

The viewer stores decisions in an embedded JSON payload and renders 100 decisions per page in the
browser. Client-side controls filter by round, seat, actual discard tile, and shanten-delta bin;
the search box matches hand patterns and dora indicators.

The overlay reports, per decision:

- top 3 discard alternatives
- policy probability from `heuristic-discard-overlay-v0`
- post-discard shanten delta
- hand pattern and dora indicators for local search
- estimated deal-in risk from active-riichi, river, and visible-count signals
- expected point impact from `-shanten_delta * 1200 - deal_in_risk * 8000`

These values are inspection signals, not calibrated strength claims.

## Reproduce

```bash
python3.13 - <<'PY'
from pathlib import Path

root = Path("runs/interpretability-overlay")
root.mkdir(parents=True, exist_ok=True)
out = root / "synthetic-100.xml"
draw_tags = "TUVW"
discard_tags = "DEFG"
lines = [
    "<mjloggm>",
    '  <INIT',
    '    seed="0,0,0,0,0,72"',
    '    ten="250,250,250,250"',
    '    oya="0"',
    '    hai0="0,4,8,12,16,20,24,28,32,36,40,44,48"',
    '    hai1="1,5,9,13,17,21,25,29,33,37,41,45,49"',
    '    hai2="2,6,10,14,18,22,26,30,34,38,42,46,50"',
    '    hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"',
    '  />',
    '  <DORA hai="72" />',
    '  <REACH who="1" step="1" />',
    '  <REACH who="1" step="2" ten="250,240,250,250" />',
]
for turn in range(100):
    seat = turn % 4
    tile_id = 52 + (turn % 84)
    lines.append(f"  <{draw_tags[seat]}{tile_id} />")
    lines.append(f"  <{discard_tags[seat]}{tile_id} />")
lines.extend(["  <RYUUKYOKU />", "</mjloggm>"])
out.write_text("\n".join(lines) + "\n", encoding="utf-8")
PY

PYTHONPATH=src python3.13 -m kenjaku export-decision-snapshots \
  runs/interpretability-overlay/synthetic-100.xml \
  --decision-types discard \
  --output runs/interpretability-overlay/decision-snapshots.jsonl \
  --limit 100 \
  --source-label synthetic-interpretability-local

PYTHONPATH=src python3.13 -m kenjaku interpretability-overlay \
  runs/interpretability-overlay/decision-snapshots.jsonl \
  --output docs/media/interpretability-overlay.html \
  --limit 100 \
  --min-decisions 100 \
  --title "Kenjaku Interpretability Overlay"
```

Expected CLI summary:

```text
snapshots: 100
decision_types: discard
decisions: 100
policy_kind: heuristic-discard-overlay-v0
skipped_snapshot_rows: 0
malformed_snapshot_rows: 0
```
