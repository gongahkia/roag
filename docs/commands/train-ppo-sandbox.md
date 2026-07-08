# `train-ppo-sandbox`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run a fixture-scale PyTorch PPO smoke trainer over sandbox trajectories.

## Inputs

Synthetic sandbox rollouts generated during the run.

## Outputs

Optional JSON report and checkpoint artifact.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku train-ppo-sandbox --total-steps 128 --rollout-games 1 --report runs/ppo-sandbox.json --checkpoint runs/ppo-sandbox.json.ckpt --json
```

## Gotchas

- This validates PPO plumbing, not production-strength RL.
- Resume only from checkpoints produced by this command.

## Help

```text
usage: kenjaku train-ppo-sandbox [-h] [--total-steps TOTAL_STEPS]
                                 [--rollout-games ROLLOUT_GAMES]
                                 [--max-rounds MAX_ROUNDS]
                                 [--max-turns-per-round MAX_TURNS_PER_ROUND]
                                 [--seed SEED]
                                 [--ruleset {tenhou-4p,tenhou-3p}]
                                 [--rollout-discard-policy {random,drawn,frequency}]
                                 [--rollout-call-policy {pass,first,random}]
                                 [--rollout-riichi-policy {pass,first,random}]
                                 [--rollout-kan-policy {pass,first,random}]
                                 [--rollout-kita-policy {pass,first,random}]
                                 [--rollout-ron-policy {pass,win,first,random}]
                                 [--ppo-epochs PPO_EPOCHS]
                                 [--batch-size BATCH_SIZE]
                                 [--learning-rate LEARNING_RATE]
                                 [--hidden-dim HIDDEN_DIM] [--gamma GAMMA]
                                 [--gae-lambda GAE_LAMBDA]
                                 [--clip-epsilon CLIP_EPSILON]
                                 [--entropy-coef ENTROPY_COEF]
                                 [--value-coef VALUE_COEF]
                                 [--max-grad-norm MAX_GRAD_NORM]
                                 [--reward-scale REWARD_SCALE]
                                 [--supervised-warmup-epochs SUPERVISED_WARMUP_EPOCHS]
                                 [--model-seed TORCH_SEED]
                                 [--device {auto,cpu,cuda,mps}]
                                 [--checkpoint CHECKPOINT] [--resume RESUME]
                                 [--report REPORT] [--json]

options:
  -h, --help            show this help message and exit
  --total-steps TOTAL_STEPS
                        minimum additional environment decisions to train on
  --rollout-games ROLLOUT_GAMES
                        sandbox matches collected for each PPO update
  --max-rounds MAX_ROUNDS
                        maximum hands per collected sandbox match
  --max-turns-per-round MAX_TURNS_PER_ROUND
                        maximum action decisions per collected hand
  --seed SEED           stable seed for deterministic PPO rollouts
  --ruleset {tenhou-4p,tenhou-3p}
                        sandbox static tile set and player count
  --rollout-discard-policy {random,drawn,frequency}
                        discard policy used to collect PPO trajectories
  --rollout-call-policy {pass,first,random}
                        call policy used to collect PPO trajectories
  --rollout-riichi-policy {pass,first,random}
                        riichi policy used to collect PPO trajectories
  --rollout-kan-policy {pass,first,random}
                        kan policy used to collect PPO trajectories
  --rollout-kita-policy {pass,first,random}
                        Kita policy used to collect PPO trajectories
  --rollout-ron-policy {pass,win,first,random}
                        ron/tsumo policy used to collect PPO trajectories
  --ppo-epochs PPO_EPOCHS
                        PPO epochs per rollout
  --batch-size BATCH_SIZE
                        PPO mini-batch size
  --learning-rate LEARNING_RATE
                        Torch SGD learning rate
  --hidden-dim HIDDEN_DIM
                        reserved model width
  --gamma GAMMA         discount factor
  --gae-lambda GAE_LAMBDA
                        GAE lambda
  --clip-epsilon CLIP_EPSILON
                        PPO probability-ratio clipping epsilon
  --entropy-coef ENTROPY_COEF
                        entropy regularization coefficient
  --value-coef VALUE_COEF
                        value loss coefficient
  --max-grad-norm MAX_GRAD_NORM
                        gradient clipping norm
  --reward-scale REWARD_SCALE
                        divide final-score rewards by this value
  --supervised-warmup-epochs SUPERVISED_WARMUP_EPOCHS
                        optional behavior-cloning warmup epochs on collected
                        rollout actions
  --model-seed, --torch-seed TORCH_SEED
                        model initialization and mini-batch random seed
  --device {auto,cpu,cuda,mps}
                        Torch device for PPO training
  --checkpoint CHECKPOINT
                        optional path for the PPO checkpoint artifact
  --resume RESUME       optional PPO checkpoint path to resume from
  --report REPORT       optional path for a JSON PPO report artifact
  --json                emit the PPO report as JSON instead of text
```
