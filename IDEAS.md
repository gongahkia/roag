# kenjaku — Open-Source Riichi Mahjong AI

> An open-source riichi mahjong research agent and analysis toolkit, with a reproducible training pipeline, strong local evaluation, interpretable recommendations, and no unauthorized public-ladder automation.

---

## 0. tl;dr

- **What**: Train and evaluate an open-source riichi mahjong AI that can be reproduced locally, benchmarked against open baselines, and used for replay/decision analysis.
- **Why now**: Microsoft Suphx is the SOTA closed-source bot (Tenhou 10-dan, 2019). NAGA is closed. The strongest open-source bot (`mortal`) reaches stable 7-8 dan but has a dated architecture (CNN+MLP), limited interpretability, no Sanma support, no live ladder presence.
- **Differentiation**: Reproducible data pipeline + modern sequence/transformer experiments + Sanma (3-player) extension + interpretability overlay + public offline evaluation artifacts.
- **Hardware**: Mac (M-series MPS) primary; ~$200-500 cloud GPU bursts (Lambda/RunPod) for final RL training passes.
- **Timeline**: 4-6 months solo part-time to credible launch.
- **Outputs**: Open-source repo, arxiv-style technical report, replay analyzer, browser-playable demo, reproducible benchmark suite, launch posts.

### 0.1 current implementation stance (2026-06-03)

- Start with a narrow Phase 0 scaffold: Python package, tests, tile/action/state primitives, compliant sample data path, and a tiny discard-prediction baseline.
- Treat Tenhou and Mahjong Soul as data/evaluation ecosystems, not as automation targets unless explicit platform permission exists.
- Avoid redistributing raw Tenhou logs. Store fixtures only when they are small, legally safe, and documented, or use synthetic fixtures for unit tests.
- Keep all experiments reproducible: deterministic config files, documented data provenance, seed handling, and benchmark reports checked into the repo.
- Prefer interoperable formats (`mjai`, Tenhou XML/JSON conversion) over custom one-off state dumps.

### 0.2 research updates that change the original plan

- Tenhou's manual has explicit AI-play guidance: Phoenix-table AI play is prohibited, higher-table AI use requires a dedicated ID, and AI play should publish replay URLs and avoid blind/high-volume behavior. Tenhou also restricts log redistribution and use outside Tenhou-related purposes. Reference: https://cdn.tenhou.net/man/
- Mahjong Soul's English terms prohibit automated systems, bots, automation software, cheats, and unauthorized third-party software that modifies or interferes with the service. This makes live ranked automation inappropriate without written permission. Reference: https://mahjongsoul.yo-star.com/terms_of_service
- `MahjongRepository/phoenix-logs` is archived; `Apricot-S/houou-logs` is the current downloader to evaluate, and it documents Tenhou's no-redistribution and one-download-session constraints. Reference: https://github.com/Apricot-S/houou-logs
- `mortal` remains the main open baseline and provides a Rust emulator, `mjai` interface, and documented duplicate-mahjong evaluation. Reference: https://github.com/Equim-chan/Mortal and https://mortal.ekyu.moe/
- `mjx` is useful background but currently warns that its build is broken and Apple Silicon is unsupported. Reference: https://github.com/mjx-project/mjx
- `Mahjax` is a new JAX simulator paper submitted on 2026-05-20, claiming GPU-vectorized rollouts up to 2M steps/sec on 8x A100. It is worth tracking before committing to a custom simulator. Reference: https://arxiv.org/abs/2605.20577
- Transformer novelty must be claimed carefully. MahjongLM-style tokenized Tenhou datasets/models already exist, so the sharper claim is an open transformer policy agent with reproducible riichi evaluation, not "first transformer mahjong AI." Reference: https://huggingface.co/datasets/mitsutani/mahjonglm-dataset

---

## 1. why this project

### the gap

