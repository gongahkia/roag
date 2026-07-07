# How Kenjaku Differs

Reviewed: 2026-07-07.

Kenjaku is an offline research and replay-analysis toolkit. It is not a live-play bot, a bundled
strong policy, or a source of private replay data. The table below compares that scope with adjacent
riichi Mahjong AI, review, and simulator projects.

`Not verified` means the reviewed upstream repository/API did not expose a standard license signal;
it does not mean the project is free to reuse.

| Project | Scope | License | Target user | Dataset | Offline vs live | GPU story | ML frameworks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Kenjaku | Local Tenhou XML parsing, decision reconstruction, neutral decision snapshots, baseline training, sandbox experiments, and public-safe replay artifacts. | MIT. | Researchers who need auditable local tooling, compliant data boundaries, and reproducible baseline reports. | Bundled fixtures and synthetic fixtures only; users provide permitted local logs. No raw private replay data, model weights, or live ladder data are bundled. | Offline/local by design; live ladder automation and live fetching are out of scope without explicit platform permission. | CPU-first; PyTorch commands can use accelerator devices if the local PyTorch install supports them. | PyTorch for MLP/transformer experiments plus dependency-free frequency, linear, and PPO smoke paths. |
| [Mortal](https://github.com/Equim-chan/Mortal) | Fast, strong riichi Mahjong AI with a Rust simulator, Python policy code, mjai interface, and documented Docker inference path. | Code: AGPL-3.0-or-later; assets have separate CC BY-SA terms. | Users who want a strong engine/policy, mjai-compatible inference, or an external baseline. | Repository/docs do not bundle trained model files; Docker inference expects model files supplied separately. | Offline mjai inference is documented; keep Kenjaku comparisons behind subprocess/data files, not imported AGPL code. | Upstream Docker quick start is inference-only; model training/inference runtime is external to Kenjaku. | Rust `libriichi` plus Python/PyTorch policy modules. |
| [kanachan](https://github.com/Cryolite/kanachan) | Mahjong Soul-rule AI framework with annotation tooling, simulation components, and training/prediction programs. | Not verified; GitHub repo metadata reports no license and the license API returned 404 during review. | Researchers training large Mahjong Soul models from their own records. | No crawler, training data, or trained models are provided; users collect Mahjong Soul WebSocket records and convert them to annotations. | Offline training/inference over collected records; no bundled live crawler. | Users supply compute resources for large-model training. | Python/C++ with PyTorch training modules. |
| [mjai-reviewer](https://github.com/Equim-chan/mjai-reviewer) | Review tool/web app for Mahjong logs using mjai-compatible engines such as Mortal and akochan. | Apache-2.0. | Players and reviewers who want engine-assisted log review. | Consumes Mahjong Soul, Tenhou URL/local log inputs, and external engine outputs. | Web app and local CLI review logs; it is review tooling, not a Kenjaku live agent. | GPU needs are engine-dependent; the reviewer itself is not an ML trainer. | Rust review app; external engines provide policy inference. |
| [akochan](https://github.com/critter-mj/akochan) | C++ Japanese Mahjong AI engine with self-match and mjai-related entry points. | Custom Japanese terms, not a standard SPDX license; separate license review required before reuse. | Engine users and baseline experimenters who can run it as an external process. | Repository includes its own engine assets/configs; treat outputs and assets as externally licensed. | Local engine/self-match usage; Kenjaku should compare through subprocess/data boundaries only. | No GPU path verified in reviewed upstream docs. | C++ engine code; no standard ML framework verified from reviewed docs. |
| [Mjx](https://github.com/mjx-project/mjx) | Japanese Mahjong simulator/game server for AI research with gym-like API, Tenhou compatibility claims, gRPC distribution, and mjai compatibility. | MIT. | RL/evaluation researchers who need a simulator/server around custom agents. | Simulator/self-play and evaluation workflows; upstream says Tenhou compatibility was validated with many Tenhou logs. | Local/distributed simulator server; README currently says the build is broken and APIs may change. | Distributed RL/evaluation via gRPC; no bundled neural-network/GPU framework. | C++/Python simulator; user agents can be implemented in any language via gRPC. |
| [MahJax](https://github.com/nissymori/mahjax) | GPU-accelerated Mahjong simulator for reinforcement learning in JAX, with vectorized environments, visualization, web UI, and BC/PPO examples. | Apache-2.0. | RL researchers who need high-throughput vectorized simulation. | Self-play/vectorized environments; upstream validates rules against downloaded Tenhou logs and ships examples, not Kenjaku-style replay-intake tooling. | Offline simulator plus local browser UI for playing against built-in/custom agents. | GPU-centered JAX design; upstream reports about 1M+ steps/sec for red Mahjong on 8x A100 GPUs. | JAX/JAXLIB; examples include behavior cloning and PPO. |

## Practical Boundary

Kenjaku should interoperate with strong engines through neutral decision snapshots and subprocess
prediction files. Do not copy external engine code, model weights, raw replay data, or license-bound
assets into this repository without a separate review.

## Sources

- Kenjaku: `README.md`, `LICENSE`, `pyproject.toml`, `docs/external-baselines.md`.
- Mortal: <https://github.com/Equim-chan/Mortal>,
  <https://mortal.ekyu.moe/user/docker.html>,
  <https://raw.githubusercontent.com/Equim-chan/Mortal/main/mortal/model.py>.
- kanachan: <https://github.com/Cryolite/kanachan>,
  <https://api.github.com/repos/Cryolite/kanachan>.
- mjai-reviewer: <https://github.com/Equim-chan/mjai-reviewer>.
- akochan: <https://github.com/critter-mj/akochan>,
  <https://github.com/critter-mj/akochan/blob/master/LICENSE>.
- Mjx: <https://github.com/mjx-project/mjx>.
- MahJax: <https://github.com/nissymori/mahjax>.
