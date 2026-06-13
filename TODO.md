# Kenjaku TODO

This file is the remaining actionable backlog. Completed history belongs in git
commits and handoff notes, not here.

Rules for this backlog:

- Every task must have a measurable deliverable.
- Do not mark a task done without tests, command output, report artifacts, or
  documentation that proves the acceptance criteria.
- Do not implement live ranked automation or replay redistribution unless the
  target platform explicitly permits it in writing.
- Keep raw Tenhou XML, databases, generated model artifacts, and private replay
  exports out of git.

## P0 Compliance And Data Gates

### TODO-001 Verify Tenhou Phoenix Data Intake

- [x] Run a local `houou-logs` export/import smoke test on at least 1 permitted
  Tenhou XML game stored outside git.
- [x] Document the exact export/import commands, source date, and
  no-redistribution handling in `docs/data-policy.md`.
- [x] Add or update a CLI smoke test that proves Kenjaku can parse the exported
  file format without committing raw logs.

Done when: `inspect-tenhou` or the replacement intake command parses at least 1
local exported game, the command is documented, and `git status --short` shows no
raw replay files.

Evidence: completed locally on 2026-06-13 SGT with one ignored `houou-logs`
4-player hanchan export dated `2026-04-02T00:06`; `inspect-tenhou` parsed
1 XML file into 13 rounds, 619 discard examples, 164 call examples, and 0 parse
failures. Commands and no-redistribution handling are recorded in
`docs/data-policy.md`. The checked-in CLI smoke test copies a synthetic Tenhou
fixture into a temporary exported-XML directory outside git.

### TODO-002 Gate Mahjong Soul Replay Usage

- [x] Decide whether Mahjong Soul replay sharing can be used for offline
  analysis without violating current terms.
- [x] If permitted, manually validate 3 replay-share examples and document the
  allowed source, storage, and redistribution limits. Not applicable: the
  2026-06-12 policy decision is not-permitted without explicit permission.
- [x] If not permitted, document the block and remove Mahjong Soul scraping from
  the active implementation path.

Done when: `docs/data-policy.md` has a dated Mahjong Soul decision, evidence
links, and a clear allowed/not-allowed workflow.

### TODO-003 Prove Cloud GPU Dry Run

- [ ] Run one cloud GPU smoke job on Lambda, RunPod, or another provider using a
  small ignored training slice.
- [ ] Record provider, GPU type, wall-clock runtime, cost, command, and artifact
  path in docs.
- [ ] Verify the same command can fall back to CPU/MPS locally on a tiny fixture.

Done when: a documented dry run produces a training report and cost estimate
without committing model weights or raw data.

Progress: local fallback was verified on 2026-06-13 SGT with
`train-discard-transformer --device auto`; it selected CPU, wrote ignored
artifacts under `runs/todo-003/`, and completed in 9.30 seconds. The cloud GPU
run remains open because this environment has no provider CLI, credentials, or
local GPU. See `docs/cloud-gpu-dry-run.md` for the exact command and remaining
evidence needed.

## P1 Data And Supervised Models

### TODO-101 Finish Deal-In Probability Estimator

- [x] Train a deal-in estimator on at least 50,000 local discard decisions.
- [x] Evaluate on a held-out slice of at least 10,000 decisions.
- [x] Report Brier score, log loss, accuracy, balanced accuracy, precision,
  recall, and calibration-threshold sweep.
- [x] Beat the current heuristic baseline on Brier score and log loss, or
  document why the model is not ready.

Done when: `benchmark-deal-in` outputs a reproducible report showing model versus
heuristic metrics and the status command no longer needs to describe the defense
scorer as only heuristic/report-only.

Evidence: completed on 2026-06-13 SGT with
`runs/todo-101/deal-in-benchmark-130-v0.json` over an ignored 130-log Tenhou
4-player hanchan slice. The report has 65,777 direct-labeled decisions,
55,252 train examples, 10,525 eval examples, 697 positive labels, and an eval
threshold sweep. Eval metrics at threshold 0.50: accuracy 0.9853, balanced
accuracy 0.5068, precision 0.0426, recall 0.0179, Brier 0.0144, log loss
0.0688. The heuristic baseline scored Brier 0.0322 and log loss 0.3176, so the
model beat it on both required calibration metrics. Commands and caveats are in
`docs/local-tenhou-eval.md`; the status command now says only that no trained
deal-in estimator is bundled.

