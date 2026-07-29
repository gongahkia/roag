# `bot`

Generated from `argparse` help and reviewed on 2026-07-08.

## Purpose

Run a stdin/stdout MJAI bot adapter for local smoke tests and Docker submission wrapping.

## Inputs

MJAI JSON objects or legacy MJAI event arrays on stdin.

## Outputs

MJAI action JSONL on stdout.

## Example

```bash
printf '%s\n' '{"type":"request_action","request_id":1,"possible_actions":[{"type":"none"}]}' \
  | PYTHONPATH=src python3.13 -m kenjaku bot --policy frequency --player-id 0
```

## Gotchas

- The adapter only chooses among server-provided `possible_actions` for `request_action`.
- Legacy event-array input falls back to discard-on-own-tsumo and `none` otherwise.
- `mlp` and `transformer` checkpoints require the `ml` extra.

## Help

```text
usage: kenjaku bot [-h] [--policy POLICY]
                   [--policy-type {auto,frequency,linear-discard,mlp,mlp-discard,transformer,transformer-discard}]
                   --player-id PLAYER_ID [--device DEVICE]

options:
  -h, --help            show this help message and exit
  --policy POLICY       policy name or checkpoint path; defaults to frequency
  --policy-type {auto,frequency,linear-discard,mlp,mlp-discard,transformer,transformer-discard}
                        checkpoint family; auto detects paths
  --player-id PLAYER_ID
                        MJAI seat id, 0 through 3
  --device DEVICE       PyTorch device for mlp or transformer policies
```
