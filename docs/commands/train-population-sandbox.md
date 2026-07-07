# `train-population-sandbox`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Exercise population-pool PPO snapshot training and promotion plumbing.

## Inputs

Synthetic sandbox PPO rollouts and sampled pool matchups.

## Outputs

Optional population report and checkpoint directory.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-population-sandbox --generations 1 --total-steps 128 --output-dir runs/population --report runs/population.json --json
```

## Gotchas

- Keep `--pool-size` at least 4 unless deliberately testing validation failures.
- Artifacts are experiment outputs and should remain ignored.

## Help

```text
usage: kenjaku train-population-sandbox [-h] [--pool-size POOL_SIZE]
                                        [--generations GENERATIONS]
                                        [--candidates-per-generation CANDIDATES_PER_GENERATION]
                                        [--matchups-per-candidate MATCHUPS_PER_CANDIDATE]
                                        [--total-steps TOTAL_STEPS]
                                        [--max-rounds MAX_ROUNDS]
                                        [--max-turns-per-round MAX_TURNS_PER_ROUND]
                                        [--seed SEED]
                                        [--ruleset {tenhou-4p,tenhou-3p}]
                                        [--ppo-epochs PPO_EPOCHS]
                                        [--batch-size BATCH_SIZE]
                                        [--learning-rate LEARNING_RATE]
                                        [--hidden-dim HIDDEN_DIM]
                                        [--evaluation-games EVALUATION_GAMES]
                                        [--evaluation-max-rounds EVALUATION_MAX_ROUNDS]
                                        [--evaluation-max-turns-per-round EVALUATION_MAX_TURNS_PER_ROUND]
                                        [--promotion-margin PROMOTION_MARGIN]
                                        [--output-dir OUTPUT_DIR]
                                        [--report REPORT] [--json]

options:
  -h, --help            show this help message and exit
  --pool-size POOL_SIZE
                        number of active policy snapshots to maintain
  --generations GENERATIONS
                        generations to run
  --candidates-per-generation CANDIDATES_PER_GENERATION
                        candidate snapshots trained per generation
  --matchups-per-candidate MATCHUPS_PER_CANDIDATE
                        sampled pool matchups per candidate or initial
                        snapshot
  --total-steps TOTAL_STEPS
                        minimum environment decisions per trained snapshot
  --max-rounds MAX_ROUNDS
                        maximum hands per PPO training rollout
  --max-turns-per-round MAX_TURNS_PER_ROUND
                        maximum action decisions per PPO training hand
  --seed SEED           stable seed for deterministic population training
  --ruleset {tenhou-4p,tenhou-3p}
                        sandbox static tile set and player count
  --ppo-epochs PPO_EPOCHS
                        PPO epochs per snapshot rollout
  --batch-size BATCH_SIZE
                        PPO mini-batch size
  --learning-rate LEARNING_RATE
                        manual SGD learning rate
  --hidden-dim HIDDEN_DIM
                        reserved model width
  --evaluation-games EVALUATION_GAMES
                        sandbox games per sampled matchup
  --evaluation-max-rounds EVALUATION_MAX_ROUNDS
                        maximum hands per sampled matchup; defaults to --max-
                        rounds
  --evaluation-max-turns-per-round EVALUATION_MAX_TURNS_PER_ROUND
                        maximum decisions per matchup hand; defaults to --max-
                        turns-per-round
  --promotion-margin PROMOTION_MARGIN
                        average-score margin required to replace the pool
                        floor
  --output-dir OUTPUT_DIR
                        optional directory for population PPO checkpoint
                        artifacts
  --report REPORT       optional path for a JSON population report artifact
  --json                emit the population report as JSON instead of text
```