- **Suphx (Microsoft, 2019)** — reached 10-dan on Tenhou, top 0.01% performance. Paper published, code never released. Used distributional RL, oracle guiding, parametric Monte Carlo policy adaptation. Closed source.
- **NAGA (Dwango, 2020)** — Japanese closed-source bot, used heavily by pros for analysis. Closed source.
- **akochan (open source, ~2016-2020)** — historical baseline. Supervised learning from Tenhou Phoenix logs. Plateaued at 6-7 dan equivalent. Older codebase, hard to extend.
- **mortal (open source, 2022-2024)** — current best open-source. Combines supervised pretraining with PPO. Reaches stable 7-8 dan on Tenhou. CNN-based feature encoder + MLP policy. Limited interpretability, 4-player only, no formal paper, no live deployment.

There is a substantial gap between the strongest closed bots (Suphx) and the strongest open bots (mortal). Closing or narrowing it with a clean, reproducible, interpretable agent is a high-prestige open-source contribution and a publishable research artifact.

### why mahjong matters as a research target

- **Imperfect information at extreme scale**: ~134 hidden tiles, multiple draws, partial observation of opponent state through discards.
- **Mixed objectives**: Offense (forming a winning hand) vs defense (folding to avoid dealing in). The defense decision is what separates strong players from average ones — a research-rich problem.
- **Long-horizon credit assignment**: A bad discard 6 turns ago can lose you a hand. Sparse reward.
- **Multi-agent imperfect-info**: 4 players, partially competitive, occasional implicit coordination (suji defense, etc.).
- **Yaku/shape strategy**: The reward structure rewards complex shape patterns (yaku combinations), not just point totals. Value estimation must integrate shape-completion probability with point yield.

This combination is why CFR-style equilibrium solving doesn't directly apply (the game tree is far too large) and why model-free RL with imitation pretraining has been the dominant approach.

### why I want to do this

- Bridges my computing + game-theory interests with a publishable research target.
- Solo-tractable on a Mac with limited cloud spend.
- Strong portfolio piece: open-source repo + arxiv paper + visible public bot.
- Mahjong has a passionate global community (Japan, China, Korea, EU, US) — built-in audience for the launch.

---

## 2. existing landscape (detailed)

### Suphx (Microsoft Research Asia, 2019)
- **Paper**: "Suphx: Mastering Mahjong with Deep Reinforcement Learning" (arXiv:2003.13590)
- **Architecture**: Resnet + value/policy heads, 5 separate models (discard, riichi, chi, pon, kan)
- **Training**: Supervised pretraining on Tenhou Phoenix logs + entropy-regularized PPO + parametric Monte Carlo policy adaptation (PMCPA) + oracle guiding
- **Results**: 10-dan on Tenhou (top ~0.01% of human players)
- **Reproducibility**: None. No code released. Tenhou access for the bot was rescinded.

### NAGA (Dwango / Suzaku)
- Japanese commercial product. Closed source.
- Used heavily for pro analysis.
- Reaches Phoenix-room level.

### akochan
- Author: critter / Hiroshi Nakagawa
- Approach: Supervised learning from Tenhou Phoenix logs, hand-crafted defense heuristics
- Codebase: C++, older
- Plateau: ~6-7 dan equivalent

### mortal
- Author: Equim Chen
- Repo: https://github.com/Equim-chan/Mortal
- Approach: Imitation learning from Tenhou Phoenix logs + PPO self-play
- Architecture: ResNet/CNN encoder + MLP heads
- Performance: 7-8 dan on Tenhou, decent on Mahjong Soul
- Limitations: 4-player only, weak interpretability, no formal evaluation paper, no live public deployment

### research papers
- Li et al. 2020 (Suphx) — closed
- Mizukami & Tsuruoka 2015 — early supervised approach
- Various NeurIPS/AAMAS workshop papers on smaller mahjong variants

### gap summary

