# kenjaku — Open-Source Riichi Mahjong AI

> An open-source riichi mahjong agent targeting Tenhou's Phoenix room and Mahjong Soul's Celestial rank, with modern architecture, full reproducibility, and a public ladder presence.

---

## 0. tl;dr

- **What**: Train and deploy an open-source riichi mahjong AI that climbs to top-tier ranks on Tenhou (Phoenix room, top ~0.1% of players) and Mahjong Soul (Saint+ / Celestial).
- **Why now**: Microsoft Suphx is the SOTA closed-source bot (Tenhou 10-dan, 2019). NAGA is closed. The strongest open-source bot (`mortal`) reaches stable 7-8 dan but has a dated architecture (CNN+MLP), limited interpretability, no Sanma support, no live ladder presence.
- **Differentiation**: Modern transformer architecture + improved RL pipeline + Sanma (3-player) extension + interpretability overlay + always-on public ladder bot with auto-shared replays.
- **Hardware**: Mac (M-series MPS) primary; ~$200-500 cloud GPU bursts (Lambda/RunPod) for final RL training passes.
- **Timeline**: 4-6 months solo part-time to credible launch.
- **Outputs**: Open-source repo, arxiv preprint, public ladder bot, browser-playable demo, HN/Twitter launch.

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

### 3.5 Live ladder bot
- Daily automated runs on Mahjong Soul ladder
- Auto-post replay URLs to a public Twitter/X account
- Public-facing rank tracker on a website
- "AI is currently ranked Saint 3" creates continuous engagement
- Risk: TOS — must be transparent about being a bot, may need MJS approval

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
- External: deploy on Tenhou (general room first, then push to Phoenix)
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
| Bot harness | Playwright (Mahjong Soul automation) | Web-based MJS |

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
- [ ] Mahjong Soul automation via Playwright
- [ ] Auto-replay-sharing pipeline (post replay URLs to public site/Twitter)
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
   - Tenhou Phoenix room performance
   - Mahjong Soul Saint+ ranking
   - Ablations: transformer vs CNN, with vs without aux heads, sanma transfer
6. **Discussion**: Limitations, ethical considerations (TOS, fairness)
7. **Conclusion + future work**

### contribution claims (defensible)
- First open-source riichi mahjong agent to formally evaluate on Tenhou Phoenix and Mahjong Soul Saint+
- First transformer-based architecture for riichi mahjong
- First open Sanma agent
- Interpretability overlay novel in mahjong AI literature
- Reproducible training pipeline released

---

## 8. risks and open questions

### 8.1 TOS and bot policy
- **Tenhou**: explicitly bans bots in PvP rooms. Past bots (including Suphx) had access rescinded. Likely cannot deploy live on Tenhou.
- **Mahjong Soul**: less clear. Some Japanese players run bots, MJS has not aggressively pursued. Risk of account bans.
- **Mitigation**: Build the bot for both, deploy on whichever permits it. Always disclose. Consider reaching out to MJS for permission. Worst case: train on Tenhou logs (offline), evaluate via simulator + manual ladder pushes.

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

1. **Tenhou log access**: are Phoenix room logs still publicly downloadable? what's the latest year covered?
2. **mortal architecture details**: read the code, confirm CNN baseline, understand training pipeline
3. **Mahjong Soul automation legality**: research current state of MJS bot policy, find precedents
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
- Tenhou log archive: various community mirrors (verify in phase 0)
- pymahjong / mjx (Python mahjong simulators): https://github.com/mahjong-py/mjx (verify still maintained)

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

1. Download 100 sample Tenhou Phoenix logs, verify parser works
2. Clone and build mortal locally, confirm it runs on Mac
3. Create Mahjong Soul account, play 10 games to relearn the UI
4. Spin up a Lambda Labs account, do a smoke-test H100 run ($2 budget)
5. Reserve GitHub repo + domain name + twitter handle for the project
6. Read Suphx paper end-to-end, extract reusable ideas
7. Draft README.md skeleton (this file's structure → public README)
