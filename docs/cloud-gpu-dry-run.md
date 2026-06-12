# Cloud GPU Dry Run

TODO-003 requires an actual cloud GPU smoke job. Do not mark it complete from a local CPU run.

## Required Evidence

Record all of the following after the cloud job finishes:

- Provider and region.
- GPU type.
- Wall-clock runtime.
- Billed runtime and cost estimate.
- Exact training command.
- Ignored artifact paths for the JSON report and checkpoint.
- `benchmark-report-summary` output for the JSON report when supported.
- `git status --short` output showing no model weights, raw data, or generated reports staged.

Recommended tiny smoke command:

```bash
PYTHONPATH=src python -m kenjaku train-discard-transformer \
  data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --device auto \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --checkpoint runs/todo-003/cloud-fixture-discard-transformer.pt \
  --report runs/todo-003/cloud-fixture-discard-transformer.json \
  --source-label PROVIDER-GPU-fixture-smoke \
  --source-date YYYY-MM-DD \
  --source-command "python -m kenjaku train-discard-transformer data/fixtures/tenhou --epochs 1 --batch-size 2 --device auto --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 --checkpoint runs/todo-003/cloud-fixture-discard-transformer.pt --report runs/todo-003/cloud-fixture-discard-transformer.json"
```

Use `--device auto` for the cloud and local runs so the same command path selects CUDA on the GPU
machine and falls back locally.

## Local Fallback Smoke

Local fallback was run on 2026-06-13 SGT in this workspace after creating an ignored `.venv` with
PyTorch. There was no local CUDA or MPS backend available.

```bash
uv run --python /usr/bin/python3.11 python -c \
  'import torch; print("cuda_available", torch.cuda.is_available()); print("mps_available", hasattr(torch.backends, "mps") and torch.backends.mps.is_available())'
```

Observed output:

```text
cuda_available False
mps_available False
```

Fallback training command:

```bash
/usr/bin/time -f 'wall_clock_seconds: %e' \
  uv run --python /usr/bin/python3.11 python -m kenjaku train-discard-transformer \
  data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --device auto \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --checkpoint runs/todo-003/local-fixture-discard-transformer.pt \
  --report runs/todo-003/local-fixture-discard-transformer.json \
  --source-label fixture-local-auto-device \
  --source-date 2026-06-13 \
  --source-command "python -m kenjaku train-discard-transformer data/fixtures/tenhou --epochs 1 --batch-size 2 --device auto --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 --checkpoint runs/todo-003/local-fixture-discard-transformer.pt --report runs/todo-003/local-fixture-discard-transformer.json"
```

Observed output:

```text
examples: 4
train_examples: 3
eval_examples: 1
device: cpu
model: discard-transformer-policy-v0
encoder: mahjong-transformer-encoder-v0
input_tokens: 152
train_accuracy: 0.0000
eval_accuracy: 0.0000
checkpoint_path: runs/todo-003/local-fixture-discard-transformer.pt
report_path: runs/todo-003/local-fixture-discard-transformer.json
wall_clock_seconds: 9.30
```

The local report path and checkpoint path are ignored. This proves the fallback side of TODO-003,
not the required cloud GPU side.

## Current Blocker

As of 2026-06-13 SGT, this environment had no `runpodctl`, Lambda Cloud CLI, `aws`, `gcloud`, or
`az` command on `PATH`, and no relevant provider API tokens in the environment. A cloud GPU run
still requires provider credentials or an already-provisioned GPU shell.