| Capability | Suphx | NAGA | mortal | This project |
|---|---|---|---|---|
| Open source | ❌ | ❌ | ✅ | ✅ |
| 4-player ryanma | ✅ | ✅ | ✅ | ✅ |
| 3-player sanma | ❌ | ✅ | ❌ | ✅ (planned) |
| Modern architecture (transformer) | ❌ | ? | ❌ | ✅ |
| Interpretability overlay | ❌ | partial | ❌ | ✅ |
| Live public ladder bot | ❌ | ❌ | ❌ | ✅ |
| Reproducible training pipeline | ❌ | ❌ | partial | ✅ |
| Formal paper with evaluation protocol | ✅ | ❌ | ❌ | ✅ |

---

## 3. differentiation strategy

Single-axis improvements over mortal aren't enough for a credible launch. The differentiation must be a *bundle*:

### 3.1 Modern architecture
- Transformer-based encoder over the game state (tiles, discards, riichi calls, dora indicators, scores, round info)
- Treat the game state as a sequence of typed tokens; attention layer captures inter-tile relationships (suji, kabe, kanchan)
- Compare against mortal's CNN baseline in ablation

### 3.2 Improved RL pipeline
- Imitation pretraining on Tenhou Phoenix logs (2M+ games available publicly)
- PPO with entropy regularization
- Add **decision-transformer-style return-conditioned pretraining** as an alternative head, evaluate which works better
- Explore **offline RL** (CQL, IQL) over the full Tenhou log corpus as a stronger pretrain than pure imitation

### 3.3 Interpretability layer
- For each discard, surface:
  - Top-3 alternative discards with predicted value
  - Tile efficiency score (shanten reduction probability)
  - Defense score (deal-in probability given opponent state)
- Render as a transparent overlay on Mahjong Soul replays
- This is the **single biggest viral differentiator** — turns the bot into a teaching tool, not just a player

### 3.4 Sanma (3-player) extension
- Mahjong Soul Sanma is enormously popular but barely studied
- Different ruleset: no man 2-8 tiles, kita uragra mechanics, different yaku weights
- Train a separate Sanma model from the same pipeline
- This single contribution justifies a separate paper if needed

### 3.5 Public evaluation without unauthorized ladder automation
- Primary public proof should be reproducible offline benchmarks, duplicate-mahjong matches, and replay-analysis pages.
- If a live ladder account is ever used, it requires explicit platform permission and public disclosure before implementation.
- Auto-posted replay URLs can still exist for permitted private-room/tournament games or offline replay analysis, not unapproved ranked automation.
- "AI is currently ranked Saint 3" is a compelling narrative, but it is not a Phase 0 or Phase 1 deliverable because the compliance risk is too high.

### 3.6 Tooling and dev experience
- One-command Docker run for the bot
- Browser-playable demo (you vs the AI on a web mahjong client)
- Discord bot for hand analysis
- VSCode-style replay analyzer

---

## 4. technical design

### 4.1 game representation

State features (per decision point):
- **Hand**: 14 tiles, one-hot encoded over 34 tile types + ankan/minkan flags
- **River (discards)**: per-player ordered list of discarded tiles, with riichi-stick markers
- **Calls**: pon/chi/kan calls per player with tile sources
- **Dora indicators**: visible dora tiles
- **Scores**: 4-player score state, round wind, seat wind
- **Turn count**: from 0 (deal) to ~18 (haitei)
- **Riichi state**: which players have declared, when
- **Remaining wall**: count of unseen tiles by type (a function of visible info)

Action space (factored):
- **Discard**: 34 tile types + tsumogiri flag
- **Riichi**: yes/no
- **Call**: pon / chi / kan / pass on opponent's discard
- **Win**: ron / tsumo when legal

### 4.2 architecture

```
Input tokens (state) → Embedding layer → Transformer encoder (6-12 layers) → 
  → Policy head (per action type) 
  → Value head (expected hand outcome in points)
  → Auxiliary heads (shanten prediction, opponent tenpai prediction)
```

Token types: tile (34) + position (hand/river/call/dora) + player (4) + meta (turn, score). All embedded into a shared vector space.

