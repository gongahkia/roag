# Session Handoff

Last updated: 2026-06-12.

## Stop State

- Last work slice: refreshed README/docs, narrowed Python support to `>=3.11,<3.14`, added
  PyTorch as a core dependency, added GitHub Actions CI, fixed balanced call limiting to
  deterministic call/pass interleaving, added stub decision prediction production, added opt-in
  outcome labels, added a minimal PyTorch discard MLP, and reran local aggregate call/riichi checks
  on the ignored 500-log Tenhou slice.
- Current work slice: added `benchmark-discard-mlp` for small PyTorch discard MLP comparisons
  against frequency, risk-context linear, and defense-context linear anchors on one deterministic
  split. Extended `benchmark-report-summary` for MLP reports and report-local call policy
  selection. Added `run-external-prediction-producer` as the generic subprocess boundary for real
  prediction JSONL producers. Added `kenjaku status` backed by `kenjaku.status` so the CLI and
  scripts can report the current implementation boundary: offline research toolkit, no bundled
  trained model, no transformer policy, no RL self-play, no Sanma ruleset, no browser demo, and no
  live ladder automation. Added the missing MIT `LICENSE` file so the repository matches the
  package metadata. Added a dependency-free heuristic defense risk scorer for candidate discards
  against active riichi opponents; it is explicitly not a calibrated deal-in probability estimator.
  Added direct ron-discard labels and a dependency-free `deal-in-linear-v0` logistic estimator
  command for offline deal-in probability experiments. Added `mahjong-transformer-encoder-v0`, a
  PyTorch fixed-token state encoder plus an untrained masked discard policy head and tensor dataset
  helpers for future behavior-cloning work. Added `train-discard-transformer`, which trains that
  policy head on reconstructed discard examples and writes reports/checkpoints. Added
  `benchmark-discard-transformer`, which compares the transformer against frequency, risk-context
  linear, and defense-context linear anchors on one split. This is still not a trained transformer
  agent, and no Tenhou Phoenix transformer report exists in this workspace. Added
  `replay-intake-review`, an offline manifest-gated replay intake review command with accepted-item
  JSONL output. Added `replay-share-plan`, an offline shareability planner for accepted intake rows.
  Added `replay-public-summary`, a sanitized public-safe summary report for accepted and permitted
  demo/redistribution rows.
  Added `self-play-sandbox`, a deterministic four-seat draw/discard sandbox for turn-rotation and
  synthetic trajectory plumbing. Added basic closed-hand winning-shape detection and optional
  sandbox tsumo termination. Added static `tenhou-3p` tile-set support to the sandbox. These are
  permission/provenance gates and offline harnesses, not live-service fetchers, client automation,
  a yaku/scoring engine, full Sanma implementation, or a full RL simulator. Added
  `kenjaku-sandbox-environment-v0`, a reusable immutable environment boundary for deterministic
  initial state, draw, legal-discard, and discard transitions; `self-play-sandbox` now uses it.
  Extended that boundary with legal closed-hand tsumo actions, explicit tsumo application, and
  simple zero-sum terminal reward payloads. Added a pending-discard reaction window with legal
  closed-hand ron action generation, explicit ron application, and simple zero-sum ron rewards.
  Added legal chi/pon/minkan call action generation, explicit call application, open meld tracking,
  post-call discard obligation, and later replaced kan live-wall shortcuts with basic dead-wall
  replacement draws plus kan-dora indicator metadata. Added basic rinshan draw-source metadata:
  normal wall draws clear `rinshan_draw`, kan replacement draws set it, discards clear it, and tsumo
  terminal metadata records `winning_rinshan_seats` when the current draw is marked as a
  replacement draw. Added basic open/kan standard-shape win detection by combining concealed tiles
  with each existing meld as one completed group for tsumo, ron, chankan ron, wait, and furiten
  checks. Added kokushi-only ankan chankan by tagging pending chankan windows with
  `pending_chankan_kind`; kakan keeps standard-shape chankan behavior, while ankan opens ron/pass
  only for kokushi wins on the concealed-kan tile. Added basic wait-preserving post-riichi
  closed-kan exceptions: a riichi player can ankan only the just-drawn tile, and only when the
  before/after wait sets match. Added a basic sandbox yaku filter and terminal yaku metadata:
  legal tsumo/ron/chankan now require at least one recognized sandbox yaku from kokushi,
  chiitoitsu, riichi, double riichi, ippatsu, menzen tsumo, rinshan, haitei, houtei, chankan,
  tanyao, toitoi, honroutou, or a broad honor-triplet yakuhai approximation, and terminal states
  expose `winning_yaku` plus `winning_yaku_by_seat`.
  The current yakuhai filter is now narrowed to dragons, the sandbox state's round wind, and the
  winner's dealer-relative seat wind.
  Added terminal point-delta metadata: win terminals expose `terminal_point_deltas` from the current
  riichi-stick and honba point ledger, live-wall exhaustion can expose basic tenpai/noten deltas,
  and max-turn terminals expose neutral zero deltas.
  Added report-level self-play reward projections for terminal, point-delta, normalized point-delta,
  and placement-delta modes, plus per-mode aggregate summaries and win/deal-in/draw outcome
  counters.
  Together these are still not a call/ron policy, complete yaku-aware open-hand legality, complete
  rinshan yaku/scoring semantics, complete kan-dora/ura-dora indicator ordering, full riichi/kan
  timing legality, real scoring, or PPO-ready self-play. Added
  individual reaction passes and ron-priority call gating so calls are blocked while any pending
  reaction seat has legal ron.
  Added basic multi-ron terminal resolution with per-winner metadata and simple sandbox rewards.
  Added basic multi-ron turn-priority ordering for `winner_seat`, per-winner metadata, score
  estimates, and carried riichi-stick assignment.
  Added sandbox discard history plus discard-furiten ron filtering for permanent own-discard
  furiten. Added temporary ron-pass furiten that persists until that seat's next draw. Added seeded
  riichi-furiten filtering for states that already mark `riichi_seats`. Added a basic closed-tenpai
  riichi declaration action. Added basic post-riichi action restrictions: the declaration discard
  remains flexible, later riichi turns must tsumogiri the drawn tile, and riichi seats cannot
  chi/pon/minkan opponent discards. Added a basic point ledger and riichi-stick pool: declaration
  requires and subtracts a 1000-point deposit, and terminal tsumo/ron transfers the pool to the
  first recorded winner. Added basic honba bonus deltas: ron applies 300 points per honba from the
  discarder to each winner, and tsumo applies 100 points per honba from each loser to the winner.
  Added basic ippatsu window tracking: riichi declaration marks active ippatsu, calls clear active
  ippatsu windows, the riichi player's next post-declaration discard expires their window, and
  terminal tsumo/ron records winning ippatsu seats. Added basic closed-kan/ankan self-turn actions:
  non-riichi seats, plus wait-preserving post-riichi drawn-tile exceptions, can consume four
  matching concealed tiles, record an `ANKAN` meld, clear active ippatsu windows when the kan
  completes, and take a basic dead-wall replacement draw. Added basic added-kan/kakan self-turn
  actions: non-riichi seats can promote an existing pon into a `KAKAN` meld and either open a basic
  chankan ron/pass window or clear active ippatsu windows before taking a basic dead-wall
  replacement draw. Chankan pass resolution performs the delayed replacement draw; chankan ron
  terminates with `terminal_reason="chankan"`, with active ippatsu preserved for immediate
  robbing-kan ron and cleared if the kan completes after passes. Sandbox states now reserve a
  14-tile dead wall and expose visible dora/kan-dora indicator metadata plus basic rinshan
  draw-source metadata. The sandbox now detects basic open/kan standard hand shapes, including
  rinshan replacement draws after kan, and supports kokushi-only ankan robbery. Terminal win score
  estimates now include basic
  dealer-aware ron/tsumo payment handling plus visible-dora/red-five bonus han and kazoe-yakuman
  limits, suppresses bonus han on yakuman estimates, and tsumo yaku/dora tile views no longer
  duplicate the drawn tile. Added basic Nagashi mangan wall-exhaustion handling as mangan tsumo
  when every discard is terminal/honor and no own discard was called, plus win-style next-round
  progression for dealer repeat and honba. This is still not hand scoring, full
  round-continuation semantics, real
  multi-ron payment validation, complete payment accounting, complete yaku validation, full
  open-hand yaku rules, complete rinshan yaku/scoring semantics, complete kan-dora/ura-dora
  indicator ordering, complete robbing-kan/chankan semantics, complete post-riichi kan timing, or
  ippatsu scoring.
  Added a narrow Sanma Kita/pei-nuki sandbox action that records exposed North tiles separately
  from melds, takes a dead-wall replacement draw without revealing kan-dora, allows post-riichi
  Kita only on a drawn North, and counts exposed Kita as bonus han in sandbox score estimates.
  Added Tenhou Sanma's 1m/9m dora indicator wrap to visible-dora score estimates.
  Added basic post-pon Kita suppression and post-call discard-obligation action listing.
  Added basic Kita ippatsu reaction timing: ron on a called North can still carry ippatsu, and
  passing the reaction clears ippatsu before the replacement draw.
  Added basic chankan ippatsu reaction timing.
  Added basic haitei/houtei yaku metadata for explicitly final live-wall draws/discards.
  Added basic double-riichi yaku metadata for first-turn riichi before any tile call or Sanma Kita
  exposure.
  Added basic toitoi yaku metadata for all-triplet/all-kan standard hands.
  Added basic honroutou yaku metadata for terminal-and-honor standard or chiitoitsu hands.
  Added basic kazoe-yakuman score estimates for non-yakuman hands reaching 13 or more total han.
  Added basic yakuman bonus-han suppression in score estimates while preserving raw dora counts.
  Added basic Nagashi mangan wall-exhaustion handling that bypasses normal tenpai/noten settlement.
  Added basic Nagashi mangan next-round progression for the sandbox's win-style settlement path.
  Added direct coverage for basic Tenhou Sanma tsumo-loss point estimates.
  Added a basic Tenhou Sanma eight-rinshan replacement reserve cap for full dead-wall states.
  `self-play-sandbox` auto-passes reaction windows because it still has no ron/call/kan/Kita
  policy.
