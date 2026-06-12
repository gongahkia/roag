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
- `houou-logs` also supports exporting downloaded `mjlog` XML from its local database into a
  directory, which is the right bridge into Kenjaku's local-only CLI. Reference:
  https://github.com/Apricot-S/houou-logs
- Tenhou's `N m="..."` meld code has a documented bit layout for chi, pon, chakan/kakan, and kan in `NegativeMjark/tenhou-log`. Use exact decoding before attempting post-call hand reconstruction. Reference: https://github.com/NegativeMjark/tenhou-log
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
- [x] Validate Tenhou Phoenix log availability (download a sample, verify parser works)
- [x] Fork mortal, get it running locally on Mac (baseline reproduction)
- [ ] Spin up a Mahjong Soul account, verify replay sharing works
- [ ] Confirm cloud GPU rental pipeline (Lambda or RunPod test run)
- [x] Lock in project name and create the GitHub repo (private until launch)

### Phase 1 — Data and baseline (weeks 2-3)
- [x] Parse Tenhou logs into supervised discard/call/riichi training formats
- [x] Implement minimal PyTorch discard Dataset + DataLoader
- [x] Train a simple masked-logit MLP baseline on discard prediction to validate pipeline
- [x] Tile efficiency calculator (shanten counter)
- [ ] Defense scorer (deal-in probability estimator)
  - [x] Report-only threshold calibration for `deal-in-linear-v0` benchmark artifacts

### Phase 2 — Architecture (weeks 4-6)
- [x] Implement transformer encoder for mahjong state
- [ ] Behavior cloning on Tenhou Phoenix logs
- [ ] Match mortal's supervised baseline performance
- [ ] First cloud training burst

### Phase 3 — RL pipeline (weeks 7-12)
- [ ] Self-play harness (simulator + multi-agent training loop)
  - [x] Basic sandbox exhaustive-draw tenpai/noten point-delta metadata
  - [x] Basic sandbox dragon/round-wind/seat-wind yakuhai filtering
  - [x] Basic sandbox next-round dealer/honba transition helper
  - [x] Basic sandbox round-wind state and dealer-wrap progression
  - [x] Basic sandbox dealer-aware win payment estimates
  - [x] Basic sandbox visible-dora score-estimate bonus han
  - [x] Basic sandbox red-five score-estimate bonus han
  - [x] Basic sandbox tsumo yaku/dora tile-view de-duplication
- [ ] PPO implementation tuned for mahjong reward structure
- [ ] Population-based training
- [ ] Evaluate against mortal and akochan
- [ ] Target: match or exceed mortal on Tenhou General room

### Phase 4 — Deployment infrastructure (weeks 13-14)
- [x] Permission-aware replay ingestion and review pipeline
- [ ] Auto-replay-sharing pipeline for permitted games and offline analysis
- [ ] Live rank tracker website
- [ ] Browser playable demo (you vs AI)
- [ ] Interpretability overlay

### Phase 5 — Sanma (weeks 15-18)
- [ ] Sanma ruleset implementation
  - [x] Basic Tenhou Sanma 35,000-point sandbox starts
  - [x] Basic Tenhou Sanma no-chi call filtering
  - [x] Basic Tenhou Sanma North-as-guest-wind yaku filtering
  - [x] Basic sandbox Kita/pei-nuki action with dead-wall replacement draw and bonus-han metadata
  - [x] Basic sandbox Kita ron/pass reaction window without chankan yaku
  - [x] Basic Tenhou Sanma 1m/9m dora indicator wrap
  - [x] Basic Tenhou Sanma post-pon Kita suppression
  - [x] Basic Tenhou Sanma Kita ippatsu reaction timing
  - [x] Basic Tenhou Sanma tsumo-loss payment estimates
  - [x] Basic Tenhou Sanma eight-rinshan replacement reserve cap
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
7. **License**: resolved to MIT for Kenjaku's own code. Keep AGPL external-baseline boundaries
   explicit if Mortal integration changes.
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

## 11. immediate next steps

1. Restore or regenerate the ignored local 500-log Tenhou slice, then rerun the documented
   10k/20k call reports through `benchmark-report-summary` so the report-local `selected_policy`
   is based on comparable artifacts.
2. Run `benchmark-deal-in` on the ignored 100/500-log Tenhou slices and compare the resulting
   `benchmark-report-summary` output for `deal-in-linear-v0`, including train/eval best threshold
   calibration, against the uncalibrated heuristic risk baseline before marking the defense
   scorer/probability-estimator item complete.
3. Run `benchmark-discard-mlp` and `benchmark-discard-transformer` on comparable ignored Tenhou
   slices, then summarize both artifacts before marking behavior-cloned transformer progress.
4. Keep riichi on train-best calibration for now. Fixed threshold 0.25 raises riichi recall but
   loses pass recall and is not the balanced baseline on the latest 500-log split checks.
5. Keep external-baseline work at the neutral prediction protocol. Stub producers and a generic
   subprocess producer boundary exist for tests; real Mortal inference still requires legally
   usable weights and must stay outside Kenjaku's dependency boundary.
6. Use disagreement tag filters before changing discard features. The capped 100-log sample is
   dominated by efficiency-preserving and close-logit cases, so do not add another defense profile
   until a tag-specific sample points to a concrete feature gap.
7. Use `replay-intake-review` and `replay-share-plan` for any replay URL or local-export queue
   before building analysis, demo, or sharing features. The current implementation is a manifest
   gate plus local shareability plan, not a live-service fetcher or automatic poster.
8. Do not promote `discard-linear-defense-context-v1` as a default until a matching split shows an
   aggregate or targeted gain that survives the existing risk/defense comparison.
9. Use `self-play-sandbox` only for deterministic draw/discard and basic synthetic tsumo plumbing
   checks. It is not a full rules simulator, yaku/scoring engine, PPO loop, or evidence that the
   Phase 3 self-play harness is complete.
10. Treat `self-play-sandbox --ruleset tenhou-3p` as Sanma plumbing only. It now has static tile
    exclusions, three-seat rotation, Tenhou's 35,000-point start, no-chi call filtering,
    North-as-guest-wind yaku filtering, a basic Kita/pei-nuki action, and a basic Kita ron/pass
    reaction window, plus Tenhou's 1m/9m dora indicator wrap, post-pon Kita suppression, and basic
    Kita ippatsu reaction timing, plus basic tsumo-loss payment estimates and an eight-rinshan
    replacement reserve cap, but the Phase 5 Sanma ruleset still needs real 3-player round flow,
    exact platform timing, complete call handling, scoring, training data, and evaluation.
11. Use the sandbox environment boundary for future simulator work. It currently has deterministic
    initial state, draw, legal-discard, discard history, pending-discard reaction windows, legal
    closed-hand tsumo/ron, discard-furiten, temporary ron-pass furiten, and seeded riichi-furiten
    filtering, basic closed-tenpai riichi declaration, basic post-riichi discard/call restrictions,
    post-call discard-obligation action listing,
    basic wait-preserving post-riichi closed-kan exceptions, basic riichi deposit accounting, basic
    honba bonus accounting, a basic next-round dealer/honba transition helper, explicit round-wind
    state with dealer-wrap progression, basic ippatsu window metadata, legal chi/pon/minkan, basic
    dead-wall replacement draws for minkan/ankan/kakan, basic kan-dora indicator metadata, basic
    rinshan draw-source metadata, individual reaction passes, ron-priority call gating, a basic
    chankan ron/pass window before kakan replacement draw, kokushi-only ankan robbery,
    discard/call/tsumo/ron transitions, basic multi-ron terminal resolution, and simple terminal
    reward payloads with terminal point-delta metadata, basic live-wall exhaustive-draw
    tenpai/noten point-delta metadata, basic dealer-aware ron/tsumo win payment estimates, basic
    visible-dora and red-five bonus han in score estimates, Tenhou Sanma 1m/9m dora indicator wrap,
    Tenhou Sanma post-pon Kita suppression, basic Tenhou Sanma Kita ippatsu reaction timing, tsumo
    yaku/dora tile views that avoid duplicating the drawn tile, basic Tenhou Sanma tsumo-loss
    payment estimates, a basic Tenhou Sanma eight-rinshan replacement reserve cap, plus basic
    open/kan standard-shape win detection and a basic sandbox yaku filter/metadata layer with
    dragon/round-wind/seat-wind yakuhai filtering only; the next simulator step is full call/kan
    timing, complete
    yaku/terminal legality,
    complete rinshan yaku/scoring semantics,
    complete kan-dora/ura-dora
    indicator ordering, complete chankan semantics, complete payment accounting, complete
    post-riichi kan timing, scoring, and richer reward semantics.