Estimated parameter count: 10-50M parameters (similar order to small LLMs, fits comfortably on Mac for inference).

### 4.3 training pipeline

**Phase 1 — Data preparation (weeks 1-2)**
- Scrape/obtain Tenhou Phoenix room logs (publicly available, ~2M games)
- Parse into `(state, action, outcome)` triples
- Build PyTorch Dataset class with on-the-fly augmentation (mirror suits, etc.)
- Train/val split by date

**Phase 2 — Supervised pretraining (weeks 3-6)**
- Behavior cloning on Tenhou Phoenix logs
- Cross-entropy loss on action prediction
- Mac MPS: ~3-7 days for full pretrain on M-series, depending on model size
- Cloud burst alternative: ~$30-50 on Lambda H100 for a single overnight run

**Phase 3 — RL fine-tuning (weeks 7-12)**
- Self-play with PPO
- Reward shaping: hand value at hand end + survival bonus + deal-in penalty
- Population-based training: maintain 4-8 agent variants, play them against each other
- Cloud GPU required: estimate $200-500 for full RL training pass
- Checkpoint and ablate

**Phase 4 — Evaluation (weeks 13-14)**
- Internal: head-to-head against akochan, mortal
- External: only permitted live environments, private rooms, or public replay challenges; no Tenhou Phoenix or Mahjong Soul ranked automation without permission
- Track win rate, average placement, deal-in rate, riichi rate, win value, defense correctness

**Phase 5 — Sanma extension (weeks 15-18)**
- Adapt model for 3-player rules
- Retrain from scratch on Mahjong Soul Sanma logs
- Separate evaluation pass

### 4.4 stack

| Layer | Choice | Reason |
|---|---|---|
| Language | Python 3.11+ | Standard for ML |
| ML framework | PyTorch | MPS support, ecosystem |
| RL framework | CleanRL or custom PPO | Auditable, modifiable |
| Game simulator | Fork mortal's simulator OR write fresh in Rust | Rust if performance matters |
| Logging | wandb (free tier) | Visible runs, sharable |
| Serving | FastAPI + ONNX export | Cross-platform inference |
| Frontend | Vite + React + Tailwind | For browser demo |
| Bot harness | Deferred | Do not implement live platform automation without explicit permission |

### 4.5 compute budget estimate

| Item | Cost |
|---|---|
| Tenhou Phoenix log dataset | $0 (public) |
| Mac dev work (free) | $0 |
| Supervised pretraining (cloud, one-shot) | ~$50 |
| RL fine-tuning passes (5-10 iterations) | ~$200-400 |
| Sanma training | ~$100 |
| Demo hosting (year 1) | ~$60 (cheap VPS) |
| Domain name | ~$15 |
| **Total** | **~$425-625** |

This is well within "side project budget" territory.

---

## 5. roadmap

### Phase 0 — Scoping and validation (week 1)
- [ ] Validate Tenhou Phoenix log availability (download a sample, verify parser works)
- [ ] Fork mortal, get it running locally on Mac (baseline reproduction)
- [ ] Spin up a Mahjong Soul account, verify replay sharing works
- [ ] Confirm cloud GPU rental pipeline (Lambda or RunPod test run)
- [ ] Lock in project name and create the GitHub repo (private until launch)

### Phase 1 — Data and baseline (weeks 2-3)
- [ ] Parse Tenhou logs into training format
- [ ] Implement PyTorch Dataset + DataLoader
- [ ] Train a simple MLP baseline on discard prediction to validate pipeline
- [ ] Tile efficiency calculator (shanten counter)
- [ ] Defense scorer (deal-in probability estimator)

### Phase 2 — Architecture (weeks 4-6)
- [ ] Implement transformer encoder for mahjong state
- [ ] Behavior cloning on Tenhou Phoenix logs
- [ ] Match mortal's supervised baseline performance
- [ ] First cloud training burst