- Expected tracked worktree after this implementation is committed and pushed: clean.
- Do not promote `discard-linear-defense-context-v1` as the default path yet. Lowering learning
  rate fixed the largest aggregate regression, but v1 still trails risk/defense v0 on the 100-log
  slice.

## Current State

- Branch: `main`.
- License: MIT, with the canonical text in `LICENSE`.
- GitHub check on 2026-06-12: no open issues and no open PRs. Recent `main` CI runs are still
  failed, but the latest inspected run had jobs with no executed steps and no failed-job log
  available through `gh run view --log-failed`; treat that as an external Actions/account setup
  blocker until a fresh run proves otherwise. Local verification is the current code signal.
- Raw Tenhou data, generated reports, disagreement exports, model artifacts, external checkouts,
  and Mortal build outputs are local-only and ignored by git.
- `benchmark-discard` trains and scores six baselines on one deterministic split: frequency,
  raw-count linear, shanten-aware linear, risk-context linear, defense-context linear, and
  defense-context-v1 linear.
- `benchmark-discard --models` accepts `all`, `fast`, or comma-separated model names. `all` is the
  default; `fast` runs frequency, shanten-aware, risk-context, and defense-context only.
- `benchmark-discard --l2` applies L2 regularization to every linear model in the benchmark and
  records it in each training block.
- Benchmark reports now include `weight_summary` and `feature_summary` for every linear model.
- `benchmark-discard --disagreements PATH` writes local-only capped examples where risk-context and
  defense-context predictions disagree, including defense buckets, heuristic defense-risk payloads,
  and legal-candidate logits.
- `benchmark-report-summary` reads ignored discard, call, riichi, and discard-MLP benchmark
  reports. Discard summaries include ablation deltas and selected defense buckets. Call/riichi
  summaries include eval accuracy, balanced accuracy, pass/target recall, policy threshold/source,
  train/eval best threshold diagnostics, and positive class weight. Call summaries also include a
  report-local `selected_policy` ranked by eval balanced accuracy, target recall, pass recall, and
  eval accuracy. MLP summaries include final/best validation metrics and anchor deltas.
- `benchmark-dashboard` writes a static public HTML page from benchmark report JSON files. It
  publishes the Kenjaku version, dataset label/date and split, metric definitions, latest eval
  scores, selected call policy, and report/checkpoint artifact links while explicitly keeping live
  ladder rank tracking out of scope unless platform permission is granted.
- `disagreement-report-summary` reads ignored disagreement exports and summarizes category counts,
  defense bucket rates, common actual/predicted tile pairs, and logit margins. Use `--examples N`
  to append representative stored examples with defense flags and top logits. Use `--tags` to add
  deterministic stored-example tags for defense signals, efficiency-like cases, close logits,
  active riichi, and safe-tile candidates. Use `--tag TAG` with `--examples` to render only stored
  examples matching one deterministic tag.
- `benchmark-call` scores four supervised call/pass baselines from existing `CallExample`
  reconstruction data: `call-frequency-v0`, `call-legal-frequency-v0`, `call-linear-v0`, and
  additive `call-linear-v1`.
  Reports include overall accuracy, balanced accuracy, macro recall, pass/call recall, and
  per-action recall. Reports now also include `call_linear_v1_calibrated`, a fixed-threshold
  policy variant over `call-linear-v1` at non-pass threshold 0.40. Linear call payloads include
  report-only call/pass threshold calibration sweeps, and `--include-weighted` adds
  `call_linear_v1_weighted` trained with `--call-positive-weight`. `benchmark-call --models`
  accepts `all`, `fast`, or comma-separated names; `fast` runs frequency, legal-frequency,
  `call_linear_v1`, and `call_linear_v1_calibrated`. For weighted sweeps, use `all` with
  `--include-weighted` or explicitly include `call_linear_v1_weighted`; `fast` intentionally omits
  it. `--example-limit N` keeps bounded large-slice iteration practical.
  `--example-limit-strategy prefix|balanced` selects either a deterministic
  prefix or a roughly even call/pass cap. `--profile-stages` records stage timings,
  `--example-cache PATH` reuses reconstructed call examples before feature preparation,
  `--feature-cache PATH` reuses prepared call features across repeated runs, `--epochs 0` supports
  zero-update report smokes, and `--call-threshold-source train-best` uses the train split threshold
  sweep winner for the calibrated policy variant.
- `benchmark-riichi` scores the first conservative riichi/pass dataset from explicit Tenhou reach
  events plus closed tenpai no-riichi discard decisions. It now reports `riichi-frequency-v0` and
  `riichi-linear-v0`. Reports now also include `riichi_linear_calibrated`, a fixed-threshold policy
  variant over `riichi-linear-v0` at riichi threshold 0.95. Linear riichi payloads include
  report-only riichi/pass threshold calibration sweeps, `--include-weighted` adds
  `riichi_linear_weighted` trained with `--riichi-positive-weight`, and
  `--riichi-threshold-source train-best` uses the train split threshold sweep winner for the
  calibrated policy variant.
- Local Mortal checkout/build reconnaissance is recorded in `docs/external-baselines.md`.
- `export-decision-snapshots` writes local-only JSONL decision rows for discard/call/riichi
  examples. Rows include stable `row_id` values, Kenjaku reconstruction fields, legal actions,
  observed action, and a minimal `mjai_events` prefix for future offline comparison through a
  neutral boundary.
