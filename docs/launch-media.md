# Launch Media

Status: generated from synthetic fixtures on 2026-07-07.

Selected public-safe artifacts:

- `docs/media/replay-analysis.mp4`
- `docs/media/play.mp4`
- `docs/media/benchmark-summary.png`

Source data:

- `data/fixtures/tenhou`

Verified fixture outputs:

```text
decision snapshots: 8 rows, 0 malformed
decision types: discard=4, call=1, riichi=3
mjai_events: present=8, missing=0
echo-actual comparison: overall_accuracy=1.0000
fixture discard benchmark: 4 examples, 3 train, 1 eval
fast discard models: frequency, linear, risk_context_linear, defense_context_linear
```

## Generate Source Artifacts

```bash
mkdir -p runs/launch-media docs/media
(
  cd web
  npm ci
  npm run build
)
PYTHONPATH=src python3.13 -m kenjaku export-decision-snapshots \
  data/fixtures/tenhou --output runs/launch-media/decision-snapshots.jsonl --limit 20
PYTHONPATH=src python3.13 -m kenjaku produce-decision-predictions \
  runs/launch-media/decision-snapshots.jsonl --strategy echo-actual \
  --output runs/launch-media/decision-predictions.jsonl
PYTHONPATH=src python3.13 -m kenjaku decision-snapshot-summary \
  runs/launch-media/decision-snapshots.jsonl
PYTHONPATH=src python3.13 -m kenjaku decision-snapshot-compare \
  runs/launch-media/decision-snapshots.jsonl runs/launch-media/decision-predictions.jsonl
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard \
  data/fixtures/tenhou --epochs 3 --models fast \
  --report runs/launch-media/fixture-discard-benchmark.json
PYTHONPATH=src python3.13 -m kenjaku benchmark-report-summary \
  runs/launch-media/fixture-discard-benchmark.json
PYTHONPATH=src python3.13 -m kenjaku benchmark-dashboard \
  runs/launch-media/fixture-discard-benchmark.json \
  --output runs/launch-media/benchmark-dashboard/index.html \
  --title "Kenjaku Fixture Benchmark Summary"
```

## Capture Browser Artifacts

Run the browser-table preview in one shell:

```bash
cd web
npm run preview -- --host 127.0.0.1 --port 8876 --strictPort
```

Capture the rendered gameplay surface from another shell:

```bash
PWCLI="$HOME/.codex/skills/playwright/scripts/playwright_cli.sh"
"$PWCLI" open http://127.0.0.1:8876/
"$PWCLI" resize 1280 720
"$PWCLI" screenshot --filename runs/launch-media/play.png --full-page
"$PWCLI" close
```

Run the benchmark-dashboard server in one shell:

```bash
cd runs/launch-media
python3.13 -m http.server 8877 --bind 127.0.0.1
```

Capture the selected benchmark image from another shell:

```bash
PWCLI="$HOME/.codex/skills/playwright/scripts/playwright_cli.sh"
"$PWCLI" open http://127.0.0.1:8877/benchmark-dashboard/index.html
"$PWCLI" resize 1280 720
"$PWCLI" screenshot --filename docs/media/benchmark-summary.png --full-page
"$PWCLI" close
```

The only browser console error observed during capture was a missing `favicon.ico` 404.

## Encode Clips

```bash
magick -size 1280x720 xc:'#0b1117' \
  -font /System/Library/Fonts/SFNSMono.ttf \
  -fill '#e6edf3' -pointsize 34 -gravity NorthWest \
  -annotate +64+64 $'Kenjaku replay-analysis fixture smoke\n\nsource: data/fixtures/tenhou\nexport: decision snapshots, limit=20\nrows: 8 valid, 0 malformed\ndecisions: discard=4, call=1, riichi=3\nmjai_events: present=8, missing=0\n\npublic-safety: synthetic fixtures only; no raw private replay URLs' \
  runs/launch-media/replay-frame-01.png
magick -size 1280x720 xc:'#0b1117' \
  -font /System/Library/Fonts/SFNSMono.ttf \
  -fill '#e6edf3' -pointsize 34 -gravity NorthWest \
  -annotate +64+64 $'Prediction protocol compare\n\nstrategy: echo-actual\npredictions: 8\nmissing_predictions: 0\nmalformed_snapshot_rows: 0\nmalformed_prediction_rows: 0\nduplicate_prediction_rows: 0\noverall_accuracy: 1.0000\n\nby type: call=1.0000/1, discard=1.0000/4, riichi=1.0000/3' \
  runs/launch-media/replay-frame-02.png
ffmpeg -y -framerate 0.5 -i runs/launch-media/replay-frame-%02d.png \
  -vf "scale=1280:720,format=yuv420p" -c:v libx264 -movflags +faststart \
  docs/media/replay-analysis.mp4
ffmpeg -y -loop 1 -i runs/launch-media/play.png \
  -vf "zoompan=z=1:x=0:y='min(ih-oh,on*4)':d=120:s=1280x720:fps=30,format=yuv420p" \
  -t 4 -c:v libx264 -movflags +faststart docs/media/play.mp4
```

## Verify Media

```bash
ffprobe -v error -select_streams v:0 \
  -show_entries stream=width,height,nb_frames,duration \
  -of default=noprint_wrappers=1 docs/media/play.mp4
ffprobe -v error -select_streams v:0 \
  -show_entries stream=width,height,nb_frames,duration \
  -of default=noprint_wrappers=1 docs/media/replay-analysis.mp4
identify docs/media/benchmark-summary.png
```

Expected media properties:

```text
play.mp4: 1280x720, 4.000000 seconds, 120 frames
replay-analysis.mp4: 1280x720, 4.000000 seconds, 2 frames
benchmark-summary.png: 1280x1395
```