---

## 12. implementation log

### 2026-06-03

- Reframed the project from live ladder automation to a permission-aware research agent and replay-analysis toolkit.
- Recorded current external constraints: Tenhou AI/log restrictions, Mahjong Soul automation risk, `houou-logs` as the maintained downloader, `mortal` as the baseline, `mjx` caveats, and `Mahjax`/MahjongLM as new areas to track.
- Added the initial Python package scaffold: `pyproject.toml`, `README.md`, package entrypoints, CLI shell, and project ignore rules.
- Added immutable core domain primitives for tile types, physical red-five tiles, rule presets, player actions, melds, discards, and player-perspective round state.
- Added standard-library unit tests for tile parsing/counting, action validation, rule presets, visible-state accounting, and unseen tile counts.
- Added data policy docs that keep raw logs local, permit only synthetic/safe fixtures, and document compliant `houou-logs` usage.
- Added a minimal Tenhou XML parser and synthetic fixture covering starting hands, dora indicators, draws, discards, red fives, and tsumogiri inference.
- Extended the Tenhou parser to retain ordered draw/discard events, enabling hand reconstruction for supervised discard examples.
- Added a Phase 0 discard-example builder that reconstructs draw/discard-only hands and emits hand counts, visible counts, metadata, and supervised discard actions.
- Added a CLI smoke path for parsing a Tenhou XML file and reporting round, discard, and discard-example counts.
- Added parser records for riichi declarations, opaque calls, wins, and exhaustive draws using synthetic Tenhou fixtures.
- Made discard-example generation stop at the first opaque call or terminal event so unsupported hand reconstruction cannot contaminate supervised examples.
- Added a deterministic discard-frequency baseline and CLI command for the synthetic Tenhou fixture path.
- Researched Tenhou's meld-code bit layout from `NegativeMjark/tenhou-log` and replaced opaque call handling with exact decoding for chi, pon, kakan/chakan, open kan, and concealed kan.
- Promoted Tenhou physical tile-id mapping into a shared IO module so the XML parser and meld decoder use the same red-five and logical-tile behavior.
- Added parser integration and tests for decoded `N` call events using generated synthetic meld codes instead of arbitrary integers.
- Tightened the synthetic call fixture into a coherent discard-call-discard-win sequence so parser and reconstruction tests exercise realistic event order.
- Extended discard-example reconstruction through decoded calls: consumed tiles leave the caller's concealed hand, claimed tiles leave the river, and open meld tiles become visible context.
- Extracted shared training reconstruction state so discard, call, and future outcome-example builders use one transition path for draws, discards, and calls.
- Added call-decision examples for actual chi/pon/kan claims and legal pass decisions, using reconstructed hand counts and visible table context.
- Added a stable hash-based train/eval splitter so tiny-model experiments are reproducible without depending on Python random state.
- Added a tiny dependency-free linear softmax discard model with legal-action masking and deterministic CLI train/eval output.
- Added JSON save/load for tiny model artifacts and an optional CLI output path for reproducible local experiments.
- Added multi-file/directory Tenhou XML loading so local raw-log samples can be evaluated without copying them into the repository.
- Added JSON experiment reports for tiny linear-model runs, including input path labels, XML file
  count, example counts, split settings, metrics, and model artifact path.
- Added a local Tenhou evaluation runbook that connects `houou-logs`
  import/fetch/download/validate/export commands to Kenjaku's ignored local data paths.
- Added `inspect-tenhou` JSON reports so local raw-log exports can be audited before any training
  run starts.
- Added a dependency-free shanten calculator for standard hands, chiitoitsu, and kokushi as the
  first tile-efficiency primitive for discard analysis.
- Added discard shanten-delta utilities that compare before/after shanten for supervised discard
  examples and summarize preserved/worsened counts.
- Added discard shanten summaries to inspection and tiny-model training JSON reports.
- Ran the first real local Tenhou smoke export with `houou-logs` (5 logs under ignored
  `data/raw/`) and found a parser hardening gap: metadata tags such as `UN` can look like draw
  tags unless event tags require numeric tile suffixes.
- Fixed the metadata-tag parser gap, then completed the first real local Tenhou smoke run: 5
  exported XML files, 49 rounds, 2,262 discard examples, 591 call examples, 2,095 shanten-preserved
  discards, and a 3-epoch tiny linear discard model at 0.4227 train / 0.3274 eval accuracy. Raw
  logs, generated reports, and model artifacts stayed in ignored local paths.
- Added opt-in parser-failure accounting for larger local XML batches via `--skip-errors`, with
  report metadata for failed file paths, error types, and messages.
- Added report source metadata flags (`--source-label`, `--source-command`, `--source-date`) so
  local benchmark artifacts can record provenance without storing raw logs.
- Added a `benchmark-discard` command and JSON report that score the global frequency baseline and
  the tiny linear discard model on the same deterministic split, with source metadata,
  parse-failure accounting, and shanten summaries in one artifact.
- Ran `benchmark-discard` on the existing ignored 5-log Tenhou smoke slice: 2,262 examples, 1,810
  train / 452 eval split, frequency baseline at 0.3083 train / 0.2788 eval accuracy, and the
  3-epoch linear model at 0.4326 train / 0.3009 eval accuracy.
- Upgraded the tiny linear discard model to `discard-linear-v1` with candidate-aware tile-efficiency
  features: per-candidate hand/visible counts, terminal-or-honor flag, before/after shanten,
  shanten delta, and a shanten-preserved flag. Training and benchmark reports now record model kind
  and feature dimension.
- Re-ran the same ignored 5-log Tenhou benchmark with `discard-linear-v1`: frequency stayed at
  0.3083 train / 0.2788 eval accuracy, while the 3-epoch linear model moved to 0.5414 train /
  0.4270 eval accuracy.
- Scaled the ignored local Tenhou benchmark slice to 25 current-year four-player hanchan logs:
  25 XML files, 255 rounds, 11,855 discard examples, 3,185 call examples, zero parse failures,
  10,992 shanten-preserved discards, and 863 shanten-worsened discards. On a deterministic 9,484 /
  2,371 train/eval split, frequency scored 0.3046 train / 0.3037 eval accuracy and
  `discard-linear-v1` scored 0.5150 train / 0.4757 eval accuracy.
- Added a feature-profile switch to the tiny linear discard model and extended `benchmark-discard`
  into a compact ablation report: frequency, raw-count linear, and shanten-aware linear models now
  train and score on the same deterministic split.
- Re-ran the 25-log local benchmark with the ablation report. Raw-count linear scored 0.4313 train /
  0.3830 eval accuracy; shanten-aware `discard-linear-v1` remained at 0.5150 train / 0.4757 eval,
  giving a +0.0837 train / +0.0928 eval absolute lift over raw counts.
- Cached prepared linear-model examples inside training/scoring so candidate tile-efficiency
  features are computed once per example per profile instead of once per epoch. The 25-log ablation
  benchmark preserved the same metrics and completed in about 16.6 seconds locally.
- Added held-out discard error-analysis summaries to benchmark reports. Each model now reports
  accuracy by actual discard shanten impact, discarded tile family, and rough round event phase.
- Re-ran the 25-log local benchmark with error analysis. The shanten-aware linear model scored
  0.5091 on shanten-preserving eval discards but only 0.0414 on shanten-worsening eval discards,
  suggesting the next modeling work needs risk/context features rather than more tile-efficiency
  pressure alone.
- Added `docs/session-handoff.md` so an independent agent can resume from the current state without
  relying on chat history.
