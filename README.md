# kenjaku

Open-source riichi mahjong AI research agent and replay-analysis toolkit.

Kenjaku currently focuses on a compliant local Tenhou workflow: parse XML logs, reconstruct
decision points, train small supervised baselines, export neutral decision snapshots, and compare
local prediction artifacts. Live ladder automation is intentionally out of scope unless a platform
grants explicit permission.

The repo is not yet a transformer agent, RL system, complete Sanma implementation, or full scoring
engine. See `TODO.md` and `docs/session-handoff.md` for the research backlog and running handoff
notes.

## Launch Evidence

Fixture-only launch media generated on 2026-07-07:

- Replay-analysis clip: `docs/media/replay-analysis.mp4`
- Browser-demo clip: `docs/media/browser-demo.mp4`
- Benchmark summary image: `docs/media/benchmark-summary.png`

These artifacts use only `data/fixtures/tenhou`; they do not include raw private replay data,
player/account data, model weights, or live ladder data.

Exact reproduction commands:

```bash
mkdir -p runs/launch-media docs/media
PYTHONPATH=src python3.13 -m kenjaku browser-demo \
  --output-dir runs/launch-media/browser-demo --no-serve
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

Capture and encoding commands are recorded in `docs/launch-media.md`.

## Current State

- Tested tile/action/state primitives, 4-player Tenhou XML parsing, exact Tenhou meld decoding,
  shanten calculation, and draw/discard/call reconstruction.
- Supervised discard, call/pass, and riichi/pass example builders over local Tenhou XML.
- Dependency-free frequency and linear baselines with deterministic train/eval splits, calibration
  reports, feature summaries, and local-only disagreement diagnostics.
- Neutral decision snapshot JSONL export plus prediction JSONL comparison.
- Permission-aware replay manifest review with accepted-item JSONL output for offline replay
  analysis queues. It validates consent/provenance gates but does not fetch from live services.
- Offline replay share planning plus public-safe replay summary generation for accepted intake
  rows, gated by demo/redistribution permission scope. These commands do not fetch, post, or expose
  raw replay URLs.
- Static browser-playable demo generation via `kenjaku browser-demo`. The demo writes local
  HTML/CSS/JS assets, can be served by the CLI, and does not require or embed raw replay data.
- Basic closed-hand winning-shape detection for standard, chiitoitsu, and kokushi hands, plus
  optional synthetic tsumo termination in the self-play sandbox. This is not complete yaku
  validation or scoring.
- A reusable sandbox environment boundary with deterministic initial state, draw transitions, legal
  discard actions and discard history, pending-discard reaction windows, legal chi/pon/minkan call
  actions, legal closed-hand tsumo/ron actions, discard-furiten, temporary ron-pass furiten, and
  seeded riichi-furiten filtering, basic closed-tenpai riichi declaration with future-draw gating,
  first-turn double-riichi detection, post-riichi discard/call restrictions, basic wait-preserving
  post-riichi closed-kan exceptions, a basic riichi deposit/stick ledger with declaration-discard
  deal-in refunds, basic honba bonus deltas, a basic next-round dealer/honba transition
  helper with round-wind dealer-wrap progression, all-last and West-round sudden-death game-end
  handling, bankruptcy termination, ending-riichi-stick top-take, and 3p/4p return-point,
  oka/uma, placement, and final-rank metadata, basic active/winning ippatsu metadata, basic
  closed-kan/ankan and added-kan/kakan actions with dead-wall replacement draws, kan-dora indicator
  metadata, rinshan draw-source metadata, timing fixtures for rinshan after minkan/ankan/kakan,
  haitei/houtei final live-wall and replacement-draw edges, basic double-riichi yaku metadata, a
  basic chankan ron/pass window before kakan replacement draw with ippatsu preserved until the
  reaction resolves plus kokushi-only ankan robbery and chankan dora timing fixtures, individual reaction
  passes, ron-priority call gating, discard/call/tsumo/ron application, basic multi-ron terminal
  resolution with turn-priority riichi-stick assignment, basic open/kan
  standard-shape win detection, a basic sandbox yaku filter/metadata layer with
  dragon/round-wind/seat-wind yakuhai filtering plus toitoi and honroutou, selectable self-play
  reward projections for terminal, point-delta, normalized point-delta, and placement-delta modes,
  plus terminal point-delta and score-estimate metadata, including basic
  dealer-aware win payments,
  visible-dora, kan-dora, riichi-gated ura/kan-ura, red-five, and Kita bonus han,
  basic kazoe-yakuman limits, yakuman bonus-han suppression, tsumo
  yaku/dora tile views that do not duplicate the drawn tile, and basic live-wall exhaustive-draw
  tenpai/noten point deltas plus basic Nagashi mangan wall-exhaustion and next-round progression.
  Abortive draws are also modeled for kyuushu kyuuhai, four winds, four riichi, four kans, and
  triple ron as neutral point terminals that repeat dealer, increment honba, keep round wind, and
  carry riichi sticks.
  The sandbox also has narrow Sanma
  support: static 3-player tile exclusions, 35,000-point starts, no-chi call filtering,
  North-as-guest-wind yaku filtering, Tenhou's 1m/9m dora indicator wrap, and a basic
  Kita/pei-nuki action that tracks exposed North tiles separately from melds, opens a ron/pass
  reaction window when an opponent can win on the North, takes a delayed dead-wall replacement draw
  without revealing kan-dora after passes, suppresses immediate post-pon Kita, and counts as bonus
  han in sandbox score estimates while preserving ippatsu for immediate ron on the called North and
  using basic tsumo-loss point transfers plus an eight-rinshan replacement reserve cap. The
  self-play sandbox uses this boundary for
  4-player and static 3-player tile-set draw/discard turn-rotation plus
  terminal-outcome plumbing. A separate `self-play-match-sandbox` command can run deterministic
  multi-round 3-player or 4-player sandbox matches to final placement with pluggable discard, call,
  riichi, kan, Kita, ron/tsumo, and pass policies plus optional state/action/reward trajectories.
  `train-ppo-sandbox` runs a dependency-free fixture-scale PPO smoke trainer over those trajectories
  with policy/value losses, GAE, clipping, entropy regularization, mini-batching, checkpoint/resume,
  training curves, and evaluation summaries. `train-population-sandbox` maintains a pool of at
  least four PPO checkpoint snapshots, samples pool opponents, reports matchup metrics, and records
  promotion/replacement decisions.
  It is not a full riichi/Sanma simulator, complete yaku validator, complete yaku/scoring
  implementation, complete post-riichi kan timing model, scoring engine, automated full-match
  learned-policy self-play trainer, or strength-grade RL implementation.
- Local-only Mortal reconnaissance documented behind an AGPL-safe subprocess/data boundary.
- A PyTorch discard MLP baseline with per-epoch validation history and optional best-checkpoint
  artifacts for validating the next supervised-learning path.
- A dependency-free heuristic defense risk scorer for ranking candidate discard danger against
  active riichi opponents. This is not a calibrated deal-in probability estimator.
- Direct ron-discard label extraction plus a small dependency-free `deal-in-linear-v0` logistic
  estimator command for offline probability-estimator experiments. Its benchmark reports include
  threshold calibration sweeps and heuristic-risk comparisons. A local 130-log Tenhou smoke run on
  2026-06-13 trained on 55,252 decisions, evaluated on 10,525 decisions, and beat the heuristic
  baseline on eval Brier score and log loss. No trained deal-in model is bundled.
- A PyTorch transformer state encoder and masked discard policy head module for future supervised
  policy experiments. It is architecture scaffolding only; no trained transformer policy is bundled.
- `train-discard-transformer`, a PyTorch behavior-cloning command for supervised discard policy
  experiments over local Tenhou XML. No Tenhou Phoenix transformer run or trained checkpoint is
  bundled yet.
- `benchmark-discard-transformer`, which compares the transformer policy head against frequency,
  risk-context linear, and defense-context linear anchors on the same split.
- Public release planning lives in `docs/public-release.md`; it records the selected release name,
  canonical URL, channel owners, draft launch posts, outreach targets, and naming-risk checks while
  keeping public claims limited to verified local artifacts.

## Quickstart

Use a supported Python version: `>=3.11,<3.14`.

```bash
python3.13 -m pip install -e ".[dev]"
PYTHONPATH=src python3.13 -m unittest discover -s tests
PYTHONPATH=src python3.13 -m compileall -q src tests
python3.13 -m ruff check .
PYTHONPATH=src python3.13 -m kenjaku --version
PYTHONPATH=src python3.13 -m kenjaku status
```

If you are working from this checkout without installing the package, keep `PYTHONPATH=src`.

## Fixture Smokes

```bash
PYTHONPATH=src python3.13 -m kenjaku inspect-tenhou data/fixtures/tenhou

