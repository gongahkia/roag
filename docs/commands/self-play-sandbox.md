# `self-play-sandbox`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run deterministic offline draw/discard sandbox episodes.

## Inputs

No replay input; synthetic sandbox state is seeded locally.

## Outputs

Text or JSON report, plus optional `--report` artifact.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku self-play-sandbox --episodes 2 --max-turns 32 --policy drawn --report runs/self-play-sandbox.json --json
```

## Gotchas

- The sandbox is not a complete Tenhou simulator.
- Use fixed `--seed` values for comparable runs.

## Help

```text
usage: kenjaku self-play-sandbox [-h] [--episodes EPISODES]
                                 [--max-turns MAX_TURNS] [--seed SEED]
                                 [--policy {random,drawn,frequency}]
                                 [--ruleset {tenhou-4p,tenhou-3p}]
                                 [--reward-mode {terminal,point-delta,normalized-point-delta,placement-delta}]
                                 [--include-trajectories] [--stop-on-tsumo]
                                 [--report REPORT] [--json]

options:
  -h, --help            show this help message and exit
  --episodes EPISODES   number of sandbox episodes to simulate
  --max-turns MAX_TURNS
                        maximum draw/discard turns per episode
  --seed SEED           stable seed for deterministic sandbox episodes
  --policy {random,drawn,frequency}
                        sandbox discard policy
  --ruleset {tenhou-4p,tenhou-3p}
                        sandbox static tile set and player count
  --reward-mode {terminal,point-delta,normalized-point-delta,placement-delta}
                        reward vector to expose as the selected sandbox reward
  --include-trajectories
                        include full synthetic draw/discard trajectories in
                        JSON output
  --stop-on-tsumo       stop an episode on basic closed-hand tsumo shape
                        detection
  --report REPORT       optional path for a JSON sandbox report artifact
  --json                emit the sandbox report as JSON instead of text
```