- Expanded `docs/session-handoff.md` with the explicit stop state, working rules, hidden
  assumptions, local-artifact constraints, and the exact files where riichi/opponent-river context
  work should start.
- Next implementation target: investigate shanten-worsening decisions and add the first risk/context
  features, starting with riichi state and opponent-river visibility before scaling beyond the
  25-log local slice.

### 2026-06-04

- Added first risk-context discard-example fields: active riichi seats, per-seat river count
  snapshots, and seat-relative discard index.
- Added `discard-linear-risk-context-v0`, preserving the raw-count and shanten-aware model kinds as
  ablation anchors.
- Extended `benchmark-discard` to report frequency, raw-count linear, shanten-aware linear, and
  risk-context linear models on one deterministic split.
- Added held-out `by_seat_turn_phase` error-analysis buckets alongside the existing rough event
  phase buckets.
- Updated tests and handoff docs for the new context fields, benchmark report shape, and next
  implementation targets.
- Added ordered river snapshots and riichi declaration chronology to discard examples so defensive
  features can distinguish pre-riichi and post-riichi river evidence.
- Added dependency-free defense feature helpers for active opponent riichi, genbutsu, basic suji,
  basic kabe, one-chance, and candidate visibility before/after riichi.
- Added `discard-linear-defense-context-v0`, preserving the existing raw-count, shanten-aware, and
  risk-context model kinds as ablation anchors.
- Extended `benchmark-discard` to report the defense-context model and its lift over risk context.
- Added held-out `by_active_opponent_riichi` and `by_actual_discard_genbutsu` error-analysis
  buckets.
- Did not record new 25-log metrics because the ignored local Tenhou slice was absent in this
  workspace.

### 2026-06-05

- Installed `houou-logs` locally as an isolated `uv` tool, fetched the current-year Tenhou index,
  downloaded 25 four-player hanchan logs, validated them, and exported XML under ignored
  `data/raw/`.
- Re-ran the 25-log `benchmark-discard` slice with the fixed `tenhou-25-v0` split: 11,855
  examples, 9,484 train, 2,371 eval, and zero parse failures.
- Recorded risk-context and defense-context metrics: risk-context linear scored 0.5314 train /
  0.4829 eval, while defense-context linear scored 0.5357 train / 0.4825 eval, giving a +0.0043
  train lift but -0.0004 eval lift over risk context.
- Defense context improved targeted buckets despite the aggregate miss: active opponent riichi
  improved from 0.4734 to 0.4911, actual-discard genbutsu improved from 0.5030 to 0.5636, and
  shanten-worsening discards improved from 0.0769 to 0.1183.
- Updated `docs/session-handoff.md` with the benchmark findings and next target: add finer
  held-out defense diagnostics for actual discard suji, kabe, one-chance, and pre/post-riichi
  visibility before creating a new defense model kind.
- Added finer held-out defense diagnostics for actual discard suji, kabe, one-chance, and whether
  the actual discard was visible before or after an opponent's riichi declaration.
- Enriched discard examples with opponent meld ownership, dora indicators, last-discard tsumogiri
  snapshots, and ippatsu-active flags.
- Added `discard-linear-defense-context-v1` as a new stable model kind rather than mutating
  `discard-linear-defense-context-v0`. The v1 profile adds active-opponent defense fractions,
  sotogawa-style outside tiles, live terminal/honor pressure, dora/indicator flags, ippatsu timing,
  last-tsumogiri-after-riichi context, and opponent meld-tile pressure.
- Re-ran the fixed 25-log benchmark with the new diagnostics and v1 profile: v1 scored 0.5415
  train / 0.4884 eval, improving +0.0059 eval over defense-context v0 and about +0.0055 over
  risk-context. Targeted eval buckets also improved, including active-riichi 0.4734 -> 0.4911 ->
  0.5038 and actual-genbutsu 0.5030 -> 0.5636 -> 0.5818 for risk -> defense -> v1.
- Downloaded enough current-year Tenhou logs to export an ignored 100-log four-player hanchan
  slice, then ran inspect and benchmark reports: 100 XML files, 1,039 rounds, 50,276 discard
  examples, 13,435 call examples, and zero parse failures.
- The 100-log benchmark changed the interpretation: risk-context linear remained best at 0.4919
  eval, defense-context v0 fell to 0.4881, and defense-context v1 fell further to 0.4840. Defense
  features still helped some targeted buckets, such as actual-suji and shanten-worsening discards,
  but the aggregate regression means the next work should diagnose/regularize before scaling.
- Cloned Mortal into ignored `data/raw/external/mortal` at
  `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`, built `libriichi` locally, ran the Rust workspace
  tests, and verified Python can import the built module after copying
  `target/release/libriichi.dylib` to `mortal/libriichi.so`.
- Added `docs/external-baselines.md` with Mortal build results, AGPL boundary notes, and an offline
  comparison path based on `mjai` decision snapshots rather than live ladder automation.
- Added `benchmark-report-summary` so ignored benchmark JSON reports can be compared without ad hoc
  extraction scripts.
- Wired the existing linear-model L2 parameter through `train-discard-linear` and
  `benchmark-discard`, and recorded `l2` in JSON training blocks.
- Added linear-model feature names, feature activation summaries, and weight summaries to
  benchmark reports for every linear profile.
- Added `benchmark-discard --disagreements` to write local-only capped examples where risk-context
  and defense-context models disagree, including defense buckets and legal-candidate logits.
- Ran controlled 100-log sweeps. Lowering learning rate to `0.05` was the clear improvement:
  risk-context and defense-context v0 both reached 0.5124 eval accuracy, while v1 reached 0.5102.
  L2 hurt aggregate accuracy at both `0.1` and `0.05`.
- The best 100-log `lr=0.05`, `l2=0.0` run preserved targeted defense gains despite tying risk in
  aggregate: actual-suji improved 0.3686 -> 0.4278 -> 0.4227, seen-after-riichi improved 0.5401 ->
  0.5875 -> 0.5935, and shanten-worsening improved 0.0977 -> 0.1015 -> 0.1157 for risk ->
  defense -> v1.
- Added sparse discard benchmark selection with `benchmark-discard --models all|fast|...`; `fast`
  keeps frequency, shanten-aware, risk-context, and defense-context while skipping raw-count and v1.
- Added `disagreement-report-summary` for capped disagreement artifacts, including defense bucket
  rates, common actual/predicted tile pairs, and correct/wrong model logit-margin summaries.
- Added `call-frequency-v0` and `benchmark-call` as the first supervised call/pass baseline using
  existing `CallExample` reconstruction data.
- Ran the 100-log fast discard benchmark at the best known settings (`lr=0.05`, `l2=0.0`): frequency
  0.2985 eval, shanten-aware 0.5012, risk-context 0.5124, and defense-context 0.5124.
- Ran the first 100-log call benchmark: `call-frequency-v0` scored 0.8524 train / 0.8463 eval, but
  the result is pass-dominant with 1.0000 eval pass accuracy and 0.0000 eval call accuracy on 413
  actual call examples.
- Added `call-legal-frequency-v0`, a diagnostic opposite-side call baseline that ignores pass
  counts, predicts the most frequent legal non-pass action, and falls back to pass only when no
  legal call is available.
- Expanded `benchmark-call` reports into a `models` map with both call baselines and
  imbalance-aware metrics: balanced accuracy, macro recall, pass/call recall, and per-action
  recall.
- Re-ran the 100-log call benchmark. `call-frequency-v0` scored 0.8463 eval accuracy, 0.5000
  balanced eval accuracy, and 0.0000 call recall; `call-legal-frequency-v0` scored 0.1515 eval
  accuracy, 0.4927 balanced eval accuracy, 0.0000 pass recall, and 0.9855 call recall.
- Refreshed disagreement diagnostics at the current best discard settings, `lr=0.05`, `l2=0.0`.
  Counts were risk-correct/defense-wrong 203, risk-correct/v1-wrong 268,
  defense-correct/risk-wrong 203, and v1-correct/risk-wrong 246; capped samples showed mixed
  safety signals rather than one obvious feature gap.
- Added `disagreement-report-summary --examples N` to render representative stored disagreement
  examples with actual/predicted tiles, defense buckets, and top model logits.