PYTHONPATH=src python3.13 -m kenjaku replay-intake-review \
  path/to/replay-manifest.json \
  --report runs/replay-intake-review.json \
  --accepted-output runs/replay-intake-accepted.jsonl
PYTHONPATH=src python3.13 -m kenjaku replay-share-plan \
  runs/replay-intake-accepted.jsonl --intent demo \
  --report runs/replay-share-plan.json
PYTHONPATH=src python3.13 -m kenjaku replay-public-summary \
  runs/replay-intake-accepted.jsonl --intent demo \
  --report runs/replay-public-summary.json

PYTHONPATH=src python3.13 -m kenjaku browser-demo \
  --output-dir runs/browser-demo
# CI/nonblocking smoke:
PYTHONPATH=src python3.13 -m kenjaku browser-demo \
  --output-dir runs/browser-demo --no-serve

PYTHONPATH=src python3.13 -m kenjaku self-play-sandbox \
  --episodes 2 --max-turns 32 --policy frequency --ruleset tenhou-3p \
  --reward-mode normalized-point-delta --stop-on-tsumo \
  --report runs/fixture-self-play-sandbox.json
PYTHONPATH=src python3.13 -m kenjaku self-play-match-sandbox \
  --games 1 --max-rounds 12 --max-turns-per-round 512 \
  --ron-policy pass --report runs/fixture-self-play-match.json
