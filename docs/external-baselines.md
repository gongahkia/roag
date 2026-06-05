# External Baselines

This file records local-only baseline reconnaissance. Do not commit cloned upstream repositories,
downloaded model weights, generated simulator logs, or copied dynamic libraries.

## Mortal

Local checkout status on 2026-06-05:

- Repo: `https://github.com/Equim-chan/Mortal`
- Ignored checkout path: `data/raw/external/mortal`
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`
- License note: code is AGPL-3.0-or-later; keep integration boundaries explicit before reusing code.
- Upstream docs describe Mortal as a Rust `libriichi` simulator plus Python policy code with an
  `mjai` interface and 1v3 duplicate-mahjong evaluation.
- Trained weights are not part of the repo; config files expect local `*.pth` state files.

Local build checks that passed:

```bash
git clone --depth 1 https://github.com/Equim-chan/Mortal data/raw/external/mortal
cd data/raw/external/mortal
cargo build -p libriichi --lib --release
cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture
cp target/release/libriichi.dylib mortal/libriichi.so
PYTHONPATH=mortal python3 -c "import libriichi; print('ok')"
```

Observed local result:

- `cargo build -p libriichi --lib --release` finished successfully in 37.70s.
- `target/release/libriichi.dylib` was produced at about 3.1 MB on macOS.
- Rust tests passed: `28 passed; 0 failed`, plus doctests reported OK.
- `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded after copying the dylib to
  `mortal/libriichi.so`.

Practical comparison path:

1. Keep Mortal as an external, ignored baseline until Kenjaku has a policy that can act through a
   common interface.
2. Prefer a neutral decision/export layer over copying AGPL simulator code into Kenjaku.
3. Add a Tenhou XML to `mjai` decision snapshot exporter, then compare Kenjaku decisions against a
   Mortal-compatible inference path only when model weights are available and legally usable.
4. For head-to-head evaluation, target duplicate-mahjong offline runs through a common simulator or
   a subprocess boundary; do not add live ladder automation.
