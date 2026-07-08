# Kenjaku Command Runbooks

Generated and reviewed on 2026-07-07 from the current `argparse` command set.

The CLI currently exposes 50 subcommands. Issue #57 mentioned 35 commands; this index
covers all 50 commands present in `kenjaku --help` so the docs match the implemented CLI.

Root options apply before the subcommand; use `--global-seed SEED` to seed Python, NumPy,
and PyTorch RNGs before command execution.

- [`status`](status.md) - Print the implemented feature matrix and local environment status.
- [`repro-report`](repro-report.md) - Verify report provenance against the current checkout.
- [`demo`](demo.md) - Run the fixture quickstart and write a linked local artifact landing page.
- [`browser-demo`](browser-demo.md) - Generate the static browser-playable demo and optionally serve it locally.
- [`serve`](serve.md) - Serve a local artifact directory with an auto-generated landing page.
- [`replay-intake-review`](replay-intake-review.md) - Validate a permission-aware replay intake manifest before local analysis.
- [`replay-share-plan`](replay-share-plan.md) - Plan whether accepted replay intake rows can be used for a demo or redistribution.
- [`replay-public-summary`](replay-public-summary.md) - Build a public-safe replay summary from accepted replay intake rows.
- [`replay-viewer`](replay-viewer.md) - Render self-play trajectory JSONL as a turn-by-turn HTML viewer.
- [`self-play-sandbox`](self-play-sandbox.md) - Run deterministic offline draw/discard sandbox episodes.
- [`self-play-match-sandbox`](self-play-match-sandbox.md) - Run deterministic multi-round sandbox matches to a final placement result.
- [`train-ppo-sandbox`](train-ppo-sandbox.md) - Run a fixture-scale PyTorch PPO smoke trainer over sandbox trajectories.
- [`train-population-sandbox`](train-population-sandbox.md) - Exercise population-pool PPO snapshot training and promotion plumbing.
- [`training-dashboard`](training-dashboard.md) - Build a static dashboard from training metrics JSONL files.
- [`inspect-tenhou`](inspect-tenhou.md) - Parse Tenhou XML inputs and print dataset-level counts.
- [`tenhou-to-mjai`](tenhou-to-mjai.md) - Convert Tenhou XML files into MJAI JSONL replay streams.
- [`bot`](bot.md) - Run a stdin/stdout MJAI bot adapter.
- [`defense-risk-summary`](defense-risk-summary.md) - Summarize heuristic discard danger scores from Tenhou XML decisions.
- [`safety-advisor`](safety-advisor.md) - Rank arbitrary hand tiles by riichi-defense safety signals.
- [`benchmark-deal-in`](benchmark-deal-in.md) - Train and evaluate a small direct ron-discard probability estimator.
- [`train-placement`](train-placement.md) - Train a sandbox final-placement probability estimator.
- [`placement-probability`](placement-probability.md) - Estimate final-placement probabilities from current scores and round state.
- [`analyze-hand`](analyze-hand.md) - Rank discard candidates for one hand position.
- [`export-decision-snapshots`](export-decision-snapshots.md) - Export neutral decision snapshots for shared offline prediction/evaluation protocols.
- [`decision-snapshot-summary`](decision-snapshot-summary.md) - Summarize one or more decision snapshot JSONL exports.
- [`interpretability-overlay`](interpretability-overlay.md) - Render a discard interpretability HTML page from decision snapshots.
- [`review-game`](review-game.md) - Render a per-player offline review HTML report from one Tenhou XML.
- [`transformer-attention-overlay`](transformer-attention-overlay.md) - Render transformer attention heatmaps from a discard transformer checkpoint.
- [`feature-importance`](feature-importance.md) - Rank linear-model features from a benchmark report.
- [`produce-decision-predictions`](produce-decision-predictions.md) - Write stub prediction rows for decision snapshot protocol tests.
- [`predict`](predict.md) - Run batch model inference over decision snapshots.
- [`decision-snapshot-compare`](decision-snapshot-compare.md) - Compare neutral snapshots against prediction JSONL keyed by `row_id`.
- [`external-baseline-report`](external-baseline-report.md) - Build a shared-log report across named external baseline prediction files.
- [`run-external-prediction-producer`](run-external-prediction-producer.md) - Run a separate process that converts snapshots into prediction JSONL.
- [`train-discard-baseline`](train-discard-baseline.md) - Fit and print the deterministic discard frequency baseline.
- [`train-discard-linear`](train-discard-linear.md) - Train a dependency-free linear discard model on reconstructed examples.
- [`train-discard-mlp`](train-discard-mlp.md) - Train a small PyTorch masked-logit discard MLP.
- [`train-discard-transformer`](train-discard-transformer.md) - Train a PyTorch transformer masked-logit discard policy.
- [`export-bc-examples`](export-bc-examples.md) - Stream Tenhou XML into behavior-cloning JSONL shards.
- [`benchmark-discard-from-examples`](benchmark-discard-from-examples.md) - Benchmark discard baselines from exported BC JSONL shards or manifests.
- [`benchmark-discard`](benchmark-discard.md) - Compare deterministic discard baselines on one train/eval split.
- [`benchmark-discard-mlp`](benchmark-discard-mlp.md) - Compare a PyTorch discard MLP against discard baseline anchors.
- [`benchmark-discard-transformer`](benchmark-discard-transformer.md) - Compare a PyTorch discard transformer against discard baseline anchors.
- [`benchmark-report-summary`](benchmark-report-summary.md) - Summarize one or more benchmark JSON reports.
- [`benchmark-dashboard`](benchmark-dashboard.md) - Build a static public dashboard from benchmark JSON reports.
- [`disagreement-report-summary`](disagreement-report-summary.md) - Summarize discard model-disagreement JSON reports.
- [`benchmark-call`](benchmark-call.md) - Compare deterministic call/pass baselines on one train/eval split.
- [`benchmark-call-from-examples`](benchmark-call-from-examples.md) - Benchmark call/pass baselines from exported BC JSONL shards.
- [`benchmark-riichi`](benchmark-riichi.md) - Compare deterministic riichi/pass baselines on one train/eval split.
- [`benchmark-riichi-from-examples`](benchmark-riichi-from-examples.md) - Benchmark riichi/pass baselines from exported BC JSONL shards.