PYTHONPATH=src python3.13 -m kenjaku train-ppo-sandbox \
  --total-steps 1024 --rollout-games 1 --max-rounds 12 \
  --max-turns-per-round 512 --ppo-epochs 1 --batch-size 128 \
  --checkpoint runs/fixture-ppo-sandbox-checkpoint.json \
  --report runs/fixture-ppo-sandbox.json
PYTHONPATH=src python3.13 -m kenjaku train-population-sandbox \
  --pool-size 4 --generations 1 --candidates-per-generation 1 \
  --matchups-per-candidate 1 --total-steps 8 --max-rounds 1 \
  --max-turns-per-round 8 --evaluation-max-rounds 1 \
  --evaluation-max-turns-per-round 8 \
  --output-dir runs/fixture-population-sandbox \
  --report runs/fixture-population-sandbox.json

PYTHONPATH=src python3.13 -m kenjaku defense-risk-summary \
  data/fixtures/tenhou --report runs/fixture-defense-risk-summary.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-deal-in \
  data/fixtures/tenhou --epochs 2 --report runs/fixture-deal-in-benchmark.json
PYTHONPATH=src python3.13 -m kenjaku benchmark-report-summary \
  runs/fixture-deal-in-benchmark.json
PYTHONPATH=src python3.13 -m kenjaku benchmark-dashboard \
  runs/fixture-deal-in-benchmark.json \
  --output runs/public-benchmarks/index.html

PYTHONPATH=src python3.13 -m kenjaku benchmark-discard \
  data/fixtures/tenhou --epochs 3 --models fast \
  --report runs/fixture-discard-benchmark.json \
  --disagreements runs/fixture-disagreements.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-call \
  data/fixtures/tenhou --models fast --example-limit 1 \
  --example-limit-strategy balanced --profile-stages \
  --example-cache runs/fixture-call-example-cache.json \
  --feature-cache runs/fixture-call-feature-cache.json \
  --call-threshold-source train-best \
  --report runs/fixture-call-benchmark.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi \
  data/fixtures/tenhou --riichi-threshold-source train-best \
  --report runs/fixture-riichi-benchmark.json

PYTHONPATH=src python3.13 -m kenjaku train-discard-mlp \
  data/fixtures/tenhou --epochs 1 --batch-size 4 --device cpu \
  --checkpoint runs/fixture-discard-mlp.pt \
  --report runs/fixture-discard-mlp.json

PYTHONPATH=src python3.13 -m kenjaku train-discard-transformer \
  data/fixtures/tenhou --epochs 1 --batch-size 2 --device cpu \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --checkpoint runs/fixture-discard-transformer.pt \
  --report runs/fixture-discard-transformer.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-transformer \
  data/fixtures/tenhou --epochs 1 --batch-size 2 --device cpu \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --linear-epochs 1 \
  --checkpoint runs/fixture-discard-transformer-benchmark.pt \
  --report runs/fixture-discard-transformer-benchmark.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-mlp \
  data/fixtures/tenhou --epochs 1 --batch-size 4 --device cpu \
  --linear-epochs 1 \
  --report runs/fixture-discard-mlp-benchmark.json
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

PYTHONPATH=src python3.13 -m kenjaku external-baseline-report \
  runs/decision-snapshots.jsonl \
  --baseline kenjaku:first-legal=runs/kenjaku-predictions.jsonl \
  --baseline mortal-compatible:local-producer=runs/mortal-predictions.jsonl \
  --baseline akochan-compatible:local-producer=runs/akochan-predictions.jsonl \
  --report runs/external-baseline-report.json
