# Cloud GPU Dry Run

TODO-003 requires an actual cloud GPU smoke job. Do not mark it complete from a local CPU run.

## Required Evidence

Record all of the following after the cloud job finishes:

- Provider and region.
- GPU type.
- Wall-clock runtime.
- Billed runtime and cost estimate.
- Exact training command.
- Ignored input slice path used by the training report.
- Ignored artifact paths for the JSON report and checkpoint.
- `benchmark-report-summary` output for the JSON report when supported.
- `git status --short` output showing no model weights, raw data, or generated reports staged.

Record the cloud run in an ignored evidence JSON file:

```json
{
  "kind": "kenjaku-cloud-gpu-dry-run-evidence-v0",
  "provider": {
    "name": "PROVIDER",
    "region": "REGION",
    "gpu_type": "NVIDIA GPU TYPE"
  },
  "runtime": {
    "wall_clock_seconds": 0,
    "billed_seconds": 0,
    "cost_usd": 0
  },
  "command": "python -m kenjaku train-discard-transformer ...",
  "artifacts": {
    "report_path": "runs/todo-003/cloud-fixture-discard-transformer.json",
    "checkpoint_path": "runs/todo-003/cloud-fixture-discard-transformer.pt"
  },
  "benchmark_report_summary": "paste benchmark-report-summary output",
  "git_status_short": "paste git status --short output"
}
```

Validate the evidence before closing TODO-003:

```bash
python3 scripts/validate_cloud_gpu_evidence.py runs/todo-003/cloud-gpu-evidence.json
```

The validator requires a CUDA-backed transformer report, matching command and checkpoint path,
positive runtime, cost metadata, ignored input/report/checkpoint paths, and no checked-in fixture
input paths. It rejects local CPU/MPS fallback reports.

Recommended tiny smoke setup copies checked-in fixtures into an ignored cloud input slice first, so
the closure evidence proves the same raw-data hygiene path used by larger runs:

```bash
mkdir -p runs/todo-003/cloud-input-slice
cp data/fixtures/tenhou/*.xml runs/todo-003/cloud-input-slice/

PYTHONPATH=src python -m kenjaku train-discard-transformer \
  runs/todo-003/cloud-input-slice \
  --epochs 1 --batch-size 2 --device auto \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --checkpoint runs/todo-003/cloud-fixture-discard-transformer.pt \
  --report runs/todo-003/cloud-fixture-discard-transformer.json \
  --source-label PROVIDER-GPU-fixture-smoke \
  --source-date YYYY-MM-DD \
  --source-command "python -m kenjaku train-discard-transformer runs/todo-003/cloud-input-slice --epochs 1 --batch-size 2 --device auto --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 --checkpoint runs/todo-003/cloud-fixture-discard-transformer.pt --report runs/todo-003/cloud-fixture-discard-transformer.json"
```

Use `--device auto` for the cloud and local runs so the same command path selects CUDA on the GPU
machine and falls back locally.

## Local Fallback Smoke

Local fallback was first run on 2026-06-13 SGT in this workspace after creating an ignored `.venv`
with PyTorch. A fresh local smoke on 2026-07-08 SGT found no CUDA backend but did find MPS.

```bash
uv run --python /usr/bin/python3.11 python -c \
  'import torch; print("cuda_available", torch.cuda.is_available()); print("mps_available", hasattr(torch.backends, "mps") and torch.backends.mps.is_available())'
```

Observed output:

```text
cuda_available False
mps_available True
```

Current fallback training command:

```bash
/usr/bin/time -p env PYTHONPATH=src python3 -m kenjaku train-discard-transformer \
  data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --device auto \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --checkpoint runs/todo-003/local-fixture-discard-transformer-2026-07-08.pt \
  --report runs/todo-003/local-fixture-discard-transformer-2026-07-08.json \
  --source-label fixture-local-auto-device-2026-07-08 \
  --source-date 2026-07-08 \
  --source-command "python -m kenjaku train-discard-transformer data/fixtures/tenhou --epochs 1 --batch-size 2 --device auto --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 --checkpoint runs/todo-003/local-fixture-discard-transformer-2026-07-08.pt --report runs/todo-003/local-fixture-discard-transformer-2026-07-08.json"
```

Observed output:

```text
examples: 4
train_examples: 3
eval_examples: 1
device: mps
model: discard-transformer-policy-v0
encoder: mahjong-transformer-encoder-v0
input_tokens: 152
train_accuracy: 0.0000
eval_accuracy: 0.0000
checkpoint_path: runs/todo-003/local-fixture-discard-transformer-2026-07-08.pt
report_path: runs/todo-003/local-fixture-discard-transformer-2026-07-08.json
real 7.57
```

The local report path and checkpoint path are ignored. `benchmark-report-summary` read the report
successfully and showed 4 total examples, 3 train examples, 1 eval example, device `mps`, and
eval accuracy `0.0000`. This proves the local fallback side of TODO-003, not the required cloud GPU
side.

## Current Blocker

As of 2026-07-08 SGT, this environment had no `runpodctl`, Lambda Cloud CLI, `aws`, `gcloud`, `az`,
or `nvidia-smi` command on `PATH`, and no `RUNPOD_API_KEY`, `LAMBDA_API_KEY`, `AWS_ACCESS_KEY_ID`,
`GOOGLE_APPLICATION_CREDENTIALS`, or `AZURE_CLIENT_ID` in the environment. A cloud GPU run still
requires provider credentials or an already-provisioned GPU shell.