- Added `call-linear-v0`, a dependency-free masked softmax call/pass model over pass plus legal
  chi/pon/minkan candidates, using discarded-tile, legal-kind, hand/visible-count, relative-seat,
  and concealed-remainder shanten-proxy features.
- Re-ran the 100-log call benchmark. `call-linear-v0` scored 0.8288 eval accuracy, 0.7294 balanced
  eval accuracy, 0.8729 pass recall, and 0.5860 call recall, a useful balanced floor compared with
  the two frequency extremes.
- Added conservative riichi/pass examples from explicit Tenhou reach events plus closed-tenpai
  no-riichi discard decisions, then added `benchmark-riichi` and `riichi-frequency-v0`.
- Ran the first 100-log riichi benchmark: 2,039 riichi/pass examples with `riichi-frequency-v0` at
  0.6618 eval accuracy, 0.5000 balanced eval accuracy, 1.0000 pass recall, and 0.0000 riichi
  recall.

### 2026-06-06

- Added `call-linear-v1` as an additive call feature profile rather than mutating
  `call-linear-v0`. V1 appends exact chi-position flags, consumed-tile/open-call proxies,
  shanten-improvement indicators, cached ukeire proxies, discarded-tile visibility, and
  terminal/honor flags while keeping the v0 kind and 120-feature prefix stable.
- Extended `benchmark-call` to report `call_linear_v1` beside the existing call baselines.
- Added `riichi-linear-v0`, a dependency-free masked softmax model over conservative riichi/pass
  examples with hand/visible counts, score, dealer, turn, active-riichi, river-count, shanten, and
  hand-shape features.
- Extended `benchmark-riichi` with linear training knobs and a `riichi_linear` report payload.
- Added `disagreement-report-summary --tags`, which derives deterministic stored-example tags:
  `defense_signal`, `efficiency_like`, `close_logit`, `active_riichi`, `safe_tile_candidate`, and
  `no_obvious_signal`.
- Re-ran fixture smoke benchmarks and the 100-log local call/riichi benchmarks on the
  `tenhou-100-v0` split. `call-linear-v1` scored 0.8377 eval accuracy, 0.7496 balanced eval
  accuracy, 0.8769 pass recall, and 0.6223 call recall versus v0 at 0.8288 / 0.7294 / 0.8729 /
  0.5860.
- On the same 100-log riichi split, `riichi-linear-v0` scored 0.4510 eval accuracy, 0.5639 balanced
  eval accuracy, 0.2148 pass recall, and 0.9130 riichi recall, while `riichi-frequency-v0` remained
  the pass-only floor at 0.6618 eval accuracy, 0.5000 balanced accuracy, 1.0000 pass recall, and
  0.0000 riichi recall.
- The tagged current-best discard disagreement sample (`lr=0.05`, `l2=0.0`) has 400 stored
  examples: 373 efficiency-like, 336 close-logit, 130 active-riichi, 130 safe-tile-candidate, and
  82 defense-signal tags. No stored item fell into `no_obvious_signal` under the current rules.
- Added report-only threshold calibration to linear call and riichi benchmark payloads without
  changing default model predictions. Each linear payload now records the 0.00-1.00 grid, train/eval
  sweep rows, and a deterministic best threshold.
- Added masked `logits_for_example` and `probabilities_for_example` helpers for `CallLinearModel`
  and `RiichiLinearModel`.
- Added `disagreement-report-summary --tag TAG` to render stored examples matching one deterministic
  tag while still showing the aggregate tag counts.
- On the 100-log `tenhou-100-v0` split, calibrated `call-linear-v1` at threshold 0.40 raised binary
  balanced accuracy to 0.7762 with call precision 0.4393, call recall 0.7191, and pass recall
  0.8333. The unthresholded exact-action v1 report remained 0.8377 eval accuracy, 0.7496 balanced
  accuracy, 0.8769 pass recall, and 0.6223 call recall.
- On the same split, calibrated `riichi-linear-v0` at threshold 0.95 raised binary balanced accuracy
  to 0.6444 with riichi precision 0.5476, riichi recall 0.5000, and pass recall 0.7889. The
  unthresholded model remains useful mainly as a riichi-recall probe because it overcalls.
- Added explicit calibrated benchmark policy variants without changing model kinds or default
  `predict()` behavior: `call_linear_v1_calibrated` at non-pass threshold 0.40 and
  `riichi_linear_calibrated` at riichi threshold 0.95.
- Added opt-in positive class-weight comparison variants via `--include-weighted`,
  `--call-positive-weight`, and `--riichi-positive-weight`. The linear model training metadata now
  records `positive_class_weight`.
- On the 100-log `tenhou-100-v0` call split, `call_linear_v1_calibrated` scored 0.8154 eval
  accuracy, 0.7750 exact-action balanced eval accuracy, 0.8333 pass recall, and 0.7167 exact-call
  recall. The positive-weight 2.0 comparison scored 0.6729 eval accuracy, 0.7522 balanced eval
  accuracy, 0.6376 pass recall, and 0.8668 call recall; its own threshold sweep picked 0.80 with
  binary balanced accuracy 0.7669.
- On the same riichi split, `riichi_linear_calibrated` scored 0.6912 eval accuracy, 0.6444 balanced
  eval accuracy, 0.7889 pass recall, and 0.5000 riichi recall. The positive-weight 2.0 comparison
  scored 0.3603 eval accuracy, 0.5114 balanced eval accuracy, 0.0444 pass recall, and 0.9783 riichi
  recall; its own threshold sweep picked 0.95 with binary balanced accuracy 0.5713.
- Added `export-decision-snapshots`, a neutral local JSONL exporter for discard/call/riichi
  decision points. Rows include Kenjaku reconstruction fields, legal actions, observed action, and a
  minimal `mjai_events` prefix for future offline baseline comparison without copying Mortal code.
- Extended `benchmark-report-summary` to summarize call and riichi benchmark reports as well as
  discard reports, including balanced accuracy, pass/target recall, policy threshold, and positive
  class weight.
- Downloaded/exported an ignored 500-log four-player hanchan slice under
  `data/raw/tenhou/xml/4p-hanchan-500`. The riichi benchmark completed: 10,249 examples, 8,199
  train / 2,050 eval. `riichi_linear` scored 0.6712 eval accuracy, 0.6337 balanced accuracy,
  0.7660 pass recall, and 0.5014 riichi recall. Fixed-threshold `riichi_linear_calibrated` at 0.95
  fell to 0.5143 balanced accuracy, while `riichi_linear_weighted` at weight 2.0 scored 0.6412
  balanced accuracy. The unweighted sweep preferred threshold 0.20 with 0.6606 binary balanced
  accuracy.
- The equivalent 500-log call benchmark with `--include-weighted` was stopped after more than an
  hour while still CPU-active, before producing a report. Treat large-slice call benchmarking as a
  runtime problem before using it to choose policies.
- Added prepared-example prediction helpers for call and riichi linear models so benchmark scoring
  and threshold sweeps can reuse prepared feature vectors instead of rebuilding features for every
  prediction path.
- Added `benchmark-call --models all|fast|...` and `--example-limit`. `fast` runs the bounded call
  comparison path with frequency, legal-frequency, `call_linear_v1`, and
  `call_linear_v1_calibrated`.
- Added `--call-threshold-source fixed|train-best` and
  `--riichi-threshold-source fixed|train-best`. Defaults preserve the prior fixed thresholds, while
  train-best uses the train split sweep winner and records that source in report policy metadata.
- Added `decision-snapshot-summary`, a neutral local JSONL summary/validator for decision snapshot
  exports. Fixture summary smoke counted five snapshots, zero malformed rows, and present
  `mjai_events` for all five rows.
- The uncapped 500-log call fast run with train-best calibration was still CPU-active after about
  five minutes and was stopped before producing a report. A bounded run using 5,000 of 69,013 call
  examples, 5 epochs, `--models fast`, and train-best calibration completed: `call_linear_v1` scored
  0.8760 eval accuracy and 0.5468 balanced eval accuracy; `call_linear_v1_calibrated` selected
  threshold 0.05 and scored 0.7910 eval accuracy, 0.6428 balanced eval accuracy, 0.8453 pass recall,
  and 0.4403 call recall.