```

Stub prediction strategies are for protocol tests only. Real Mortal comparison should remain behind
a subprocess/data boundary and requires legally usable weights. External producers can be tested
through `run-external-prediction-producer`, which passes `KENJAKU_SNAPSHOTS` and
`KENJAKU_PREDICTIONS` to a separate process and then reuses the same prediction comparator.
`external-baseline-report` compares multiple named prediction files from the same shared snapshots,
computes Wilson 95% confidence intervals, and requires 1,000 comparable decisions per baseline by
default.

## Local Tenhou Data

Raw game logs, exported XML, model artifacts, feature caches, and reports are local-only. Keep them
under ignored paths such as `data/raw/`, `runs/`, and `models/`. Do not commit downloaded Tenhou
logs or processed datasets that can reconstruct restricted source logs.

The local runbook is in `docs/local-tenhou-eval.md`; data constraints are in `docs/data-policy.md`.

Current useful local commands:

```bash
PYTHONPATH=src python3.13 -m kenjaku export-bc-examples \
  data/raw/tenhou/xml/todo-102-bc-6500 \
  --output-dir runs/todo-102/bc-examples-v0 \
  --shard-size 50000 \
  --source-label tenhou-4p-hanchan-todo-102 \
  --source-date 2026-06-21

PYTHONPATH=src python3.13 -m kenjaku benchmark-discard-from-examples \
  runs/todo-102/bc-examples-v0/manifest.json \
  --models fast --example-limit 125000 \
  --eval-fraction 0.16 --split-seed todo-102-discard-v0 \
  --report runs/todo-102/discard-benchmark-from-examples-v0.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-call-from-examples \
  runs/todo-102/bc-examples-v0/manifest.json \
  --models fast --example-limit 125000 --example-limit-strategy balanced \
  --eval-fraction 0.16 --split-seed todo-102-call-v0 \
  --call-threshold-source train-best \
  --report runs/todo-102/call-benchmark-from-examples-v0.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi-from-examples \
  runs/todo-102/bc-examples-v0/manifest.json \
  --example-limit 125000 \
  --eval-fraction 0.16 --split-seed todo-102-riichi-v0 \
  --riichi-threshold-source train-best \
  --report runs/todo-102/riichi-benchmark-from-examples-v0.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 --split-seed tenhou-500-v0 \
  --epochs 30 --learning-rate 0.05 \
  --models call_linear_v1,call_linear_v1_calibrated,call_linear_v1_weighted \
  --include-weighted --call-positive-weight 1.0 \
  --example-limit 20000 \
  --example-limit-strategy balanced --call-threshold-source train-best \
  --profile-stages \
  --example-cache runs/call-examples-tenhou-500-v1.json \
  --feature-cache runs/call-features-tenhou-500-balanced-limit20000-v1.json \
  --report runs/call-benchmark-tenhou-500-balanced-limit20000-v1-report.json

PYTHONPATH=src python3.13 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 --split-seed tenhou-500-v2 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v2-report.json
```

## Prioritized Next Work

1. Use the call example cache for large-slice call/pass comparisons, then compare calibrated versus
   positive-weight policies under matching caps and splits. Prefer `call_linear_v1_calibrated`
   with `--call-threshold-source train-best` unless matching reports show another policy wins on
   balanced accuracy without an unacceptable pass/call recall tradeoff.
2. Keep `riichi_linear_calibrated` with train-best thresholds as the baseline. Fixed threshold
   `0.25` improves riichi recall but is not the best balanced policy on the current 500-log splits.
3. Use decision snapshot prediction producers for external-baseline protocol work; do not import
   or copy AGPL baseline code.
4. Use discard disagreement tags before adding another feature profile.
5. Use `benchmark-discard-mlp`, `benchmark-discard-transformer`, and `benchmark-report-summary` on
   comparable ignored Tenhou slices before claiming behavior-cloned transformer progress. Use
   `benchmark-dashboard` to publish a static offline benchmark page with model/version, dataset
   slice, metric definitions, latest eval scores, and report/checkpoint artifact links. The
   dashboard intentionally omits live ladder rank tracking unless explicit platform permission is
   available.
6. Broaden deal-in calibration beyond the current 130-log local smoke: compare multiple seeds,
   larger ignored slices, and active-riichi-only variants before using it as a production defense
   probability model.

## License

MIT. See `LICENSE`.