### TODO-102 Run Behavior Cloning On Real Tenhou Slices

- [ ] Train discard, call, and riichi supervised models on a real local Tenhou
  export, not only synthetic fixtures.
- [ ] Use at least 100,000 train decisions and 20,000 held-out eval decisions per
  decision type when data is available.
- [ ] Save report JSON for each model family with train/eval sizes, loss,
  accuracy, balanced accuracy, per-action recall, seed, and command.

Done when: documented reports exist for discard, call, and riichi behavior
cloning runs and can be summarized by `benchmark-report-summary`.

### TODO-103 Reproduce A Mortal Baseline Boundary

- [ ] Read and document the exact Mortal interface used for comparison.
- [ ] Run Mortal or a documented Mortal-compatible baseline on the same eval
  slice used by Kenjaku.
- [ ] Report metric definitions so Kenjaku and Mortal comparisons are not mixing
  incompatible datasets or action spaces.

Done when: a report compares Kenjaku and Mortal on one shared slice, with the
license boundary documented.

### TODO-104 Match Or Exceed The Supervised Baseline

- [ ] Define the target metric before training: discard exact accuracy, call
  balanced accuracy, riichi balanced accuracy, and deal-in calibration.
- [ ] Run at least 3 seeds for the selected Kenjaku supervised model.
- [ ] Compare mean and best seed against the Mortal/baseline report from
  TODO-103.

Done when: a checked-in summary states whether Kenjaku matched, exceeded, or
missed the baseline and includes the exact artifact paths.

## P2 Simulator And Scoring

### TODO-201 Complete Call And Kan Timing

- [x] Model full chi, pon, minkan, ankan, kakan, Kita, ron, pass, and replacement
  draw timing as explicit state transitions.
- [x] Add tests for priority order across ron, pon, kan, chi, pass, and multiple
  reaction seats.
- [x] Add tests for illegal calls after riichi, post-call discard obligations,
  and simultaneous reaction windows.

Done when: all call/kan reaction windows have deterministic priority tests and no
state path can leave two pending windows open.

### TODO-202 Expand Yaku Legality

- [ ] Add exact yaku detection for pinfu, iipeikou, sanshoku doujun, sanshoku
  doukou, ittsuu, chanta, junchan, sanankou, sankantsu, shousangen, honitsu,
  chinitsu, chinroutou, tsuuiisou, daisangen, shousuushi, daisuushi, suuankou,
  suukantsu, ryuuiisou, chuuren, and tenhou/chiihou where applicable.
- [ ] For every yaku, add at least one positive and one negative unit test.
- [ ] Distinguish closed-only, open-allowed, and open-value-reduced yaku.

Done when: sandbox terminal legality no longer depends on the current small yaku
subset, and unsupported yaku are listed explicitly.

### TODO-203 Implement Exact Fu And Han Scoring

- [ ] Compute fu for waits, pair value, open/closed triplets, kans, pinfu,
  chiitoitsu, ron, tsumo, and rounding.
- [ ] Implement limit handling for mangan, haneman, baiman, sanbaiman, counted
  yakuman, multiple yakuman, and optional kiriage only behind an explicit ruleset
  flag.
- [ ] Validate at least 50 scoring fixtures covering dealer/child ron, tsumo,
  honba, riichi sticks, multi-ron, and Sanma tsumo-loss.

Done when: score estimates are replaced or backed by an exact scorer with fixture
coverage and current status text no longer says scoring is false.

### TODO-204 Complete Dora And Ura-Dora Ordering

- [x] Implement visible dora, kan-dora, ura-dora, kan-ura, red dora, and Kita
  counting in the same order as the target ruleset.
- [x] Add tests for indicator reveal timing after minkan, ankan, kakan, rinshan,
  chankan, and riichi wins.
- [x] Validate Tenhou Sanma 1m/9m wrap remains covered.