- Re-ran the 500-log riichi benchmark with `--riichi-threshold-source train-best`. The train sweep
  selected threshold 0.25; `riichi_linear_calibrated` scored 0.6293 eval accuracy, 0.6534 balanced
  eval accuracy, 0.5684 pass recall, and 0.7384 riichi recall. The raw linear and weighted metrics
  matched the previous 500-log run, and the eval diagnostic best remained threshold 0.20.
- Added stable `row_id` values to decision snapshot rows and `decision-snapshot-compare` for neutral
  snapshot/prediction JSONL comparison. The comparator reports exact-action accuracy by decision
  type plus binary call/riichi metrics, and counts missing, malformed, and duplicate prediction
  rows.
- Extended `benchmark-call` with `--profile-stages`, `--feature-cache PATH`,
  `--example-limit-strategy prefix|balanced`, and `--epochs 0`. The feature cache stores prepared
  call examples behind a key that includes selected/train/eval example signatures so stale
  equivalent-count slices cannot be reused silently.
- Corrected the balanced call limiter to reserve roughly half the cap for non-pass calls and half
  for pass examples, filling from the other class only when one side runs out.
- Ran the corrected 500-log balanced call diagnostic on 10,000 of 69,013 call examples. The first
  run spent 37.8s in v1 feature preparation and 77.9s wall-clock total; the cache-hit repeat
  recorded `feature_cache_hits: v1` and finished in 10.6s. On this roughly even cap, 5-epoch
  `call_linear_v1` collapsed to pass-only: 0.5125 eval accuracy, 0.5000 balanced eval accuracy,
  1.0000 pass recall, and 0.0000 call recall. Treat the next call step as training/calibration
  quality on cached balanced caps, not just runtime.
- Re-ran the 500-log riichi benchmark on split `tenhou-500-v1` with train-best calibration. The
  train sweep again selected threshold 0.25; `riichi_linear_calibrated` scored 0.6420 eval
  accuracy, 0.6495 balanced eval accuracy, 0.6204 pass recall, and 0.6785 riichi recall. This
  initially supported checking 0.25 further, but later 500-log seed validation superseded it in
  favor of train-best thresholds rather than a fixed 0.25 policy.
- Refreshed `README.md` to describe the repo as implemented: tested Tenhou parsing,
  discard/call/riichi supervised baselines, local-only reports, neutral decision snapshots, the
  PyTorch discard MLP smoke path, and current blockers.
- Narrowed package support to Python `>=3.11,<3.14`, made `torch` a core dependency, added a `dev`
  extra with `ruff`, and added GitHub Actions CI for Python 3.11, 3.12, and 3.13 running install,
  unittest, compileall, and ruff.
- Changed balanced call limiting from grouped calls-then-passes to deterministic call/pass
  interleaving and added tests proving exact caps plus cache-signature invalidation when selected
  order changes.
- Added `produce-decision-predictions` as a protocol-level stub producer for snapshot JSONL. It
  supports `pass`, `first-legal`, and `echo-actual` strategies for comparator tests only; real
  Mortal inference remains blocked on legally usable weights and a subprocess/data boundary.
- Extended Tenhou terminal parsing for `AGARI` and `RYUUKYOKU` `sc` score-change fields and added
  outcome helpers exposing per-seat score deltas plus win, deal-in, and draw flags. Decision
  snapshots remain inference-safe by default; outcome labels require `--include-outcome`.
- Added `train-discard-mlp`, a minimal PyTorch masked-logit discard baseline over normalized
  hand-count and visible-count tensors with deterministic seed, CPU/MPS/CUDA/auto device selection,
  JSON reports, and fixture smoke tests.
- Reran the real 500-log balanced call grid over epochs `{5,15,30}`, learning rates
  `{0.1,0.05,0.02}`, and positive weights `{1.0,1.5,2.0}` with explicit weighted reports and v1
  feature cache hits. Best train-selected calibrated 10k policy: 30 epochs, LR 0.05, threshold
  0.40, eval balanced accuracy 0.7558, pass recall 0.7404, call recall 0.7713. Best weighted
  comparison: 30 epochs, LR 0.1, weight 2.0, balanced 0.7601, pass recall 0.7687, call recall
  0.7515.
- Scaled the call check to a 20k balanced cap after 10k no longer collapsed. The 20k cache-build
  run spent 267.5774s in `feature_prepare_v1`; the cache-hit rerun loaded features in 0.7491s.
  The 20k train-selected calibrated policy selected threshold 0.50 and scored 0.7289 balanced eval
  accuracy, 0.6979 pass recall, and 0.7598 call recall.
- Rechecked riichi on 500-log split seeds `tenhou-500-v0` through `tenhou-500-v3`. Train-best
  thresholds were 0.35, 0.45, 0.35, and 0.40 with balanced eval accuracy 0.6737, 0.6608, 0.6738,
  and 0.6533. Fixed threshold 0.25 raised riichi recall but hurt pass recall and balanced accuracy
  on most splits, so it is no longer the current balanced baseline.

### 2026-06-08

- Added `benchmark-call --example-cache` for local-only reconstructed `CallExample` JSON caching
  before feature preparation.
- The call example cache key uses command input paths, resolved XML file paths, per-file size and
  `mtime_ns`, and `--skip-errors`. Source metadata is intentionally report-only and does not
  invalidate cached examples.
- Call reports now include an additive `example_cache` block with path, hit/write status, loaded
  example count, and a compact cache-key summary.
- Added fixture tests for call-example serialization, cache hit reuse without reparsing, applying
  `--example-limit` after cache load, and invalidation after source XML changes.
- Extended `train-discard-mlp` reports with per-epoch train/eval metrics, deterministic best-epoch
  selection, and optional best-checkpoint artifacts via `--checkpoint`.
- Added fixture tests for MLP history, zero-epoch metric recording, checkpoint payloads, and CLI
  report/checkpoint consistency. PyTorch-specific tests are skipped when `torch` is unavailable.

### 2026-06-09

- Added `benchmark-discard-mlp`, which trains a small PyTorch discard MLP and the frequency,
  risk-context linear, and defense-context linear anchors on the same deterministic split.
- Added `kenjaku-discard-mlp-benchmark-report-v0` reports with final MLP metrics, best-epoch
  validation metrics, checkpoint paths, and MLP eval-accuracy deltas against the anchor models.
- Extended `benchmark-report-summary` to summarize standalone MLP training reports and MLP
  benchmark reports, including final/best loss and accuracy.
- Extended call benchmark summaries with a report-local `selected_policy`, ranked by eval balanced
  accuracy, call recall, pass recall, then eval accuracy. This is a comparable-report selector,
  not a global performance claim.
- Added `run-external-prediction-producer`, a generic subprocess boundary that passes
  `KENJAKU_SNAPSHOTS` and `KENJAKU_PREDICTIONS` to an external producer, then validates prediction
  JSONL and can write a comparison report.
- Updated README, local evaluation, external-baseline, and handoff docs. No real 500-log reports
  were regenerated in this workspace because ignored raw Tenhou data and run artifacts were absent.

### 2026-06-11

- Added `kenjaku status`, a text/JSON CLI status command backed by `kenjaku.status` that makes the
  current implementation boundary explicit: offline research toolkit, no bundled trained model, no
  transformer policy, no RL self-play, no Sanma ruleset, no browser demo, and no live ladder
  automation. The status payload also reports the current Python version, whether it falls inside
  the supported `>=3.11,<3.14` range, PyTorch availability, and ignored local artifact presence.
- Added CLI tests for the status command and updated the quickstart to include it.
- Added the missing MIT `LICENSE` file so the repository matches the package metadata and GitHub can
  detect the license.
- Added a dependency-free heuristic defense risk scorer over discard examples. It ranks candidate
  discard danger against active riichi opponents and reports safety/danger reasons, but remains
  explicitly uncalibrated and does not complete the trained deal-in probability estimator item yet.