### Phase 3 — RL pipeline (weeks 7-12)
- [ ] Self-play harness (simulator + multi-agent training loop)
- [ ] PPO implementation tuned for mahjong reward structure
- [ ] Population-based training
- [ ] Evaluate against mortal and akochan
- [ ] Target: match or exceed mortal on Tenhou General room

### Phase 4 — Deployment infrastructure (weeks 13-14)
- [ ] Permission-aware replay ingestion and review pipeline
- [ ] Auto-replay-sharing pipeline for permitted games and offline analysis
- [ ] Live rank tracker website
- [ ] Browser playable demo (you vs AI)
- [ ] Interpretability overlay

### Phase 5 — Sanma (weeks 15-18)
- [ ] Sanma ruleset implementation
- [ ] Mahjong Soul Sanma log scraping
- [ ] Sanma-specific training
- [ ] Evaluation

### Phase 6 — Paper and launch (weeks 19-24)
- [ ] Final evaluation runs
- [ ] arxiv preprint
- [ ] README with clip
- [ ] HN "Show HN" launch + Twitter/X clip
- [ ] Reddit /r/Mahjong, /r/MachineLearning posts
- [ ] Reach out to mahjong streamers/youtubers for showcase

---

## 6. demo strategy

### the question: can mahjong produce viral clips?

Yes. Specifically:

### Mahjong Soul as demo platform
- Beautiful anime-style UI (Japanese voice actors, character art, particle effects on wins)
- **Built-in replay sharing**: every game produces a shareable URL with full replay animation
- Rank-up screens are designed to be cinematic ("Promotion to Saint 3!")
- Dramatic wins (haitei, chankan, kokushi) trigger special animations
- Sound design is excellent for cuts

### clip formats
1. **Rank-up moments** (60 seconds): "AI reaches Mahjong Soul Saint" — show the climb, then the cinematic rank-up screen
2. **Dramatic wins** (30 seconds): the AI calling a difficult win with the interpretability overlay showing why it chose that line
3. **Defensive plays** (45 seconds): the AI folding to avoid dealing in, with overlay showing deal-in probability, then opponent reveals the dangerous tile
4. **Live ladder push** (vertical reel): timelapse of the AI climbing from Adept to Saint over a week
5. **Side-by-side with humans** (90 seconds): same hand, show what a 7-dan human did vs what the AI did
6. **"AI's first time winning a yakuman"** (60 seconds): the bot achieving rare hand types

### launch sequence
- T-7 days: pre-record 5 hero clips
- T-0: HN "Show HN" post with the strongest 60-second clip embedded, README opens with the clip
- T-0 + 2hr: Twitter/X thread with rank-up clip
- T+1d: /r/MachineLearning post once HN momentum settles
- T+3d: /r/Mahjong, /r/Mahjongsoul, /r/Tenhou cross-posts
- Ongoing: live ladder bot keeps posting replays daily

### versus Marvel Snap comparison

| Axis | Marvel Snap | Riichi Mahjong |
|---|---|---|
| Clip density (drama per second) | Higher (cube reveals) | Lower (long hands) |
| Prestige per clip | Lower ("won a match") | Higher ("reached Saint") |
| Universal recognizability | Higher (Marvel IP) | Lower (mahjong is regional) |
| Demo visual quality | Medium (mobile UI) | High (anime UI) |
| Auto-shareable format | Manual screen record | Built-in replay URLs |
| Long-term content engine | Limited | Strong (daily ladder posts) |
| Audience overlap with ML community | Medium | High (Japan/Asia ML twitter loves mahjong) |
| Closed-SOTA-to-beat narrative | None | Strong (Suphx, NAGA) |

Mahjong wins on prestige and narrative; snap wins on viral velocity. Both are viable.

---

## 7. paper plan