- `decision-snapshot-summary` reads local snapshot JSONL files and reports valid/malformed row
  counts, decision type counts, action counts, source labels, and `mjai_events` presence.
- `decision-snapshot-compare` compares snapshot JSONL against prediction JSONL rows containing
  `row_id` and `predicted_action`, reporting exact-action accuracy by decision type plus binary
  call/riichi metrics and missing/malformed/duplicate prediction counts.
- `produce-decision-predictions` writes protocol-test prediction JSONL with `pass`, `first-legal`,
  or `echo-actual` stub strategies. It is not real Mortal inference.
- `run-external-prediction-producer` executes a separate prediction producer process with
  `KENJAKU_SNAPSHOTS` and `KENJAKU_PREDICTIONS` in the environment, then validates the generated
  JSONL and can write a comparison report. Keep Mortal-specific inference outside Kenjaku behind
  this boundary unless licensing decisions change.
- `export-decision-snapshots --include-outcome` adds terminal score-delta/win/deal-in/draw labels
  parsed from Tenhou `sc` fields. Default snapshots intentionally omit outcome labels.
- `train-discard-mlp` trains a small PyTorch masked-logit discard MLP over normalized hand and
  visible-count tensors. It supports deterministic seeds, CPU/MPS/CUDA/auto device selection, and
  JSON reports with per-epoch history plus optional best-checkpoint artifacts.
- `benchmark-discard-mlp` trains the same MLP plus frequency/risk/defense anchors on one split and
  writes `kenjaku-discard-mlp-benchmark-report-v0` artifacts for direct local comparisons.
- `kenjaku status` prints a text status summary and `kenjaku status --json` emits
  `kenjaku-status-v0`. The payload reports the current Python version, whether it falls inside the
  supported `>=3.11,<3.14` range, PyTorch availability, ignored local artifact path presence,
  implemented toolkit capabilities, and deliberately missing product/model pieces. Use it to keep
  expectations honest before presenting the repo as a trained agent.
- `candidate_defense_risk` and `legal_candidate_defense_risks` provide a dependency-free heuristic
  ranker for discard danger against active riichi opponents. The returned `DefenseRiskScore.risk`
  is normalized for diagnostics and ranking only; `calibrated_probability` is always `False`.
  Disagreement records now include defense-risk payloads for the actual discard and each model
  prediction, and `disagreement-report-summary --examples` renders compact risk values.
- `defense-risk-summary` summarizes heuristic discard-risk scores over Tenhou XML. It supports
  text output, `--json`, `--report`, `--skip-errors`, and source metadata. Use it for local
  diagnostics before changing discard defense features. Reports include
  `kenjaku-defense-risk-outcome-analysis-v0`, which compares heuristic actual-discard risk against
  terminal `RoundOutcome` labels for eventual deal-in/no-deal-in and active-riichi subsets. Treat
  this as correlation diagnostics only, not causality or calibrated deal-in probability.
- `DealInExample` labels direct ron discards from terminal `AGARI` events. Positive labels are only
  the last discard by the ron source immediately before the win; earlier discards by the same
  eventual deal-in player stay negative. This is the stricter target for probability-estimator work.
- `benchmark-deal-in` trains/evaluates `deal-in-linear-v0`, a dependency-free logistic estimator
  over actual-discard defense features, heuristic risk, active-riichi context, and tile-safety
  signals. The command uses a deterministic label-stratified split, reports train/eval
  accuracy/balanced accuracy/Brier/log-loss metrics, and includes the old heuristic risk score as a
  separately labeled uncalibrated baseline. Reports now also include report-only threshold
  calibration sweeps for direct deal-in detection, and `benchmark-report-summary` reads
  `kenjaku-deal-in-benchmark-report-v0` artifacts, prints train/eval best thresholds, and shows
  model-vs-heuristic eval deltas for Brier score, log loss, accuracy, and balanced accuracy.
- `mahjong-transformer-encoder-v0` encodes fixed player-perspective state tokens covering hand
  counts, visible counts, unseen counts, dora indicators, active riichi flags, acting seat, dealer,
  and score. `discard-transformer-policy-v0` is an untrained masked-logit head on top of that
  encoder. The module is intentionally not re-exported from `kenjaku.models` so non-Torch command
  paths still import cleanly when PyTorch is absent.
- `train-discard-transformer` trains `discard-transformer-policy-v0` with supervised discard
  behavior cloning over local Tenhou XML. It supports model width/head/layer/feedforward/dropout
  flags, deterministic split/seed settings, CPU/MPS/CUDA/auto device selection, JSON reports under
  `kenjaku-discard-transformer-report-v0`, optional best-checkpoint artifacts, and
  `benchmark-report-summary` rendering.
- `benchmark-discard-transformer` trains the same transformer plus frequency/risk/defense anchors
  on one deterministic split and writes `kenjaku-discard-transformer-benchmark-report-v0` artifacts
  with eval accuracy deltas over those anchors. Use this, not standalone training reports, for the
  first comparable transformer behavior-cloning checks.
- `replay-intake-review` reads `kenjaku-replay-manifest-v0` JSON manifests and writes
  `kenjaku-replay-intake-review-v0` reports. It accepts only replay items whose `permission.status`
  and `permission.scope` cover the requested `intended_uses`, rejects unknown/denied permissions,
  rejects Tenhou redistribution, and requires explicit permission for every Mahjong Soul intended
  use after the 2026-06-12 policy review. `--accepted-output` writes accepted queue rows as JSONL
  for later offline analysis tooling.
- `replay-share-plan` reads accepted intake JSONL and writes `kenjaku-replay-share-plan-v0`
  reports for `--intent demo|redistribution`. It requires both the original `intended_uses` and
  `permission.scope` to include the requested share intent. It plans local sharing only; it does not
  post URLs, upload files, or call platform APIs.
- `replay-public-summary` reads accepted intake JSONL, reuses the same share gate, and writes
  `kenjaku-replay-public-summary-v0` reports with public-safe summaries, URI fingerprints, and
  explicit blocked reasons. It omits raw replay data, accepted queue rows, and raw replay URLs.
- `self-play-sandbox` runs deterministic synthetic draw/discard episodes across four seats and
  writes `kenjaku-self-play-sandbox-report-v0` reports. It supports `random`, `drawn`, and
  `frequency` discard policies, deterministic episode seeds, optional synthetic trajectories, and
  simple discard-count policy updates. It auto-passes the environment's pending-discard reaction
  windows because no ron/call policy is implemented in this sandbox. `--reward-mode` can select
  terminal, point-delta, normalized point-delta, or placement-delta reward vectors, and every report
  writes comparable summaries for all four modes. Episode summaries include raw point delta,
  normalized point delta, placement delta, win/deal-in event vectors, draw outcomes,
  `terminal_rewards`, final point-ledger and riichi-stick counts from the sandbox state, the current
  honba count, active ippatsu seats, winning ippatsu seats, active double-riichi seats, rinshan
  draw-source metadata, final live-wall draw metadata, winning rinshan seats, terminal yaku
  metadata, terminal point-delta metadata, dead-wall remaining count, and visible dora/kan-dora
  indicators.
  `--ruleset tenhou-4p|tenhou-3p` selects the static tile set and player count; `tenhou-3p`
  excludes 2m-8m, starts each seat at 35,000 points, rotates three seats, filters out chi call
  reactions, treats North triplets in hand as guest-wind rather than yakuhai, maps 1m/9m dora
  indicators to each other for visible-dora estimates, suppresses immediate post-pon Kita,
  preserves ippatsu for immediate ron on a called North, and the environment exposes a basic
  Kita/pei-nuki action plus Kita ron/pass reaction windows; episode summaries include `kita_tiles`
  and `kita_counts`. `--stop-on-tsumo` checks basic closed-hand standard, chiitoitsu, and kokushi
  winning shapes immediately after a synthetic draw.
  It is for plumbing only; it has no call/ron/kan/Kita policy, complete yaku validation, complete
  rinshan yaku/scoring semantics, complete kan-dora/ura-dora indicator ordering, complete
  robbing-kan/chankan handling, scoring, complete payment accounting, complete post-riichi kan
  timing, PPO, or population training.