- Extended discard disagreement records and example rendering with heuristic defense-risk payloads
  for the actual discard and model predictions, so tag-filtered disagreement review can inspect
  risk magnitude and reasons without adding another discard feature profile.
- Added `defense-risk-summary`, a fixture-safe/local-data CLI report for aggregate heuristic
  discard-risk inspection. It emits text, JSON, and optional report artifacts while preserving the
  explicit uncalibrated-probability boundary.
- Extended defense-risk summaries with terminal outcome analysis using existing `RoundOutcome`
  labels. The report now separates eventual deal-in/no-deal-in risk distributions, including
  active-riichi subsets, as a diagnostic correlation check rather than a causal or calibrated
  probability claim.
- Rechecked GitHub repository state: there are no open issues or PRs as of 2026-06-11. Recent
  `main` CI runs are still failed before normal test execution, so local verification remains the
  meaningful code signal until the external Actions/account setup is fixed and rerun.
- Added direct ron-discard `DealInExample` labels from terminal `AGARI` events. Positive labels are
  only the last discard by the ron source immediately before the win; earlier discards by the same
  eventual deal-in player remain negative examples.
- Added `deal-in-linear-v0`, a small dependency-free logistic estimator for direct deal-in
  probability over actual discard features, heuristic risk, active-riichi context, and tile-safety
  signals. This is a training/evaluation path, not a bundled trained model.
- Added `benchmark-deal-in` for fixture-safe/local-data reports with label-stratified splitting,
  train/eval Brier/log-loss/classification metrics, and a separately labeled uncalibrated heuristic
  risk baseline. The fixture smoke has only four labeled examples and one positive, so it is a
  command-contract check rather than evidence that the defense scorer item is complete.
- Extended `benchmark-report-summary` to read `kenjaku-deal-in-benchmark-report-v0` artifacts and
  print model-vs-heuristic eval deltas for Brier score, log loss, accuracy, and balanced accuracy.
  This gives the next ignored-slice validation step a comparable report view instead of manual JSON
  inspection.
- Added report-only threshold calibration to `benchmark-deal-in` artifacts. Reports now include
  train/eval sweeps over the standard calibration thresholds plus best-threshold summaries in
  `benchmark-report-summary`. This checks off the narrow deal-in benchmark calibration subtask, but
  does not validate or bundle a trained deal-in probability estimator.
- Added basic live-wall exhaustive-draw tenpai/noten settlement metadata to the sandbox. Normal
  wall exhaustion now records tenpai/noten seats, applies the standard 3,000-point noten pool to
  terminal point deltas and final points, and exposes the metadata in self-play reports. This
  checks off the narrow exhaustive-draw payment metadata subtask, but does not complete full round
  progression, abortive draw handling, exact platform scoring, or the Phase 3 self-play harness.
- Replaced the remaining broad honor-triplet yakuhai approximation with a basic
  dragon/round-wind/seat-wind filter. The sandbox now uses explicit round-wind state and
  dealer-relative seat winds, and guest wind triplets no longer create yaku. This checks off the
  narrow yakuhai filtering subtask, but does not complete full yaku/fu validation, full round
  progression, exact continuation rules, or scoring.
- Added a basic sandbox next-round transition helper. `SandboxEnvironmentState` now carries an
  explicit `dealer_seat`, and `next_round_sandbox_environment` creates a fresh shuffled round after
  real terminal states while preserving the point ledger, carrying riichi sticks through exhaustive
  draws, rotating/repeating dealer, and updating honba for basic win/draw outcomes. This checks off
  the narrow next-round transition subtask, but does not complete full round/end-of-game
  progression, placement/return handling, abortive draws, exact platform continuation rules, or the
  Phase 3 self-play harness.
- Added explicit sandbox round-wind state and basic dealer-wrap progression. Round wind now appears
  in sandbox payloads/self-play reports, yakuhai filtering uses `state.round_wind`, and
  `next_round_sandbox_environment` advances the round wind when dealer rotation wraps back to seat
  0. This checks off the narrow round-wind progression subtask, but does not complete end-of-game
  placement/return handling, exact hanchan continuation rules, abortive draws, or the Phase 3
  self-play harness.
- Added `mahjong-transformer-encoder-v0`, a PyTorch fixed-token state encoder over hand, visible,
  unseen, dora-indicator, riichi, seat, dealer, and score signals, plus an untrained masked discard
  policy head and tensor dataset helpers. This checks off the Phase 2 encoder implementation item,
  but behavior cloning, trained transformer policy metrics, and cloud training remain open.
- Added `train-discard-transformer`, a PyTorch supervised discard behavior-cloning command around
  `discard-transformer-policy-v0`. It emits `kenjaku-discard-transformer-report-v0` reports,
  optional best-checkpoint artifacts, and `benchmark-report-summary` output. The Phase 2 behavior
  cloning item remains unchecked because this workspace has no ignored Tenhou Phoenix/raw slice and
  no real transformer training report yet.
- Added `benchmark-discard-transformer`, which trains the transformer and frequency/risk/defense
  anchors on one deterministic split, then emits
  `kenjaku-discard-transformer-benchmark-report-v0` with transformer-vs-anchor eval accuracy
  deltas. This gives the Phase 2 behavior-cloning work a comparable report target once ignored raw
  Tenhou data is restored.
- Added `replay-intake-review`, a permission-aware replay manifest review command. It reads
  `kenjaku-replay-manifest-v0`, rejects unknown/denied or out-of-scope replay uses, writes
  `kenjaku-replay-intake-review-v0` reports, and can emit accepted replay queue rows as JSONL. This
  checks off the Phase 4 permission-aware replay ingestion/review item in the narrow offline
  manifest-gate sense; it deliberately does not fetch live-service data or automate clients.
- Added `replay-share-plan`, which consumes accepted replay intake JSONL and builds
  `kenjaku-replay-share-plan-v0` reports for `demo` or `redistribution` intent. It blocks rows whose
  original intended uses or permission scope do not cover the requested share intent. The Phase 4
  auto-replay-sharing item remains unchecked because this command plans permitted offline sharing
  only; it does not post URLs, upload files, or call platform APIs.
- Added `self-play-sandbox`, a deterministic offline four-seat draw/discard turn-rotation harness
  that can emit `kenjaku-self-play-sandbox-report-v0` reports and optional synthetic trajectories.
  It has random/drawn/frequency discard policies and updates simple discard counts for plumbing
  tests. Phase 3 self-play remains unchecked because this sandbox has no full riichi legality,
  calls, yaku validation, scoring, PPO, or population training.
- Added basic closed-hand winning-shape detection for standard hands, chiitoitsu, and kokushi, plus
  `self-play-sandbox --stop-on-tsumo` terminal metadata. This advances terminal-outcome plumbing but
  still leaves full yaku validation, open-hand legality, ron/furiten, scoring, and the Phase 3
  full-rules self-play harness open.
- Extended `self-play-sandbox` with `--ruleset tenhou-3p`, using the existing `TENHOU_3P` static
  tile-set facts to run three seats and exclude 2m-8m from the wall. This is Sanma plumbing only;
  the Phase 5 Sanma ruleset remains unchecked because actual 3-player gameplay, full call
  semantics, scoring, training, and evaluation are not implemented. Basic Kita/pei-nuki support was
  added later as a separate sandbox slice.
- Added `kenjaku-sandbox-environment-v0`, a reusable immutable sandbox environment boundary with
  deterministic initial state, draw transitions, legal discard actions, discard transitions, and
  terminal metadata. `self-play-sandbox` now runs through this boundary instead of directly mutating
  wall/hand lists. This advances Phase 3 simulator plumbing, but the self-play harness remains
  unchecked until legal calls/ron/tsumo, yaku/scoring, and a real training loop exist.
- Extended `kenjaku-sandbox-environment-v0` with legal closed-hand tsumo action generation,
  explicit tsumo application, and simple zero-sum terminal reward payloads. This removes the first
  terminal-action shortcut from the sandbox, but did not yet add ron/call legality, yaku
  validation, real scoring, or PPO-ready self-play.
