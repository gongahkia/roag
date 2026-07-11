# Sanma Training Data Report

Date: 2026-07-08

Status: blocked pending permitted-data provenance for the 1,000-game training export.

No permitted 1,000-game Sanma replay source is available in this workspace. No raw Sanma replay
data, processed Sanma dataset, account identifier, or replay URL is committed.

## 2026-07-11 Local-Only Evaluation Evidence

An ignored local-only 1,100-log 3-player hanchan slice was fetched sequentially with
`houou-logs` 2.0.1 (`3aeb640659d4ba635739d82ad75e94f826076c76`) and stored under
`data/raw/sanma/hanchan-validation/`. Its upstream policy prohibits redistribution and requires
one download session, both observed here. The reports and bundle remain ignored:

- `runs/todo-403/discard-benchmark.json`: 50,000 streamed real decisions, 10,000 held out.
- `runs/todo-403/call-benchmark.json`: 20,000 balanced real decisions, 4,000 held out.
- `runs/todo-403/riichi-benchmark.json`: 14,184 real decisions, 2,837 held out.
- `runs/todo-403/kita-benchmark.json`: 31,187 real Kita/pass decisions, 6,237 held out.
- `runs/todo-403/sanma-report-bundle-v0.json`: validates with
  `python3 scripts/validate_sanma_report_bundle.py`.

Real Tenhou Sanma XML represents the unused fourth seat as `hai3=""`. Kenjaku now accepts that
canonical `13,13,13,0` starting-hand shape for Kita examples; the local report contains 25,687
decoded Kita calls rather than the previous false zero.

This evidence does not establish player-specific consent or an official Tenhou ML-use grant.
TODO-403 therefore remains open despite the local report bundle.

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
data/fixtures/sanma/sanma_decisions_3p.xml
data/fixtures/sanma/sanma_kita_3p.xml
```

Real Sanma games parsed for training: 0.
Synthetic Sanma plumbing fixtures: 2.

## Snapshot Schema Coverage

| Required Sanma decision | Current local snapshot support | Status |
| --- | --- | --- |
| Discard | `export-decision-snapshots` supports synthetic 3-player discard rows. | Needs validation against permitted real 3-player logs. |
| Call/pass | 4-player call/pass rows exist; synthetic 3-player call windows now suppress chi opportunities. | Needs validation against permitted real 3-player logs. |
| Riichi/pass | `export-decision-snapshots` supports synthetic 3-player riichi rows. | Needs validation against permitted real 3-player logs. |
| Kita | Self-play supports Kita actions, synthetic Tenhou Sanma logs can emit `kita` decision snapshots, and `benchmark-kita` can produce a local frequency-baseline report. | Needs validation against permitted real 3-player logs. |
| Win/pass | Self-play supports ron/tsumo/pass windows. | Missing decision snapshot row type for replay export. |

Parser status: `src/kenjaku/io/tenhou_xml.py` accepts contiguous 3-player `INIT` hands and Tenhou
Nuki meld codes in synthetic coverage. Real permitted Sanma logs are still needed to validate
draw/discard, score, call, riichi, Kita, and terminal event coverage end to end.

Benchmark status: `benchmark-kita data/fixtures/sanma/sanma_kita_3p.xml` covers the Sanma
Kita/pass report path for the checked-in synthetic fixture. It is not a substitute for a held-out
real Sanma evaluation slice.

## Report Bundle Gate

After real Sanma discard/call/riichi/Kita reports exist, write an ignored bundle manifest:

```json
{
  "kind": "kenjaku-sanma-report-bundle-v0",
  "minimum_xml_files": 1000,
  "minimum_eval_decisions": 1,
  "reports": {
    "discard": {
      "path": "runs/todo-403/discard-benchmark.json",
      "model": "defense_context_linear"
    },
    "call": {
      "path": "runs/todo-403/call-benchmark.json",
      "model": "call_linear_v1_calibrated"
    },
    "riichi": {
      "path": "runs/todo-403/riichi-benchmark.json",
      "model": "riichi_linear_calibrated"
    },
    "kita": {
      "path": "runs/todo-403/kita-benchmark.json",
      "model": "kita_frequency"
    }
  }
}
```

Validate it before closing TODO-403:

```bash
python3 scripts/validate_sanma_report_bundle.py runs/todo-403/sanma-report-bundle-v0.json
```

The validator rejects checked-in fixture/synthetic sources, requires the bundle and report paths to
be ignored, requires report input paths to exist under ignored non-fixture paths, enforces the
1,000-file default gate, and checks numeric selected-model loss, accuracy, balanced accuracy, and
per-action recall fields.

## Unblock Checklist

1. Obtain at least 1,000 Tenhou Sanma logs from a user-owned or explicitly consented source.
2. Store raw logs only under ignored local paths such as `data/raw/sanma/`.
3. Add a permission manifest using the existing replay intake workflow before processing.
4. Validate 3-player `INIT`, nuki/Kita events, and Sanma score fields against real permitted logs.
5. Validate `kita` decision snapshots and `benchmark-kita` reports against real permitted logs.
6. Export local-only JSONL and record counts without committing reconstructable logs.

[Inference] Until those items are complete, Sanma model training and final Sanma evaluation remain
blocked by data access and schema coverage, not by a checked-in corpus.