### target venues (ranked by fit)
1. **AAMAS** (Autonomous Agents and Multiagent Systems) — strong fit, multi-agent imperfect-info specialty
2. **AIIDE** (AAAI Artificial Intelligence and Interactive Digital Entertainment) — practitioner-friendly, games focus
3. **FDG** (Foundations of Digital Games) — broader scope, accepts game AI work
4. **NeurIPS Workshops** (Gaming, MARL, etc.) — workshop track is more accessible than main conference

### paper structure (draft)
1. **Introduction**: Mahjong as a research target, Suphx-mortal gap, this work's contribution
2. **Background**: Riichi rules, prior work (Suphx, NAGA, mortal, akochan), open challenges
3. **Approach**: 
   - Transformer-based state encoding
   - Imitation pretraining on Tenhou Phoenix
   - PPO with population-based training
   - Auxiliary heads for interpretability
4. **Sanma extension**: how the same pipeline transfers
5. **Evaluation**:
   - Head-to-head against akochan, mortal
   - Local duplicate-mahjong benchmark performance
   - Permitted private-room or public replay challenge results, if available
   - Ablations: transformer vs CNN, with vs without aux heads, sanma transfer
6. **Discussion**: Limitations, ethical considerations (TOS, fairness)
7. **Conclusion + future work**

### contribution claims (defensible)
- Open-source riichi mahjong agent with reproducible evaluation against open baselines
- Transformer-based policy architecture evaluated under a published protocol
- First open Sanma agent
- Interpretability overlay novel in mahjong AI literature
- Reproducible training pipeline released

---

## 8. risks and open questions

### 8.1 TOS and bot policy
- **Tenhou**: AI play has explicit restrictions. Phoenix-table AI play is prohibited; higher-table AI play is not open by default and requires a dedicated ID. Log redistribution and non-Tenhou uses are restricted.
- **Mahjong Soul**: public terms prohibit bots, automation software, and unauthorized third-party software. Treat ranked automation as off-limits unless written permission is obtained.
- **Mitigation**: Build training, local evaluation, replay analysis, and demos first. Keep live play behind a permission gate. Use private-room/tournament setups only when rules and participants allow it.

### 8.2 differentiation sufficiency
- **Risk**: mortal already does most of this. The bundle (transformer + sanma + interpretability + live bot) needs to actually deliver meaningful improvements.
- **Mitigation**: Set explicit performance targets before launch. If transformer matches mortal but doesn't exceed, the contribution is the bundle (sanma + interpretability + deployment), not raw playing strength. Pivot the paper framing accordingly.

### 8.3 compute requirements
- **Risk**: RL self-play could need 10x more compute than estimated.
- **Mitigation**: Start with supervised-only baseline (cheap, fits on Mac). Validate that pipeline works end-to-end on a small scale before scaling up. Burst to cloud only after pretraining is validated.

### 8.4 evaluation legitimacy
- **Risk**: Self-reported Tenhou/MJS ranks are gameable; "AI reached Saint" loses credibility if I can't show how.
- **Mitigation**: All games auto-logged. Public replay URLs. Adversarial evaluation: invite human players to challenge the bot. Possibly host a public tournament.

### 8.5 differentiation from mortal becomes thin
- **Risk**: Equim Chen ships a major mortal update with transformer architecture or sanma before this project launches.
- **Mitigation**: Don't keep this private for 6 months. Public repo from week 2. Updates in public. Build narrative momentum gradually so the launch is the *culmination* of visible work, not a surprise.

### 8.6 mahjong audience is regional
- **Risk**: HN front page might not care; the project is much more interesting to JP/KR/CN/TW audiences than US tech bro audience.
- **Mitigation**: Translate README and key launch posts into JP/CN. Reach out to mahjong content creators directly (e.g., USPML, ShoeYourSki, Japanese mahjong YouTubers). Don't depend solely on HN.

### 8.7 personal timeline
- **Risk**: I'm balancing internship, CS446, AetosAnon, BTO application, partner relationship. 6-month timeline assumes 10-15 hours/week. Slip is likely.
- **Mitigation**: Build in slack. Set monthly checkpoints. If month 2 is behind, pivot to the simpler Skull project as a viral warm-up to reset momentum.