Done when: dora counts are exact for at least 20 fixture hands and are no longer
described as basic estimates.

### TODO-205 Complete Riichi Timing

- [x] Enforce legal first-turn double riichi, normal riichi, ippatsu expiry, and
  post-riichi discard/kan constraints across calls and abortive draws.
- [x] Add tests for four-riichi abortive draw, riichi after calls, riichi with
  insufficient points, and riichi declaration stick accounting.

Done when: riichi timing is modeled in terminal legality, next-round progression,
and scoring tests.

### TODO-206 Complete Rinshan, Chankan, Haitei, And Houtei

- [x] Validate rinshan yaku/scoring on all kan replacement paths.
- [x] Validate chankan for kakan and kokushi-only ankan, including ippatsu and
  dora timing.
- [x] Validate haitei/houtei after calls, dead-wall draws, replacement draws, and
  final live-wall draw/discard.

Done when: each endgame yaku has timing fixtures that cover both positive and
negative cases.

### TODO-207 Add Abortive Draws

- [x] Implement kyuushu kyuuhai, four winds, four kans, four riichi, and
  triple-ron handling according to the chosen ruleset.
- [x] Define whether each draw repeats dealer, increments honba, carries riichi
  sticks, and advances round wind.
- [x] Add one next-round progression test for each abortive draw type.

Done when: abortive draws are terminal reasons with explicit point and next-round
semantics.

### TODO-208 Complete Round And Game Progression

- [x] Implement East/South/West continuation rules, all-last handling, bankruptcy,
  return points, oka/uma, placement, and final ranking.
- [x] Cover 3-player and 4-player differences.
- [x] Add tests for dealer repeat, dealer rotation, round wind advance, game end,
  and negative-score termination.

Done when: a sandbox match can run from East 1 to a final ranked result.

### TODO-209 Calibrate Reward Signals

- [x] Replace simple terminal rewards with point-delta and placement-aware reward
  options.
- [x] Add report fields for raw point delta, normalized point delta, placement
  delta, win/deal-in events, and draw outcomes.
- [x] Compare at least 3 reward formulations on the same self-play smoke run.

Done when: `self-play-sandbox` can select a reward mode and writes comparable
reward summaries.

## P3 RL Pipeline

### TODO-301 Build A Full Self-Play Harness

- [x] Run complete multi-round matches with 3 or 4 agents until game end.
- [x] Support pluggable policies for discard, call, riichi, kan, Kita, ron, and
  pass decisions.
- [x] Write trajectory artifacts with states, legal actions, chosen actions,
  rewards, terminal reasons, scores, and final placement.

Done when: one command can run at least 100 deterministic fixture-seeded games and
produce aggregate metrics without illegal state transitions.

### TODO-302 Implement PPO

- [x] Add policy/value losses, GAE, clipping, entropy regularization, batching,
  checkpointing, and resume support.
- [x] Run a fixture-scale PPO smoke test for at least 1,000 environment steps.
- [x] Save training curves and evaluation summaries.

Done when: PPO can train from random or supervised-initialized weights and pass a
deterministic smoke test in CI-sized runtime.

### TODO-303 Add Population-Based Training

- [ ] Maintain at least 4 policy snapshots in a training pool.
- [ ] Sample opponents from the pool and report win rate, average placement,
  deal-in rate, and average score.
- [ ] Add replacement/promotion criteria for stronger agents.

Done when: one training report shows pool composition, matchup counts, and
promotion decisions.

### TODO-304 Evaluate Against Mortal And Akochan

- [ ] Define an offline duplicate-mahjong or shared-log evaluation protocol.
- [ ] Run Kenjaku, Mortal, and akochan-compatible baselines on the same scenario
  set where licensing permits.
- [ ] Report confidence intervals over at least 1,000 comparable decisions or 100
  comparable games.

Done when: the project has a reproducible external-baseline report that does not
depend on unauthorized ranked automation.

## P4 Sanma

### TODO-401 Complete Tenhou Sanma Rules

- [ ] Implement complete 3-player round flow, dealer rotation, honba/riichi
  sticks, tsumo-loss, Kita, no-chi, dead-wall, and dora semantics.
