# Public Release Checklist

Status: draft release artifact.
Owner: @gongahkia.
Last checked: 2026-07-07.

This file is the dated planning artifact for the first public release. It keeps claims tied to
reproducible local commands and keeps raw replay data, model checkpoints, ignored reports, and
private player/account data out of public posts.

## Name, URL, And Handles

| Item | Choice | Owner | Status |
| --- | --- | --- | --- |
| Public name | Kenjaku Mahjong Research Toolkit | @gongahkia | selected |
| Canonical source URL | https://github.com/gongahkia/kenjaku | @gongahkia | selected |
| Release URL | https://github.com/gongahkia/kenjaku/releases | @gongahkia | selected |
| Custom domain | none for first release | @gongahkia | defer until DNS and legal checks exist |
| GitHub handle | https://github.com/gongahkia | @gongahkia | selected |
| New social handles | none | @gongahkia | avoid reserving unverified handles |

## Preliminary Trademark And Naming Checks

These are not legal clearance.

| Check | Source | Result | Release action |
| --- | --- | --- | --- |
| USPTO search workflow | https://www.uspto.gov/trademarks | USPTO provides the authoritative U.S. trademark search/apply workflow. Direct clearance was not completed in this repo. | Do not claim trademark rights. |
| International search workflow | https://www.wipo.int/en/web/global-brand-database | WIPO describes the Global Brand Database as a search tool for international trademarks, appellations, 6ter emblems, INNs, and participating offices. | Run formal search before any custom domain, logo, or mark filing. |
| Media-name collision | https://www.viz.com/jujutsu-kaisen and https://jujutsu-kaisen.fandom.com/wiki/Kenjaku | Web search shows Kenjaku is also used as a Jujutsu Kaisen character name. | Use the descriptive full name, avoid anime references, avoid character imagery, and ship no logo that resembles third-party media. |

## Public Channel Checklist

| Channel | Link | Owner | Dated artifact | Status |
| --- | --- | --- | --- | --- |
| GitHub release | https://github.com/gongahkia/kenjaku/releases | @gongahkia | 2026-07-07 draft below | ready to paste |
| Hacker News | https://news.ycombinator.com/submit | @gongahkia | 2026-07-07 draft below | ready to paste |
| Reddit r/Mahjong | https://www.reddit.com/r/Mahjong/ | @gongahkia | 2026-07-07 draft below | check current rules before posting |
| Reddit r/riichi | https://www.reddit.com/r/riichi/ | @gongahkia | 2026-07-07 draft below | check current rules before posting |
| X/Twitter | https://x.com/compose/post | @gongahkia | 2026-07-07 draft below | ready to paste from maintainer account |
| Mahjong community forums | https://www.worldriichi.org/ and https://mahjongeurope.org/ | @gongahkia | 2026-07-07 outreach note below | contact via current site route |

## Evidence Commands

Run these before posting and paste artifact paths into the release notes:

```bash
python3.13 -m pip install -e ".[dev]"
PYTHONPATH=src python3.13 -m unittest discover -s tests
PYTHONPATH=src python3.13 -m compileall -q src tests
python3.13 -m ruff check .
PYTHONPATH=src python3.13 -m kenjaku status --json
PYTHONPATH=src python3.13 -m kenjaku browser-demo --output-dir runs/browser-demo --no-serve
PYTHONPATH=src python3.13 -m kenjaku benchmark-discard data/fixtures/tenhou --epochs 3 --models fast --report runs/fixture-discard-benchmark.json
PYTHONPATH=src python3.13 -m kenjaku benchmark-report-summary runs/fixture-discard-benchmark.json
```

Post only results that were produced by these commands or by later checked artifact paths.

## GitHub Release Draft

Title:

```text
Kenjaku v0.1.0: local riichi mahjong replay-analysis toolkit
```

Body:

```text
Kenjaku is an open-source, local-first riichi mahjong research toolkit.

This release can parse small Tenhou XML fixtures, reconstruct discard/call/riichi decision points,
train dependency-free supervised baselines, export neutral decision snapshots, compare prediction
JSONL files, generate public-safe replay summaries, build a static browser demo, and run a
deterministic sandbox for rules/reward experiments.

Verified scope:
- offline research workflow only
- no bundled trained strong policy
- no live ladder automation
- no raw replay data or private player/account data
- no complete yaku/scoring engine
- no complete Sanma ruleset

Reproduce the local smoke checks:
- python3.13 -m pip install -e ".[dev]"
- PYTHONPATH=src python3.13 -m unittest discover -s tests
- PYTHONPATH=src python3.13 -m compileall -q src tests
- python3.13 -m ruff check .
- PYTHONPATH=src python3.13 -m kenjaku status --json

Use `data/README.md` and `docs/data-policy.md` before running any local replay workflow.
```

