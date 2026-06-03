# Data Directory

This directory is for local experiment data. Raw, interim, and processed datasets are ignored by
git on purpose.

Do not commit:

- Raw Tenhou log archives or downloaded `mjlog` XML.
- Mirrored Mahjong Soul logs or account-derived private data.
- Processed datasets that can reconstruct restricted source logs.
- Model checkpoints trained on restricted data unless the release terms are reviewed first.

Allowed fixtures:

- Small synthetic XML/JSON examples created specifically for tests.
- Tiny hand-written parser fixtures that do not come from real player logs.
- Metadata-only manifests that describe how to reproduce a dataset locally.

Recommended local layout:

```text
data/
  raw/        # local downloads, never committed
  interim/    # parser outputs, never committed
  processed/  # training-ready local datasets, never committed
  fixtures/   # synthetic or legally safe test fixtures
```

For Tenhou Phoenix/Houou data, use `houou-logs` as an external tool and respect its constraints:
one download session at a time, no redistribution of downloaded logs, and no public mirror service.
The local evaluation workflow lives in `docs/local-tenhou-eval.md`.