- [ ] Add exact Sanma scoring fixtures for ron, tsumo, Kita, honba, dealer, and
  child payments.
- [ ] Add at least 30 Sanma unit tests beyond the current plumbing checks.

Done when: `self-play-sandbox --ruleset tenhou-3p` can run a complete legal game
to final placement.

### TODO-402 Establish Sanma Training Data

- [ ] Identify a permitted Sanma replay source.
- [ ] Parse at least 1,000 Sanma games or document why the source is blocked.
- [ ] Export discard, call, riichi, Kita, and win/pass decisions into the same
  snapshot format used by 4-player training.

Done when: a data report gives source, count, legal constraints, and snapshot
schema coverage.

### TODO-403 Train And Evaluate Sanma Models

- [ ] Train Sanma discard/call/riichi/Kita baselines.
- [ ] Evaluate on held-out Sanma decisions.
- [ ] Compare against simple frequency and shanten/risk baselines.

Done when: Sanma benchmark reports exist for each modeled action type.

## P5 Product And Analysis Tools

### TODO-501 Build Interpretability Overlay

- [ ] For each analyzed decision, show top 3 alternatives with policy
  probability, shanten delta, estimated deal-in risk, and expected point impact.
- [ ] Render at least 100 decisions from a local permitted replay export.
- [ ] Add a screenshot or HTML artifact that contains no raw private replay data.

Done when: a user can open a local replay-analysis view and inspect model
reasoning for each discard.

### TODO-502 Build Browser-Playable Demo

- [x] Implement a browser demo that can play at least one complete hand against
  local AI policies.
- [x] Show hand, discards, calls, dora, score, legal actions, and terminal result.
- [x] Add smoke tests or screenshot checks for the main game screen.

Done when: a documented command starts the demo and a user can complete a hand
without server-side raw replay data.

### TODO-503 Add Permitted Replay Sharing Pipeline

- [x] Accept only replay sources that pass `replay-intake-review`.
- [x] Generate shareable summaries for permitted private-room, tournament, or
  offline analysis games.
- [x] Refuse unsupported ranked automation and raw replay redistribution.

Done when: the pipeline creates a public-safe report for permitted inputs and
rejects disallowed inputs with an explicit reason.

### TODO-504 Build Public Benchmark Dashboard

- [x] Publish model/version, dataset slice, metric definitions, latest eval
  scores, and artifact links.
- [x] Keep live rank tracking behind an explicit permission gate.
- [x] Add a static-site or simple web build command.

Done when: a public page can show offline benchmark progress without claiming
unauthorized live ladder results.

### TODO-505 Prepare Launch Media

- [ ] Record one replay-analysis clip, one browser-demo clip, and one benchmark
  summary image.
- [ ] Update README with the selected clip and exact reproduction commands.
- [ ] Keep claims limited to verified benchmark and demo evidence.

Done when: README opens with reproducible evidence rather than future-tense
claims.

## P6 Paper And Release

### TODO-601 Run Final Evaluation

- [ ] Freeze dataset slices, model checkpoints, and evaluation scripts.
- [ ] Run final supervised, self-play, Sanma, and interpretability evaluations.
- [ ] Produce tables for accuracy, balanced accuracy, deal-in risk calibration,
  average placement, score delta, and ablations.

Done when: all final metrics are reproducible from documented commands.

### TODO-602 Write Technical Report

- [ ] Write an arXiv-style report covering motivation, related work, data policy,
  architecture, training, evaluation, ablations, limitations, and compliance.
- [ ] Include enough implementation detail to reproduce the experiments.
- [ ] Cite Suphx, Mortal, akochan, MahjongLM, Mahjax, Tenhou policy, and relevant
  simulator/data sources.

Done when: a draft PDF builds from source and all claims link to artifacts.

### TODO-603 Prepare Public Release

- [ ] Choose project name/domain/handles and document trademark checks.
- [ ] Write launch posts for GitHub/HN, Reddit, X/Twitter, and mahjong community
  forums.
- [ ] Prepare outreach list for mahjong creators and leagues.

Done when: release checklist has dated artifacts, links, and owners for every
public channel.