- `kenjaku.simulation.environment` exposes `kenjaku-sandbox-environment-v0` state plus
  `initial_sandbox_environment`, `draw_for_current_seat`, `legal_discard_actions`,
  `legal_tsumo_actions`, `legal_ron_actions`, `legal_chankan_ron_actions`,
  `legal_chankan_reaction_actions`, `legal_kita_ron_actions`,
  `legal_kita_reaction_actions`, `legal_call_actions`, `legal_ankan_actions`,
  `legal_kakan_actions`, `legal_kita_actions`, `legal_riichi_actions`,
  `legal_reaction_actions`, `legal_sandbox_actions`, `apply_discard_action`,
  `apply_reaction_pass_action`, `apply_call_action`, `apply_ankan_action`, `apply_kakan_action`,
  `apply_kita_action`, `apply_riichi_action`, `apply_tsumo_action`, `apply_ron_action`,
  `apply_ron_actions`, and `pass_pending_discard_reactions`. This is the current simulator boundary
  for future Phase 3 work.
  It only supports draw/discard transitions, pending discard reactions with individual passes,
  ron-priority call gating, discard-furiten, temporary ron-pass furiten, and seeded riichi-furiten
  filtering, basic closed-tenpai riichi declaration, basic post-riichi discard/call restrictions,
  basic wait-preserving post-riichi closed-kan exceptions, a basic point ledger with riichi
  deposit/stick accounting and honba bonus deltas, a basic next-round dealer/honba transition
  helper with round-wind dealer-wrap progression, basic ippatsu window metadata, basic
  chi/pon/minkan calls, a reserved 14-tile dead wall with visible dora/kan-dora indicator metadata,
  basic closed-kan/ankan self-turn actions with dead-wall replacement draws, basic added-kan/kakan
  pon promotions with dead-wall replacement draws, basic rinshan draw-source and winning-rinshan
  terminal metadata, basic haitei/houtei yaku metadata for explicitly final live-wall draws/discards,
  basic double-riichi yaku metadata, a Tenhou Sanma 35,000-point start, Tenhou Sanma no-chi call
  filtering, Tenhou Sanma
  North-as-guest-wind yaku filtering, Tenhou Sanma 1m/9m dora indicator wrap, Tenhou Sanma
  post-pon Kita suppression, basic Tenhou Sanma Kita ippatsu reaction timing, basic Tenhou Sanma
  tsumo-loss point estimates, a basic Tenhou Sanma eight-rinshan replacement reserve cap, a basic
  Sanma Kita/pei-nuki action with dead-wall replacement draws and bonus-han score-estimate
  metadata, a basic Sanma Kita ron/pass reaction window before replacement draw that resolves as
  normal ron rather than chankan, a basic chankan ron/pass window before kakan replacement draw,
  basic chankan ippatsu reaction timing, kokushi-only ankan chankan, basic closed-hand tsumo/ron
  terminal metadata, basic multi-ron terminal resolution with turn-priority riichi-stick assignment,
  basic open/kan standard-shape win detection, a basic sandbox yaku
  filter/metadata layer with dragon/round-wind/seat-wind yakuhai filtering plus toitoi and
  honroutou, selectable self-play reward projections, and terminal point-delta metadata, including
  basic dealer-aware win payment estimates,
  visible-dora/red-five score-estimate bonus han, basic kazoe-yakuman score estimates, basic
  yakuman bonus-han suppression, tsumo yaku/dora tile-view de-duplication, basic Nagashi mangan
  wall-exhaustion and next-round progression, and basic live-wall exhaustive-draw tenpai/noten
  point deltas.

## Working Rules

- Start every session with `git status --short` and inspect current files before relying on this
  document.
- Make small commits for each coherent change. Update `TODO.md` with progress, benchmark results,
  and next targets as work lands.
- Do not revert or overwrite user changes. If the worktree is dirty, distinguish current-task files
  from unrelated edits before patching.
- Keep raw Tenhou XML, SQLite databases, generated reports, disagreement exports, model artifacts,
  external checkouts, build outputs, and bytecode out of git. Use ignored paths under `data/raw/`,
  `runs/`, and `models/`.
- Live ladder automation remains out of scope unless a platform gives explicit permission. The
  project is a replay-analysis and research toolkit.
- The Phase 4 permission-aware replay ingestion/review item is implemented as a manifest review and
  accepted-queue gate only. It does not download replays, post replay URLs, or automate Tenhou or
  Mahjong Soul clients.
- `replay-share-plan` and `replay-public-summary` are not auto-replay-sharing features. They are
  local permission/scope and public-safe summary reports that should precede any future permitted
  sharing implementation.
- `self-play-sandbox` is not enough to check off the Phase 3 self-play harness item. It proves a
  deterministic multi-agent turn loop, basic closed-hand tsumo terminal metadata, a basic
  next-round transition helper with round-wind dealer-wrap progression, and report shape, but not a
  full simulator or training loop.
- The sandbox environment boundary is intentionally incomplete. Do not build PPO, population
  training, or strength claims on it until it has full call/kan timing, full ron/tsumo legality
  including yaku checks, complete payment/scoring semantics beyond the current basic point-ledger,
  dealer-aware payment, visible/red dora, kazoe-yakuman limit, yakuman bonus-han suppression, tsumo
  tile-view de-duplication, Nagashi mangan wall-exhaustion and next-round handling, and next-round
  dealer/honba/round-wind helper slices, ippatsu scoring, full yaku-aware open/kan hand legality,
  complete rinshan yaku/scoring semantics, complete
  haitei/houtei endgame timing, complete double-riichi/riichi timing, complete kan-dora/ura-dora
  indicator ordering, complete chankan semantics, complete post-riichi kan timing,
  real multi-ron payment handling, and validation against real reconstructed games.
- `self-play-sandbox --ruleset tenhou-3p` is not enough to check off the Phase 5 Sanma ruleset
  item. It applies the static tile exclusions, 35,000-point starts, three-seat rotation, a basic
  no-chi call filter, a basic North-as-guest-wind yaku filter, a basic Kita/pei-nuki environment
  action, a basic Kita ron/pass reaction window, Tenhou's 1m/9m dora indicator wrap, and post-pon
  Kita suppression plus Kita ippatsu reaction timing and basic tsumo-loss point estimates only.
  It also caps full-state replacement draws at eight. Real Sanma still needs gameplay, exact
  platform timing, complete call handling, scoring, training, and evaluation.
- PyTorch is now a core dependency for the supervised-learning path. Keep non-ML command imports
  lazy where practical so source checkouts remain usable before installation.

## Implicit Assumptions

- Local benchmark metrics are aggregate-only notes from ignored local Tenhou data. They are not
  reproducibility proof unless the same local sample exists or is regenerated with the runbook.
- `benchmark-discard` comparisons are only comparable when the split seed, eval fraction, epochs,
  learning rate, L2, and data slice are held fixed.
- `eval_analysis.by_shanten_delta` is based on the actual supervised discard action, not on the
  model's predicted discard.
- `by_round_event_phase` uses `DiscardExample.event_index`. `by_seat_turn_phase` uses the
  discarding player's own discard index: 0-5 early, 6-11 middle, and 12+ late.
- `discard-linear-risk-context-v0` remains a strong aggregate anchor on the 100-log slice.
- `discard-linear-defense-context-v0` ties risk-context aggregate accuracy at `lr=0.05, l2=0.0`
  and has stronger targeted defense buckets, but it is not clearly better enough to replace the
  risk anchor.