- Extended `kenjaku-sandbox-environment-v0` with a pending-discard reaction window, legal
  closed-hand ron action generation, explicit ron application, and simple zero-sum ron rewards.
  `self-play-sandbox` auto-passes those reaction windows because it still has no ron/call policy.
  At that point this was useful simulator plumbing, but still not furiten, calls, yaku validation,
  real scoring, or PPO-ready self-play.
- Extended `kenjaku-sandbox-environment-v0` with legal chi/pon/minkan call action generation,
  explicit call application, open meld tracking, post-call discard obligation, and a simple live-wall
  replacement draw for minkan. `self-play-sandbox` still auto-passes reaction windows, so this is
  environment plumbing only; it is not call-policy learning, dead-wall/kan-dora/rinshan semantics,
  yaku validation, scoring, or PPO-ready self-play.
- Extended the sandbox reaction window with individual reaction passes and ron-priority call gating:
  calls are blocked while any pending reaction seat has a legal closed-hand ron. This moves call/ron
  timing closer to real play, but still does not implement furiten, yaku validation, real scoring,
  or a learned call/ron policy.
- Extended sandbox ron resolution to accept multiple simultaneous ron declarations, preserve
  `winner_seats` and per-seat winning shapes in terminal payloads, and assign simple multi-ron
  sandbox rewards. At that point this was still not temporary/riichi furiten, yaku validation,
  honba/riichi-stick payment handling, real scoring, or a learned call/ron policy.
- Added sandbox discard history and a discard-furiten ron filter. Legal ron actions now disappear
  when any current winning wait type is present in that player's own discard history, and explicit
  ron application rejects the same state. This covers the permanent own-discard furiten case only;
  at that point temporary furiten after passing a win, riichi furiten, yaku validation, and scoring
  remained open.
- Added temporary ron-pass furiten to the sandbox. Passing a currently legal ron adds that seat to
  `temporary_furiten_seats`, suppresses later ron actions, rejects explicit ron application while
  active, and clears when the seat next draws. At that point riichi furiten, yaku validation, and
  scoring remained open because the sandbox still had no riichi declaration/state or points model.
- Added a seeded riichi-furiten filter to the sandbox. States can now mark `riichi_seats`, and
  passing a legal ron while in riichi adds the player to `riichi_furiten_seats`, suppressing and
  rejecting future ron attempts for the hand. At that point this was still not full riichi
  declaration legality, riichi stick accounting, yaku validation, or scoring.
- Added a basic sandbox riichi declaration action. `legal_riichi_actions` and
  `apply_riichi_action` allow a closed current seat to declare riichi after drawing when at least
  one discard leaves a 13-tile tenpai hand. At that point this was still not riichi stick
  accounting, ippatsu, post-riichi discard locking, yaku validation, or scoring.
- Added basic post-riichi sandbox action restrictions. `riichi_pending_discard_seats` preserves the
  declaration discard as a flexible tenpai-preserving choice, later riichi turns are locked to a
  tsumogiri discard of the drawn tile, and riichi seats cannot chi/pon/minkan opponent discards.
  At that point this was still not riichi stick accounting, ippatsu, closed-kan exceptions after
  riichi, yaku validation, or scoring.
- Added basic sandbox riichi deposit accounting. `SandboxEnvironmentState` now carries a point
  ledger and riichi-stick pool, riichi declaration requires and subtracts a 1000-point deposit,
  terminal tsumo/ron transfers the current stick pool to the first recorded winner, and
  `self-play-sandbox` episode summaries expose final points and stick count. At that point this was
  still not hand scoring, honba, real multi-ron payment semantics, ippatsu, closed-kan exceptions
  after riichi, yaku validation, or a calibrated reward model.
- Added basic sandbox honba bonus accounting. `SandboxEnvironmentState` now carries `honba`, ron
  wins apply 300 points per honba from the discarder to each winner, tsumo wins apply 100 points per
  honba from each loser to the winner, and `self-play-sandbox` reports the honba count. This is
  still not hand scoring, full round-continuation semantics, real multi-ron payment validation,
  yaku validation, or a calibrated reward model.
- Added basic sandbox ippatsu window tracking. Riichi declaration marks the seat as ippatsu-active,
  calls clear all active ippatsu windows, the riichi player's next post-declaration discard clears
  their own window, and terminal tsumo/ron records `winning_ippatsu_seats`. This is still not yaku
  validation, ippatsu scoring, closed-kan exception handling after riichi, or full rules timing.
- Added basic sandbox closed-kan/ankan actions. `legal_ankan_actions` and `apply_ankan_action`
  allow a non-riichi current seat to declare a self-turn concealed kan after drawing, consume four
  matching concealed tiles into an `ANKAN` meld, clear active ippatsu windows, and take a simple
  live-wall replacement draw. This is still not dead-wall, kan-dora, rinshan, post-riichi
  closed-kan exception handling, open-hand win validation, yaku validation, or scoring.
- Added basic sandbox added-kan/kakan actions. `legal_kakan_actions` and `apply_kakan_action`
  allow a non-riichi current seat with an existing pon to promote that pon into a `KAKAN` meld
  after drawing, consume the fourth tile from the concealed hand, clear active ippatsu windows, and
  take a simple live-wall replacement draw. This is still not dead-wall, kan-dora, rinshan,
  robbing-kan/chankan reaction timing, post-riichi closed-kan exception handling, open-hand win
  validation, yaku validation, or scoring.
- Added a basic kakan chankan reaction window. When a kakan tile completes another player's
  currently legal ron shape under the existing shape/furiten filters, `apply_kakan_action` now
  pauses before replacement draw with `pending_chankan_tile`, `pending_chankan_seat`, and legal
  `RON`/`PASS` chankan reactions. Passing all chankan reactions performs the delayed simple
  live-wall replacement draw; ron in that window terminates with `terminal_reason="chankan"`.
  This is still not full chankan semantics, yaku validation, kokushi-only ankan robbery, kan-dora,
  dead-wall/rinshan handling, scoring, or a learned kan/chankan policy.
- Added basic sandbox dead-wall replacement draws and kan-dora indicator metadata. Initial sandbox
  states now reserve a 14-tile `dead_wall`, expose `dead_wall_remaining` and visible
  `dora_indicators` in state/self-play payloads, draw minkan/ankan/kakan replacement tiles from the
  dead wall, and reveal the next simple kan-dora indicator when another hidden indicator is
  available before the replacement draw. This replaces the earlier live-wall shortcut, but remains
  incomplete: it is not full rinshan handling, complete kan-dora/ura-dora ordering, yaku validation,
  scoring, post-riichi closed-kan exceptions, or a learned kan/chankan policy.
- Added basic sandbox rinshan draw-source metadata. Normal wall draws clear `rinshan_draw`,
  minkan/ankan/kakan replacement draws set it, discards clear it, self-play episode summaries expose
  the transient flag plus `winning_rinshan_seats`, and tsumo terminal metadata records a winner when
  the current draw is marked as a replacement draw. This is still metadata only: full yaku-aware
  open/kan hand legality, full rinshan yaku/scoring, complete kan-dora/ura-dora ordering,
  post-riichi closed-kan exceptions, and learned kan/chankan policy remain open.
- Added basic open/kan standard-shape win detection to the sandbox environment. Legal tsumo, ron,
  chankan ron, wait-type/furiten checks, and rinshan replacement draws now validate standard hands
  by combining concealed tiles with each existing meld as one completed group. Chiitoitsu and
  kokushi remain closed-hand-only, and this still does not implement yaku legality, open-hand yaku
  restrictions, scoring, complete rinshan yaku/scoring semantics, or learned call/ron/kan policy.
- Added kokushi-only ankan chankan to the sandbox. Pending chankan windows now carry a
  `pending_chankan_kind`, kakan keeps the existing standard-shape chankan behavior, and ankan opens
  a ron/pass window only for seats whose hand wins by kokushi on the concealed-kan tile under the
  existing furiten filters. At that point this was still not complete robbing-kan/chankan
  semantics, complete yaku validation, post-riichi closed-kan exception handling, scoring, or a
  learned kan/chankan policy.