---

## 9. open questions to resolve in phase 0

1. **Tenhou log access**: verify sample availability through `houou-logs`, use one download session only, and document non-redistribution constraints.
2. **mortal architecture details**: read the code, confirm CNN baseline, understand training pipeline
3. **Mahjong Soul automation legality**: currently treat ranked automation as prohibited; only revisit after explicit permission or a clearly permitted API/sandbox exists.
4. **Cloud GPU cheapest source**: Lambda Labs vs RunPod vs vast.ai pricing for the relevant GPU/duration mix
5. **Sanma log availability**: where are MJS sanma logs, is there a scraping pipeline
6. **Name**: confirm "kenjaku" isn't trademarked/registered elsewhere in the mahjong AI space
7. **License**: MIT vs Apache 2.0 vs AGPL — consider how this affects future commercial use
8. **Branding**: domain, twitter handle, github org

---

## 10. references

### key papers
- Li et al. 2020, "Suphx: Mastering Mahjong with Deep Reinforcement Learning", arXiv:2003.13590
- Mizukami & Tsuruoka 2015, "Building a Computer Mahjong Player Based on Monte Carlo Simulation and Opponent Models"
- Brown & Sandholm, "Superhuman AI for heads-up no-limit poker: Libratus beats top professionals", Science 2017 (CFR context)
- Wang et al. 2019, "Deep Counterfactual Regret Minimization" (Deep CFR)
- Schmid et al. 2023, "Mastering Stratego with Model-Free Multiagent Reinforcement Learning" (DeepNash, R-NaD)

### code references
- mortal: https://github.com/Equim-chan/Mortal
- akochan: https://github.com/critter-mj/akochan
- houou-logs: https://github.com/Apricot-S/houou-logs
- mjx: https://github.com/mjx-project/mjx
- Mahjax: https://arxiv.org/abs/2605.20577
- MahjongLM dataset/model direction: https://huggingface.co/datasets/mitsutani/mahjonglm-dataset

### community
- /r/Mahjong, /r/Mahjongsoul, /r/Tenhou
- USPML (US Pro Mahjong League)
- Tenhou client docs
- Mahjong Soul community wiki

### tools
- wandb (training run tracking)
- HuggingFace (model hosting, if I open-weight the trained models)
- arxiv (preprint)
- GitHub (code)
- Vercel/Fly.io (live demo hosting)

---

## 11. immediate next steps (week 1)

1. Create repo scaffold: Python package, tests, CLI, docs, data policy.
2. Implement tile/action/state primitives with unit tests.
3. Validate a tiny Tenhou XML parsing fixture without redistributing raw log archives.
4. Evaluate `houou-logs` as an external downloader and document exact compliant usage.
5. Clone and build mortal locally, confirm inference/evaluation hooks on Mac.
6. Read Suphx and Mahjax end-to-end, extract reusable simulator/evaluation ideas.
7. Draft README.md skeleton from this file with compliance-aware positioning.

---

## 12. implementation log

### 2026-06-03

- Reframed the project from live ladder automation to a permission-aware research agent and replay-analysis toolkit.
- Recorded current external constraints: Tenhou AI/log restrictions, Mahjong Soul automation risk, `houou-logs` as the maintained downloader, `mortal` as the baseline, `mjx` caveats, and `Mahjax`/MahjongLM as new areas to track.
- Added the initial Python package scaffold: `pyproject.toml`, `README.md`, package entrypoints, CLI shell, and project ignore rules.
- Added immutable core domain primitives for tile types, physical red-five tiles, rule presets, player actions, melds, discards, and player-perspective round state.
- Added standard-library unit tests for tile parsing/counting, action validation, rule presets, visible-state accounting, and unseen tile counts.
- Next implementation target: compliant data/fixture guidance and a tiny Tenhou XML parsing fixture.
