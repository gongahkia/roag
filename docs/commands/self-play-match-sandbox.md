# `self-play-match-sandbox`

Generated from `argparse` help and reviewed on 2026-07-07.

## Purpose

Run deterministic multi-round sandbox matches to a final placement result.

## Inputs

No replay input; match state is generated from the selected ruleset and seed.

## Outputs

Text or JSON match report, plus optional embedded trajectory data and flattened trajectory JSONL.

## Example

```bash
PYTHONPATH=src python3.13 -m kenjaku self-play-match-sandbox --games 2 --ruleset tenhou-4p --report runs/self-play-match.json --trajectory-jsonl runs/self-play-match-trajectory.jsonl --json
```

## Gotchas

- `--include-trajectories` can make reports much larger.
- `--trajectory-jsonl` forces trajectory capture and writes one turn row per line.
- Policy choices are simple sandbox policies, not trained agents.

## Help

```text
usage: kenjaku self-play-match-sandbox [-h] [--games GAMES]
                                       [--max-rounds MAX_ROUNDS]
                                       [--max-turns-per-round MAX_TURNS_PER_ROUND]
                                       [--seed SEED]
                                       [--ruleset {tenhou-4p,tenhou-3p}]
                                       [--discard-policy {random,drawn,frequency}]
                                       [--call-policy {pass,first,random}]
                                       [--riichi-policy {pass,first,random}]
                                       [--kan-policy {pass,first,random}]
                                       [--kita-policy {pass,first,random}]
                                       [--ron-policy {pass,win,first,random}]
                                       [--include-trajectories]
                                       [--trajectory-jsonl TRAJECTORY_JSONL]
                                       [--report REPORT] [--json]

options:
  -h, --help            show this help message and exit
  --games GAMES         number of sandbox matches to simulate
  --max-rounds MAX_ROUNDS
                        maximum hands per match before reporting an incomplete
                        match
  --max-turns-per-round MAX_TURNS_PER_ROUND
                        maximum action decisions per hand before max-turn
                        termination
  --seed SEED           stable seed for deterministic sandbox matches
  --ruleset {tenhou-4p,tenhou-3p}
                        sandbox static tile set and player count
  --discard-policy {random,drawn,frequency}
                        discard policy used by every seat
  --call-policy {pass,first,random}
                        call policy used during pending discard reactions
  --riichi-policy {pass,first,random}
                        riichi declaration policy used on self turns
  --kan-policy {pass,first,random}
                        kan policy used on self turns
  --kita-policy {pass,first,random}
                        Sanma Kita policy used on self turns
  --ron-policy {pass,win,first,random}
                        ron/tsumo win policy
  --include-trajectories
                        include state/action/reward trajectories in JSON
                        output
  --trajectory-jsonl TRAJECTORY_JSONL
                        optional JSONL output for flattened match trajectory
                        rows
  --report REPORT       optional path for a JSON match report artifact
  --json                emit the match report as JSON instead of text
```