- `discard-linear-defense-context-v1` is still exploratory. It adds finer active-opponent fractions,
  sotogawa-style outside tiles, terminal/honor live pressure, dora/indicator flags, ippatsu timing,
  last-tsumogiri-after-riichi context, and opponent meld-tile pressure, but still trails v0 in
  aggregate on the 100-log slice.
- The heuristic defense risk scorer is a feature/diagnostic primitive, not a learned deal-in model.
  Do not mark Phase 1's defense scorer/probability-estimator item complete until it is validated
  against outcome labels or a comparable held-out target.
- The current `benchmark-deal-in` fixture smoke has only four labeled examples and one direct
  positive. It proves command/report shape, not model quality. Run it on ignored 100/500-log slices
  before treating `deal-in-linear-v0` as more than a pipeline baseline.
- The transformer encoder module checks off the Phase 2 encoder implementation item only. It has no
  trained checkpoint, no reported Tenhou metrics, and local Torch-dependent tests skip in this
  workspace because PyTorch is not installed for Python 3.11. The behavior-cloning command exists,
  but the Phase 2 behavior-cloning roadmap item should stay unchecked until it is run on a real
  Tenhou Phoenix/raw slice and summarized against baselines.
- `DiscardLinearModel` uses explicit feature profiles. Keep `discard-linear-raw-count-v0`,
  `discard-linear-v1`, `discard-linear-risk-context-v0`, `discard-linear-defense-context-v0`, and
  `discard-linear-defense-context-v1` stable; add new model kinds rather than silently changing an
  existing profile.
- `call-linear-v0` uses a concealed-remainder shanten proxy for calls. It does not model full
  open-hand shanten or exact chi shape selection beyond choosing the best simple consumed-tile
  proxy.
- `call-linear-v1` is additive and keeps the v0 120-feature prefix stable. It appends exact
  chi-position flags, consumed-tile/open-call proxies, shanten-improvement flags, cached ukeire
  proxies, discarded-tile visibility, and terminal/honor flags.
- Call feature-cache payloads are local-only prepared-example caches. The cache key includes
  selected/train/eval example signatures; do not remove those signatures because equivalent counts
  are not enough to prove feature compatibility.
- Call example-cache payloads are local-only reconstructed-example caches. They intentionally sit
  before `--example-limit` and deterministic splitting, so one cache can support multiple caps and
  split settings as long as the XML file identity/size/mtime and `--skip-errors` match.
- `--example-limit-strategy balanced` is a deterministic roughly even call/pass cap for diagnostics,
  not a natural distribution sample. Metrics from balanced caps should not be compared directly to
  prefix or uncapped reports.
- Balanced call limiting now interleaves selected call and pass examples. Cache signatures include
  selected order, so the v1 cache path must change after any selection-order change.
- Riichi examples are conservative supervised decision points, not complete riichi legality. Negative
  examples require closed tenpai by the existing closed-hand shanten proxy and sufficient score.
- `riichi-linear-v0` fixes riichi recall but needs threshold calibration. On the current 500-log
  checks, train-best calibrated thresholds beat fixed `0.25` on balanced accuracy; fixed `0.25`
  is useful as a high-riichi-recall comparison, not as the baseline.
- Threshold sweeps use thresholds 0.00 through 1.00 in 0.05 steps and choose the best threshold by
  balanced accuracy, then target recall, then lower threshold. Calibrated report variants default
  to fixed policy thresholds from the 100-log `tenhou-100-v0` sweep, but
  `--call-threshold-source train-best` and `--riichi-threshold-source train-best` can use the train
  split winner instead. Eval best thresholds remain diagnostics only. None of these options changes
  default model `predict()` behavior.
- Mortal is AGPL-3.0-or-later. Keep any future comparison behind a neutral data/subprocess boundary
  unless the project intentionally accepts that license boundary.

## Local Artifacts

Expected ignored local paths when local Tenhou data and external baselines are available:

```bash
data/raw/tenhou/db/current-year.db
data/raw/tenhou/xml/4p-hanchan-25
data/raw/tenhou/xml/4p-hanchan-100
data/raw/tenhou/xml/4p-hanchan-500
data/raw/external/mortal
runs/discard-benchmark-tenhou-25-report-v1.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.0001-report.json
runs/discard-benchmark-tenhou-100-lr0.1-l2-0.001-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0-report.json
runs/discard-benchmark-tenhou-100-fast-lr0.05-l2-0-report.json
runs/discard-benchmark-tenhou-100-lr0.05-l2-0.0001-report.json
runs/discard-disagreements-tenhou-100-lr0.1-l2-0.json
runs/discard-disagreements-tenhou-100-lr0.1-l2-0-summary.json
runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json
runs/discard-disagreements-tenhou-100-lr0.05-l2-0-summary.json
runs/call-benchmark-tenhou-100-report.json
runs/call-benchmark-tenhou-100-v1-report.json
runs/call-benchmark-tenhou-100-v2-report.json
runs/call-benchmark-tenhou-100-v3-report.json
runs/call-benchmark-tenhou-100-v4-report.json
runs/call-benchmark-tenhou-100-v5-report.json
runs/call-benchmark-tenhou-100-v6-report.json
runs/call-benchmark-tenhou-500-fast-limit5000-epochs5-train-best-v0-report.json
runs/call-features-tenhou-500-fast-balanced-limit10000-v0.json
runs/call-benchmark-tenhou-500-fast-balanced-limit10000-epochs5-train-best-v0-report.json
runs/call-benchmark-tenhou-500-fast-balanced-limit10000-epochs5-train-best-v0-cachehit-report.json
runs/call-features-tenhou-500-fast-balanced-limit10000-v1.json
runs/call-benchmark-tenhou-500-balanced-limit10000-e30-lr0.05-w1.0-weighted-v1-report.json
runs/call-features-tenhou-500-balanced-limit20000-v1.json
runs/call-benchmark-tenhou-500-balanced-limit20000-e30-lr0.05-w1.0-weighted-v1-report.json
runs/call-benchmark-tenhou-500-balanced-limit20000-e30-lr0.05-w1.0-weighted-v1-cachehit-report.json
runs/riichi-benchmark-tenhou-100-v0-report.json
runs/riichi-benchmark-tenhou-100-v1-report.json
runs/riichi-benchmark-tenhou-100-v2-report.json
runs/riichi-benchmark-tenhou-100-v3-report.json
runs/riichi-benchmark-tenhou-500-v0-report.json
runs/riichi-benchmark-tenhou-500-train-best-v0-report.json
runs/riichi-benchmark-tenhou-500-train-best-v1-report.json
runs/riichi-benchmark-tenhou-500-train-best-tenhou-500-v*.json
runs/riichi-benchmark-tenhou-500-fixed025-tenhou-500-v*.json
runs/fixture-discard-mlp.json
runs/fixture-decision-snapshots.jsonl
runs/fixture-decision-predictions.jsonl
runs/decision-snapshots-local.jsonl
runs/inspect-tenhou-25-report.json
runs/inspect-tenhou-100-report.json
runs/
models/
```

Do not commit those paths. Record only aggregate counts and metrics in docs.

## Verification

In this workspace, `python3` currently resolves to an unsupported Python 3.14 runtime. Use
`python3.11` for the Python verification commands unless a supported `python3.12` or `python3.13`
interpreter is selected explicitly. `ruff` is available through the default `python3` environment.

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m compileall -q src tests
python3 -m ruff check .
PYTHONPATH=src python3 -m kenjaku benchmark-discard data/fixtures/tenhou \
  --epochs 1 --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --l2 0.0001 --report runs/fixture-benchmark-diagnostics.json \
  --disagreements runs/fixture-disagreements.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary runs/fixture-benchmark-diagnostics.json