## Hacker News Draft

Title:

```text
Show HN: Kenjaku - local-first riichi mahjong replay analysis
```

Body:

```text
I built Kenjaku, an open-source Python toolkit for local riichi mahjong replay analysis and
supervised-baseline experiments.

It currently focuses on Tenhou XML parsing, decision reconstruction, discard/call/riichi baseline
reports, neutral prediction snapshots, public-safe replay summaries, a static browser demo, and a
deterministic sandbox for rules/reward plumbing.

It is not a trained production mahjong agent, does not include raw replay data or model weights,
and does not automate live ladder play. The repo keeps data provenance and redistribution limits
explicit because replay logs can carry platform and player-data constraints.

Repo: https://github.com/gongahkia/kenjaku
```

## Reddit Draft

Title:

```text
Open-source local riichi replay-analysis toolkit
```

Body:

```text
I am preparing a first public release of Kenjaku, a local-first Python toolkit for riichi mahjong
replay analysis and small supervised-baseline experiments.

Current scope:
- Tenhou XML parsing and decision reconstruction
- discard/call/riichi baseline reports over local permitted logs
- neutral decision snapshot and prediction-comparison JSONL
- public-safe replay summary generation
- static browser demo generation
- deterministic sandbox plumbing for rules/reward experiments

Non-scope:
- no bundled trained strong policy
- no raw replay data or model weights
- no live ladder automation
- no complete yaku/scoring engine yet

I would value feedback on the data-policy boundaries, benchmark reporting, and what public-safe
demo artifacts would be most useful for riichi players/researchers.

Repo: https://github.com/gongahkia/kenjaku
```

## X/Twitter Draft

```text
Kenjaku is a local-first Python toolkit for riichi mahjong replay analysis: Tenhou XML parsing,
decision reconstruction, supervised baseline reports, neutral snapshots, public-safe summaries,
browser demo assets, and sandbox experiments.

No live ladder automation or bundled model weights.
https://github.com/gongahkia/kenjaku
```

## Mahjong Forum / League Outreach Note

Subject:

```text
Feedback request: public-safe riichi replay-analysis toolkit
```

Body:

```text
I am preparing a first public release of Kenjaku, an open-source local riichi mahjong replay-analysis
toolkit. It is designed for offline research workflows: parsing permitted local Tenhou XML,
reconstructing decisions, producing baseline reports, exporting neutral snapshots, and generating
public-safe summaries that do not expose raw replay URLs.

It does not automate live play, does not bundle raw replay data, and does not bundle trained model
weights. Before posting more broadly, I would appreciate feedback on data-policy language,
public-safe demo artifacts, and benchmark reporting that would be useful to players, leagues, and
researchers.

Repo: https://github.com/gongahkia/kenjaku
Data policy: https://github.com/gongahkia/kenjaku/blob/main/docs/data-policy.md
```

## Outreach List

| Target | Link | Owner | Ask | Status |
| --- | --- | --- | --- | --- |
| World Riichi Championship | https://www.worldriichi.org/ | @gongahkia | data-policy and benchmark-reporting feedback | not contacted |
| European Mahjong Association | https://mahjongeurope.org/ | @gongahkia | rules/data-policy review route or referral | not contacted |
| Riichi content creators/bloggers | https://jellicodemahjong.wordpress.com/2019/08/27/blogs-and-forums-for-riichi-mahjong/ | @gongahkia | creator feedback and public-safe demo review | not contacted |
| Reddit r/Mahjong moderators/community | https://www.reddit.com/r/Mahjong/ | @gongahkia | release feedback if allowed by rules | not contacted |
| Reddit r/riichi moderators/community | https://www.reddit.com/r/riichi/ | @gongahkia | release feedback if allowed by rules | not contacted |
| Riichi Wiki maintainers/community | https://riichi.wiki/ | @gongahkia | terminology/rules wording feedback | not contacted |
| Tenhou-adjacent researchers | docs/external-baselines.md | @gongahkia | shared-slice baseline protocol feedback | not contacted |

## Claim Guardrails

- Say "offline research toolkit", not "mahjong AI agent".
- Say "baseline reports", not "strength".
- Say "public-safe summary", not "shared replay".
- Say "sandbox score estimates", not "exact scoring".
- Say "narrow Sanma plumbing", not "complete Sanma rules".
- Link any metric claim to a dated local report path.