- Added basic wait-preserving post-riichi closed-kan exceptions to the sandbox. A riichi player can
  now declare an ankan only when the kan uses the just-drawn tile and the winning wait set before
  the kan exactly matches the wait set after adding the concealed-kan meld. This is still not full
  riichi/kan timing, yaku validation, scoring, or a learned kan policy.
- Added a basic sandbox yaku filter and terminal yaku metadata. Legal tsumo/ron/chankan now require
  at least one recognized sandbox yaku from a deliberately small set: kokushi, chiitoitsu, riichi,
  ippatsu, menzen tsumo, rinshan, chankan, tanyao, or a basic yakuhai filter.
  Terminal states now report `winning_yaku` and `winning_yaku_by_seat`. This is still not complete
  yaku validation, full open-hand yaku rules, complete bonus-han treatment, fu/han scoring, or
  calibrated reward semantics.
- Added terminal point-delta metadata to the sandbox. Terminal win states now expose
  `terminal_point_deltas` derived from the existing riichi-stick and honba point ledger, while
  wall-exhaustion and max-turn terminals expose neutral zero deltas. This is still not complete
  payment accounting, base hand scoring, point-based reward scaling, or a calibrated RL reward
  model.
- Added a basic sandbox score-estimate slice. Terminal wins now expose `terminal_score_estimates`
  with han/fu/limit metadata and use rounded score-derived ron/tsumo point transfers alongside
  riichi-stick and honba deltas. Later work added basic dealer-aware win payments. Still not done:
  point-based reward scaling, complete Sanma payment differences beyond basic tsumo-loss,
  ura-dora treatment, kiriage and kazoe handling, exhaustive yaku/fu validation, and full
  validation across every win/timing path.
  Do not treat this as complete scoring.
- Added basic dealer-aware sandbox win payment estimates. Terminal win point updates now mark
  whether each winner is dealer, use dealer ron multipliers, split nondealer tsumo payments between
  dealer and child losers, and expose `tsumo_child_payment`/`tsumo_dealer_payment` in score-estimate
  payloads. This checks off the narrow dealer-aware payment subtask, but does not complete exact
  scoring, complete Sanma payment differences beyond basic tsumo-loss, kiriage/kazoe, complete
  dora handling, exhaustive yaku/fu validation, or calibrated point-based rewards.
- Added basic visible-dora score-estimate bonus han. Terminal win score estimates now count current
  visible dora indicators over the same concealed-plus-meld tile view used for sandbox yaku metadata,
  expose `visible_dora_count`, and include it in bonus han alongside Kita. This checks off the narrow
  visible-dora score-estimate subtask, but does not complete ura-dora, exact dora ordering,
  kiriage/kazoe, exhaustive yaku/fu validation, or calibrated point-based rewards.
- Added basic red-five score-estimate bonus han. Terminal win score estimates now count ruleset
  supported red fives in the winning tile view, expose `red_dora_count`, and include it in bonus han
  alongside visible dora and Kita. This checks off the narrow red-dora score-estimate subtask, but
  does not complete ura-dora, exact dora ordering, kiriage/kazoe, exhaustive yaku/fu validation, or
  calibrated point-based rewards.
- Fixed tsumo yaku/dora score-estimate tile views to avoid duplicating the drawn tile. Tsumo yaku
  checks and terminal score estimates now use the already-complete hand, while ron/chankan still add
  the pending winning tile. This checks off the narrow tsumo tile-view de-duplication subtask and
  prevents drawn pair tiles from becoming false yakuhai triplets or drawn dora tiles from being
  counted twice, but it does not complete full yaku/fu validation or exact scoring.
- Added a basic Sanma Kita/pei-nuki sandbox action. `legal_kita_actions` and `apply_kita_action`
  expose North only under `tenhou-3p`, record exposed North tiles in `kita_tiles` instead of melds,
  clear active ippatsu windows, take a dead-wall replacement draw without revealing a kan-dora
  indicator, allow post-riichi Kita only for a drawn North, and count exposed Kita as bonus han in
  sandbox score estimates. This did not complete the Phase 5 Sanma ruleset: ron-on-Kita, exact
  platform timing, full Sanma scoring, Kita ura-dora treatment, call semantics, training data, and
  evaluation remained open.
- Added a basic Sanma Kita ron/pass reaction window. A `KITA` action now records pending Kita
  reactions before the dead-wall replacement draw when an opponent has a legal North ron, exposes
  only `RON`/`PASS` reactions, resolves Kita ron as normal `terminal_reason="ron"` without chankan
  yaku, and performs the delayed replacement draw without revealing kan-dora after passes. This
  checks off the narrow ron-on-Kita sandbox subtask, but does not complete exact platform timing,
  full Sanma scoring, Kita ura-dora treatment, call semantics, training data, or evaluation.
- Added Tenhou Sanma initial points to the sandbox. `initial_sandbox_environment` and default
  payload point views now use 35,000 points per seat for `tenhou-3p` while preserving 25,000 for
  `tenhou-4p`, matching Tenhou's 3-player ranked rule note of 35,000 start / 40,000 return. This
  checks off the narrow Sanma start-point subtask, but does not complete Sanma placement/return
  scoring, uma/oka, payment differences, round progression, training data, or evaluation.
- Added Tenhou Sanma no-chi call filtering to the sandbox. `legal_call_actions` now suppresses chi
  candidates under `tenhou-3p` while preserving legal pon and minkan reactions, matching Tenhou's
  3-player rule note that chi is unavailable. This checks off the narrow Sanma no-chi call subtask,
  but does not complete pon-after-Kita timing, exact platform call timing, full call policy,
  scoring, training data, or evaluation.
- Added Tenhou Sanma North guest-wind yaku filtering to the sandbox. The broad sandbox yakuhai
  approximation now excludes North triplets under `tenhou-3p`, matching Tenhou's rule note that
  North used inside a hand is otakaze. This checks off the narrow North-as-guest-wind yaku subtask,
  but does not complete seat/round wind specificity, full yaku/fu validation, scoring, training
  data, or evaluation.
- Added Tenhou Sanma 1m/9m dora indicator wrapping to sandbox visible-dora score estimates.
  `tenhou-3p` now maps a 1m indicator to 9m and a 9m indicator to 1m instead of pointing 1m at the
  removed 2m tile, matching Tenhou's three-player rule note. This checks off the narrow Sanma dora
  wrap subtask, but does not complete exact dead-wall dora ordering, ura/kan-ura handling, full
  Sanma scoring, training data, or evaluation.
- Added basic Tenhou Sanma post-pon Kita suppression and fixed post-call action listing.
  `legal_sandbox_actions` now returns the required discard choices during a post-call discard
  obligation instead of probing draw-only actions, and a North in hand is not exposed as a Kita
  action immediately after pon. This checks off the narrow post-pon Kita timing subtask, but does
  not complete exact platform call timing, full call handling, Sanma scoring, training data, or
  evaluation.
- Added basic Tenhou Sanma Kita ippatsu reaction timing. A pending ron on a called North now keeps
  active ippatsu available for the reacting winner, while passing the Kita reaction clears ippatsu
  when the replacement draw proceeds. This checks off the narrow Kita ippatsu timing subtask, but
  does not complete exact platform call timing, complete Chiihou/Kyuushu/Double Riichi interruption
  semantics, full Sanma scoring, training data, or evaluation.
- Added basic Tenhou Sanma tsumo-loss payment estimate coverage. The existing sandbox point loop
  now has direct regression coverage for dealer and nondealer `tenhou-3p` tsumo wins, where only
  the two real opponents pay. This checks off the narrow tsumo-loss payment subtask, but does not
  complete exact Sanma scoring, placement/return handling, pao, complete yaku/fu validation,
  training data, or evaluation.
- Added a basic Tenhou Sanma eight-rinshan replacement reserve cap. Full `tenhou-3p` dead-wall
  states now stop replacement draws once only the six non-replacement dead-wall tiles remain,
  matching the narrow "8 replacement tiles" rule while preserving short synthetic dead-wall
  fixtures. This checks off the narrow replacement-reserve subtask, but does not complete exact
  dead-wall layout, dora/ura ordering, kan-dora timing, full Sanma scoring, training data, or
  evaluation.