PYTHONPATH=src python3 -m kenjaku benchmark-dashboard \
  runs/fixture-benchmark-diagnostics.json \
  --output runs/public-benchmarks/index.html
PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/fixture-disagreements.json --examples 1 --tags
PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/fixture-disagreements.json --examples 1 --tags --tag close_logit
PYTHONPATH=src python3 -m kenjaku defense-risk-summary data/fixtures/tenhou \
  --report runs/fixture-defense-risk-summary.json
PYTHONPATH=src python3 -m kenjaku benchmark-deal-in data/fixtures/tenhou \
  --epochs 2 --report runs/fixture-deal-in-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/fixture-deal-in-benchmark.json
PYTHONPATH=src python3 -m kenjaku export-decision-snapshots data/fixtures/tenhou \
  --output runs/fixture-decision-snapshots.jsonl --limit 5
PYTHONPATH=src python3 -m kenjaku decision-snapshot-summary \
  runs/fixture-decision-snapshots.jsonl
PYTHONPATH=src python3 -m kenjaku produce-decision-predictions \
  runs/fixture-decision-snapshots.jsonl \
  --strategy echo-actual \
  --output runs/fixture-decision-predictions.jsonl
PYTHONPATH=src python3 -m kenjaku decision-snapshot-compare \
  runs/fixture-decision-snapshots.jsonl \
  runs/fixture-decision-predictions.jsonl
PYTHONPATH=src python3 -m kenjaku train-discard-mlp data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --hidden-dim 8 --device cpu \
  --eval-fraction 0.25 --split-seed fixed --seed 123 \
  --checkpoint runs/fixture-discard-mlp.pt \
  --report runs/fixture-discard-mlp.json
PYTHONPATH=src python3 -m kenjaku train-discard-transformer data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --device cpu \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --eval-fraction 0.25 --split-seed fixed --seed 123 \
  --checkpoint runs/fixture-discard-transformer.pt \
  --report runs/fixture-discard-transformer.json
PYTHONPATH=src python3 -m kenjaku benchmark-discard-transformer data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --device cpu \
  --model-dim 16 --num-heads 4 --num-layers 1 --feedforward-dim 32 --dropout 0.0 \
  --linear-epochs 1 \
  --eval-fraction 0.25 --split-seed fixed --seed 123 \
  --checkpoint runs/fixture-discard-transformer-benchmark.pt \
  --report runs/fixture-discard-transformer-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-discard-mlp data/fixtures/tenhou \
  --epochs 1 --batch-size 2 --hidden-dim 8 --device cpu \
  --linear-epochs 1 \
  --eval-fraction 0.25 --split-seed fixed --seed 123 \
  --checkpoint runs/fixture-discard-mlp-benchmark.pt \
  --report runs/fixture-discard-mlp-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/fixture-discard-mlp.json runs/fixture-discard-mlp-benchmark.json
PYTHONPATH=src python3 -m kenjaku status
PYTHONPATH=src python3 -m kenjaku status --json
PYTHONPATH=src python3 -m kenjaku replay-intake-review path/to/replay-manifest.json \
  --report runs/replay-intake-review.json \
  --accepted-output runs/replay-intake-accepted.jsonl
PYTHONPATH=src python3 -m kenjaku replay-share-plan \
  runs/replay-intake-accepted.jsonl --intent demo \
  --report runs/replay-share-plan.json
PYTHONPATH=src python3 -m kenjaku replay-public-summary \
  runs/replay-intake-accepted.jsonl --intent demo \
  --report runs/replay-public-summary.json
PYTHONPATH=src python3 -m kenjaku self-play-sandbox \
  --episodes 2 --max-turns 32 --policy frequency --ruleset tenhou-3p \
  --reward-mode normalized-point-delta --stop-on-tsumo \
  --report runs/fixture-self-play-sandbox.json
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --include-weighted \
  --report runs/fixture-call-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-call data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --models fast \
  --example-limit 1 \
  --example-limit-strategy balanced \
  --profile-stages \
  --example-cache runs/fixture-call-example-cache.json \
  --feature-cache runs/fixture-call-feature-cache.json \
  --call-threshold-source train-best \
  --report runs/fixture-call-benchmark-fast.json
PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --include-weighted \
  --report runs/fixture-riichi-benchmark.json
PYTHONPATH=src python3 -m kenjaku benchmark-riichi data/fixtures/tenhou \
  --eval-fraction 0.25 --split-seed fixed --skip-errors \
  --riichi-threshold-source train-best \
  --report runs/fixture-riichi-benchmark-train-best.json
PYTHONPATH=src python3 -m kenjaku benchmark-report-summary \
  runs/fixture-call-benchmark.json runs/fixture-riichi-benchmark.json
git diff --check
```

Useful local benchmark command:

```bash
PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --models fast \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --report runs/discard-benchmark-tenhou-100-fast-lr0.05-l2-0-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --include-weighted \
  --call-positive-weight 2.0 \
  --report runs/call-benchmark-tenhou-100-v6-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-call \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v0 \
  --epochs 30 \
  --learning-rate 0.05 \
  --skip-errors \
  --models call_linear_v1,call_linear_v1_calibrated,call_linear_v1_weighted \
  --include-weighted \
  --call-positive-weight 1.0 \
  --example-limit 20000 \
  --example-limit-strategy balanced \
  --call-threshold-source train-best \
  --profile-stages \
  --example-cache runs/call-examples-tenhou-500-v1.json \
  --feature-cache runs/call-features-tenhou-500-balanced-limit20000-v1.json \
  --report runs/call-benchmark-tenhou-500-balanced-limit20000-v1-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-500 --players 4 --length h --limit 500" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --skip-errors \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --report runs/riichi-benchmark-tenhou-100-v2-report.json \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-riichi \
  data/raw/tenhou/xml/4p-hanchan-500 \
  --eval-fraction 0.2 \
  --split-seed tenhou-500-v1 \
  --skip-errors \
  --include-weighted \
  --riichi-positive-weight 2.0 \
  --riichi-threshold-source train-best \
  --report runs/riichi-benchmark-tenhou-500-train-best-v1-report.json \
  --source-label tenhou-4p-hanchan-500 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-500 --players 4 --length h --limit 500" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku benchmark-discard \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --epochs 3 \
  --learning-rate 0.05 \
  --l2 0.0 \
  --eval-fraction 0.2 \
  --split-seed tenhou-100-v0 \
  --models risk_context_linear,defense_context_linear,defense_context_v1_linear \
  --skip-errors \
  --disagreements runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json \
  --max-disagreements 100 \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku disagreement-report-summary \
  runs/discard-disagreements-tenhou-100-lr0.05-l2-0.json \
  --examples 2 \
  --tags \
  --tag close_logit

PYTHONPATH=src python3 -m kenjaku export-decision-snapshots \
  data/raw/tenhou/xml/4p-hanchan-100 \
  --decision-types discard,call,riichi \
  --limit 1000 \
  --output runs/decision-snapshots-tenhou-100-v0.jsonl \
  --source-label tenhou-4p-hanchan-100 \
  --source-command "houou-logs export data/raw/tenhou/db/current-year.db data/raw/tenhou/xml/4p-hanchan-100 --players 4 --length h --limit 100" \
  --source-date 2026-current-year

PYTHONPATH=src python3 -m kenjaku decision-snapshot-summary \
  runs/decision-snapshots-tenhou-100-v0.jsonl
