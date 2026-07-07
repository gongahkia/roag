# Sanma Training Data Report

Date: 2026-07-07

Status: blocked for 1,000-game training export.

No permitted 1,000-game Sanma replay source is available in this workspace. No raw Sanma replay
data, processed Sanma dataset, account identifier, or replay URL is committed.

## Source Review

| Candidate | Count available here | Legal/use status | Decision |
| --- | ---: | --- | --- |
| User-owned Tenhou Sanma logs with explicit consent | 0 | [Inference] Potentially usable for local-only work if the owner provides logs and permission. Tenhou documents paid client replay save/analysis features, and community docs note browser replay storage is limited unless links are saved manually. | Blocked until logs and permission manifest exist. |
| Tenhou/Houou log download tooling | 0 | The `houou-logs` README says Tenhou prohibits publishing, sharing, mirroring, or redistributing downloaded logs and warns to use only one download session. | Do not fetch or commit data from this path without explicit review. |
| Meowjong Sanma pipeline | 0 | The repository documents downloading Sanma logs from Tenhou. I cannot verify a redistributable checked-in Sanma corpus from the README and repository listing. | Tooling reference only; not a permitted source. |
| `tenhou-to-mjai` yearly datasets | 0 | The README describes CC BY 4.0 converted data, but also says all datasets are 4-player hanchan games and no 3-player matches are included. | Not applicable to Sanma. |

Sources:

- Tenhou homepage: https://tenhou.net/
- Tenhou replay notes: https://riichi.wiki/Tenhou.net
- `houou-logs`: https://github.com/Apricot-S/houou-logs
- Meowjong: https://github.com/VictorZXY/Meowjong
- `tenhou-to-mjai`: https://github.com/NikkeTryHard/tenhou-to-mjai

## Local Count

```bash
find data -maxdepth 4 -type f -print
```

Current checked-in replay fixtures:

```text
data/fixtures/tenhou/minimal_4p.xml
data/fixtures/tenhou/ryuukyoku_4p.xml
data/fixtures/tenhou/events_4p.xml
```

Sanma games parsed for training: 0.

## Snapshot Schema Coverage

| Required Sanma decision | Current local snapshot support | Status |
| --- | --- | --- |
| Discard | `export-decision-snapshots` supports 4-player discard rows. | Needs 3-player Tenhou parser support before Sanma export. |
| Call/pass | 4-player call/pass rows exist. | Needs Sanma no-chi filtering against real 3-player logs. |
| Riichi/pass | 4-player riichi/pass rows exist. | Needs 3-player parser support. |
| Kita | Self-play supports Kita actions. | Missing decision snapshot row type. |
| Win/pass | Self-play supports ron/tsumo/pass windows. | Missing decision snapshot row type for replay export. |

Parser blocker: `src/kenjaku/io/tenhou_xml.py` currently reads `hai0` through `hai3` during
`INIT`, so real 3-player Tenhou XML with three starting hands is not accepted yet.

## Unblock Checklist

1. Obtain at least 1,000 Tenhou Sanma logs from a user-owned or explicitly consented source.
2. Store raw logs only under ignored local paths such as `data/raw/sanma/`.
3. Add a permission manifest using the existing replay intake workflow before processing.
4. Extend Tenhou XML parsing for 3-player `INIT`, Sanma nuki/Kita events, and Sanma score fields.
5. Extend decision snapshots with `kita` and win/pass row types.
6. Export local-only JSONL and record counts without committing reconstructable logs.

[Inference] Until those items are complete, Sanma model training and final Sanma evaluation remain
blocked by data access and schema coverage, not by a checked-in corpus.