```

## Latest Benchmarks

25-log local result, split `tenhou-25-v0`, `lr=0.1`, `l2=0.0`:

- Dataset: 25 XML files, 255 rounds, 11,855 discard examples, 3,185 call examples, zero parse
  failures; 9,484 train / 2,371 eval.
- Eval accuracy: frequency 0.3037, raw-count linear 0.3830, shanten-aware linear 0.4757,
  risk-context linear 0.4829, defense-context linear 0.4825, defense-context-v1 linear 0.4884.
- Defense-context-v1 lift: +0.0059 over defense-context and about +0.0055 over risk-context.

100-log local sweep, split `tenhou-100-v0`, 3 epochs, 40,221 train / 10,055 eval:

| Learning rate | L2 | Risk eval | Defense eval | V1 eval | Takeaway |
|---:|---:|---:|---:|---:|---|
| 0.10 | 0.0 | 0.4919 | 0.4881 | 0.4840 | Original regression. |
| 0.10 | 0.0001 | 0.4892 | 0.4853 | 0.4812 | Small L2 hurt. |
| 0.10 | 0.001 | 0.4707 | 0.4669 | 0.4630 | Strong L2 badly hurt. |
| 0.05 | 0.0 | 0.5124 | 0.5124 | 0.5102 | Best aggregate result. |
| 0.05 | 0.0001 | 0.5113 | 0.5093 | 0.5086 | L2 still hurt. |

100-log fast discard rerun, split `tenhou-100-v0`, `lr=0.05`, `l2=0.0`, `--models fast`:

- Eval accuracy: frequency 0.2985, shanten-aware linear 0.5012, risk-context linear 0.5124,
  defense-context linear 0.5124.
- This matches the earlier best risk/defense aggregate result while skipping raw-count and v1.

Best 100-log selected buckets at `lr=0.05`, `l2=0.0`, risk -> defense -> v1:

- Active-riichi: 0.5252 -> 0.5181 -> 0.5228 on 1,687 examples.
- Actual genbutsu: 0.6129 -> 0.6154 -> 0.6179 on 793 examples.
- Actual suji: 0.3686 -> 0.4278 -> 0.4227 on 388 examples.
- Actual kabe: 0.4962 -> 0.5166 -> 0.5192 on 391 examples.
- One-chance: 0.4730 -> 0.4950 -> 0.4950 on 1,091 examples.
- Seen after riichi: 0.5401 -> 0.5875 -> 0.5935 on 337 examples.
- Shanten-worsening: 0.0977 -> 0.1015 -> 0.1157 on 778 examples.

Diagnostics from ignored artifacts:

- Baseline disagreement counts at `lr=0.1`, `l2=0.0`: risk-correct/defense-wrong 268,
  risk-correct/v1-wrong 358, defense-correct/risk-wrong 230, v1-correct/risk-wrong 279.
- `disagreement-report-summary` on the capped `lr=0.1`, `l2=0.0` artifact shows stored examples
  are usually not obvious safe-tile cases: the four 100-item category samples have active-riichi
  rates around 24-26%, genbutsu 10-13%, suji 6-12%, and seen-after-riichi 2-8%.
- Mean stored logit margins are modest but nontrivial: risk-correct/defense-wrong has risk actual
  margin 0.3002 and defense wrong-over-actual margin 0.3169; defense-correct/risk-wrong has defense
  actual margin 0.2538 and risk wrong-over-actual margin 0.3062.
- Current-best disagreement counts at `lr=0.05`, `l2=0.0`: risk-correct/defense-wrong 203,
  risk-correct/v1-wrong 268, defense-correct/risk-wrong 203, v1-correct/risk-wrong 246.
- The capped `lr=0.05`, `l2=0.0` stored examples have active-riichi rates around 28-36%,
  genbutsu 10-16%, suji 4-14%, and seen-after-riichi 1-10%. Defense-correct samples have more
  active-riichi and safety-signal mass than risk-correct samples, but neither side is dominated by
  obvious safe-tile examples.
- `disagreement-report-summary --examples 2` confirms the stored examples are mixed: some
  defense-correct cases are active-riichi/suji or genbutsu-adjacent, while several risk-correct
  cases are no-riichi close-logit choices rather than obvious defense misses.
- `disagreement-report-summary --examples 2 --tags` on the current-best capped artifact tags 400
  stored examples as 373 efficiency-like, 336 close-logit, 130 active-riichi, 130 safe-tile
  candidates, and 82 defense-signal cases. No stored item falls into `no_obvious_signal` under the
  current deterministic rules.
- Mean stored logit margins at `lr=0.05`, `l2=0.0`: risk-correct/defense-wrong has risk actual
  margin 0.2660 and defense wrong-over-actual margin 0.2449; defense-correct/risk-wrong has defense
  actual margin 0.2312 and risk wrong-over-actual margin 0.2142.
- At `lr=0.05`, `l2=0.0`, selected v1-only feature activation rates on eval candidates:
  sotogawa 0.0462, dora 0.0033, active ippatsu fraction 0.0369, active tsumogiri fraction 0.1244,
  opponent meld tile fraction 0.4234.
- Feature summaries are identical across LR/L2 runs for a fixed profile and split; weight summaries
  change with training settings.

Call benchmark, 100-log local slice, split `tenhou-100-v0`:

- Dataset: 13,435 call/pass examples; 10,748 train / 2,687 eval.
- `call-frequency-v0` scored 0.8524 train / 0.8463 eval accuracy, but only 0.5000 balanced eval
  accuracy and 0.0000 call recall. It predicts pass too often.
- `call-legal-frequency-v0` scored 0.1515 eval accuracy, 0.4927 balanced eval accuracy, 0.0000
  pass recall, and 0.9855 call recall. It predicts legal calls too often.
- `call-linear-v0` scored 0.8306 train / 0.8288 eval accuracy, 0.7294 balanced eval accuracy,
  0.8729 pass recall, and 0.5860 call recall. Per-action eval recall: chi 0.4045, minkan 1.0000,
  pass 0.8729, pon 0.7210.
- `call-linear-v1` scored 0.8410 train / 0.8377 eval accuracy, 0.7496 balanced eval accuracy,
  0.8769 pass recall, and 0.6223 call recall. Per-action eval recall: chi 0.4663, minkan 0.5000,
  pass 0.8769, pon 0.7425.
- Report-only threshold calibration for `call-linear-v1` picked threshold 0.40 on eval, with binary
  balanced accuracy 0.7762, call precision 0.4393, call recall 0.7191, and pass recall 0.8333.
  For comparison, `call-linear-v0` picked threshold 0.35 with binary balanced accuracy 0.7531.
- The explicit `call_linear_v1_calibrated` policy variant at threshold 0.40 scored 0.8154 eval
  accuracy, 0.7750 exact-action balanced eval accuracy, 0.8333 pass recall, and 0.7167 exact-call
  recall.
- The opt-in `call_linear_v1_weighted` comparison at positive class weight 2.0 scored 0.6729 eval
  accuracy, 0.7522 balanced eval accuracy, 0.6376 pass recall, and 0.8668 call recall. Its own
  report-only threshold sweep picked 0.80 with binary balanced accuracy 0.7669.
- `call-linear-v1` is now the strongest call/pass floor on this split. It improves aggregate,
  balanced accuracy, pass recall, and call recall versus v0. The calibrated v1 policy beats the
  simple weighted training comparison on balanced accuracy, while weighted training mostly trades
  pass recall for call recall.

Riichi benchmark, 100-log local slice, split `tenhou-100-v0`:

- Dataset: 2,039 conservative riichi/pass examples; 1,631 train / 408 eval.
- `riichi-frequency-v0` scored 0.6272 train / 0.6618 eval accuracy, 0.5000 balanced eval accuracy,
  1.0000 pass recall, and 0.0000 riichi recall. It is a pass-dominant floor, not a useful riichi
  policy.
- `riichi-linear-v0` scored 0.5254 train / 0.4510 eval accuracy, 0.5639 balanced eval accuracy,
  0.2148 pass recall, and 0.9130 riichi recall.
- Report-only threshold calibration for `riichi-linear-v0` picked threshold 0.95 on eval, with
  binary balanced accuracy 0.6444, riichi precision 0.5476, riichi recall 0.5000, and pass recall
  0.7889.
- The explicit `riichi_linear_calibrated` policy variant at threshold 0.95 scored 0.6912 eval
  accuracy, 0.6444 balanced eval accuracy, 0.7889 pass recall, and 0.5000 riichi recall.
- The opt-in `riichi_linear_weighted` comparison at positive class weight 2.0 scored 0.3603 eval
  accuracy, 0.5114 balanced eval accuracy, 0.0444 pass recall, and 0.9783 riichi recall. Its own
  report-only threshold sweep picked 0.95 with binary balanced accuracy 0.5713.
- `riichi-linear-v0` proves the feature stream can identify riichi opportunities, and thresholding
  gives a much healthier tradeoff than raw argmax prediction or simple positive weighting. Do not
  add riichi features before confirming this calibrated policy on a larger local slice.

Riichi benchmark, 500-log local slice, split-seed checks:

- Dataset shape: 10,249 conservative riichi/pass examples per split; 8,199 train / 2,050 eval.
- Train-best `riichi_linear_calibrated` thresholds across seeds `tenhou-500-v0` through
  `tenhou-500-v3`: 0.35, 0.45, 0.35, 0.40.
- Train-best balanced eval accuracy by seed: 0.6737, 0.6608, 0.6738, 0.6533.
- Train-best riichi/pass recall by seed: 0.7548/0.5927, 0.6601/0.6615, 0.6970/0.6507,
  0.7015/0.6051.
- Fixed threshold 0.25 across the same seeds raised riichi recall but weakened pass recall:
  balanced eval accuracy 0.6732, 0.6401, 0.6690, 0.6406; riichi recall 0.8624, 0.8379, 0.7997,
  0.8426; pass recall 0.4840, 0.4423, 0.5383, 0.4387.
- Conclusion: keep train-best `riichi_linear_calibrated` as the current baseline. Fixed 0.25 is a
  high-riichi-recall comparison, not the balanced baseline.

Call benchmark, 500-log local slice:

- Source examples: 69,013 call/pass examples.
- Balanced 10k cap after deterministic call/pass interleaving: 8,000 train / 2,000 eval. Initial
  v1 cache build spent 377.2313s in `feature_prepare_v1`; weighted-grid cache-hit loads were
  0.3100-0.3914s.
- Full 10k grid over epochs `{5,15,30}`, learning rates `{0.1,0.05,0.02}`, and positive weights
  `{1.0,1.5,2.0}` produced 27 explicit weighted reports.
- Best train-selected calibrated policy on 10k: 30 epochs, LR 0.05, train-selected threshold 0.40,
  eval accuracy 0.7560, balanced eval accuracy 0.7558, pass recall 0.7404, call recall 0.7713.
- Best weighted comparison on 10k: 30 epochs, LR 0.1, positive weight 2.0, eval accuracy 0.7600,
  balanced eval accuracy 0.7601, pass recall 0.7687, call recall 0.7515.
- Since 10k no longer collapsed, the next 20k cap was run with 30 epochs / LR 0.05 / weight 1.0.
  Initial 20k cache build spent 267.5774s in `feature_prepare_v1`; the cache-hit rerun loaded v1
  features in 0.7491s.
- 20k calibrated result: 16,000 train / 4,000 eval, train-selected threshold 0.50, eval accuracy
  0.7290, balanced eval accuracy 0.7289, pass recall 0.6979, call recall 0.7598.
- Conclusion: balanced call training is no longer pass-only after the interleaving fix and longer
  training. The next blocker is scaling beyond 20k or uncapped without repeated parse/reconstruct
  cost, not basic call recall collapse.

Mortal local baseline reconnaissance:

- Ignored checkout: `data/raw/external/mortal`.
- Commit inspected: `0cff2b52982be5b1163aa9a62fb01f03ce91e0d2`.
- `cargo build -p libriichi --lib --release` passed locally in 37.70s.
- `cargo test --workspace --no-default-features --features flate2/zlib -- --nocapture` passed:
  28 Rust unit tests plus doctests reported OK.
- `cp target/release/libriichi.dylib mortal/libriichi.so` followed by
  `PYTHONPATH=mortal python3 -c "import libriichi"` succeeded.

## Next Tasks

1. Use `benchmark-call --example-cache` on the 500-log slice, then compare the 10k best weighted
   policy against the 20k calibrated policy with the same cap and
   seed before selecting a default call report policy.
2. Run `benchmark-deal-in` on the ignored 100/500-log Tenhou slices and compare the resulting
   `benchmark-report-summary` output for `deal-in-linear-v0`, including train/eval best threshold
   calibration, against the heuristic risk baseline before checking off the Phase 1 defense
   scorer/probability-estimator item.
3. Keep riichi on train-best calibration for now. Do not add riichi features until a concrete
   failure mode appears beyond the fixed-0.25 recall/pass-recall tradeoff.
4. Use the stub prediction producer only for protocol tests. Real Mortal inference still requires
   legally usable weights and a subprocess/data boundary.
5. Use disagreement tag filters to guide discard work. The current sample is mostly efficiency-like
   and close-logit, so avoid a new defense profile until tag-specific examples reveal a concrete gap.
6. Run `benchmark-discard-mlp` and `benchmark-discard-transformer` on comparable ignored Tenhou
   slices, then summarize both artifacts before treating transformer behavior cloning as underway.
7. Use `replay-intake-review` before any replay URL/local-export analysis queue. Keep automatic
   live-service fetching out of scope until explicit platform permission exists. Use
   `replay-share-plan` as the local permission/scope check and `replay-public-summary` as the
   sanitized report before demo or redistribution work.
8. Use `self-play-sandbox` for deterministic self-play plumbing checks only. The next real Phase 3
   step is extending the current environment boundary with full call/kan timing, yaku/terminal
   outcome semantics, complete payment/scoring semantics beyond the basic live-wall exhaustive-draw
   tenpai/noten point-delta, dealer-aware win payment, visible/red-dora score-estimate,
   kazoe-yakuman limit, yakuman bonus-han suppression, Nagashi mangan wall-exhaustion and
   next-round handling, and next-round dealer/honba/round-wind slices, ippatsu scoring beyond
   narrow ron metadata, full yaku-aware open/kan hand legality,
   complete rinshan yaku/scoring semantics, complete haitei/houtei endgame timing,
   complete double-riichi/riichi timing, complete kan-dora/ura-dora indicator ordering, complete
   chankan semantics beyond the basic ippatsu timing
   slice, complete post-riichi kan timing, real multi-ron payment handling, and scoring before PPO work.
9. Use `self-play-sandbox --ruleset tenhou-3p` only to check Sanma sandbox plumbing. Do not mark
   the Phase 5 Sanma ruleset complete just because static tile exclusions, start points, no-chi,
   North guest-wind handling, Kita actions/reactions, 1m/9m dora wrapping, post-pon Kita
   suppression, Kita ippatsu reaction timing, basic tsumo-loss point estimates, and an
   eight-rinshan replacement cap exist. Real 3-player gameplay, exact platform timing, exact call
   timing, complete call handling, full yaku/fu validation, scoring, placement/return handling,
   training, and evaluation are still open.
10. Extend `kenjaku.simulation.environment` before adding PPO: full call/kan timing, multi-ron
    payment semantics, full ron/tsumo legality, complete payment/scoring semantics beyond the basic
    dealer-aware, visible/red-dora, and next-round dealer/honba/round-wind helper slices, ippatsu
    scoring, full yaku-aware open/kan hand legality, complete rinshan yaku/scoring semantics,
    complete kan-dora/ura-dora indicator ordering, complete chankan semantics beyond the basic
    ippatsu timing slice, complete post-riichi kan timing, and yaku/scoring semantics should come
    before policy optimization.
