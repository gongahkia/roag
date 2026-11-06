# P00 preservation and planning report

Date: 2026-11-06. Result: COMPLETE.

Baseline and current HEAD: `4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`.
The starting tracked/untracked working tree was clean. The ignored derived catalog
export was already present and stale. This round adds README.md, docs/colony-web/,
and tools/verify_preservation.py without modifying existing tracked files. It also
creates a local preservation tag and ignored archive artifacts. Nothing is pushed.

## Delivered

- Complete tracked source archive and local tag, retaining C#/Lua implementations,
  old tests and tools before future runtime removal.
- Asset workbench preserving the whole on-disk assets directory, baseline docs/images
  and all other docs, authored content, asset references, catalog tooling and tests.
- Original-file integrity manifest, recovery receipt and read-only checker.
- Detailed P01-P09 plan, source-to-mechanic port map, decisions, one-phase goal prompts,
  review/repair loop, and a structured implementation report/paste-back template.
- User-confirmed heart-destruction OR all-colonists-dead defeat rule recorded.

## Acceptance evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Baseline source checkpoint | PASS | `archive/pre-web-colony-2026-11-06` resolves to baseline HEAD. |
| Complete source archive | PASS | 2,393 tracked file contents compared; archive hash/size in preservation-receipt.json. |
| Portable asset workbench | PASS | 1,164 source file contents compared, including ignored catalog export; manifest packaged too. |
| Live original integrity | PASS | `python3 -B tools/verify_preservation.py`, exit 0; 1,163 files, zero failures. |
| Catalog contracts | PASS | `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/catalog -v`, exit 0; 38 tests, zero skipped, 4.274 seconds. |
| Recovery exercise | PASS | Extracted source archive under `/tmp/roag-preservation-vmk6efj_`; checker exits 0 against original manifest. |
| Corruption/deletion detection | PASS | In isolated restored copy, appended bytes and then removed CREDITS.txt; both checker invocations exit 1 and identify the path. Restored original afterward. |
| Full generator freshness | PASS | `python3 -B tools/build_game_catalog.py --check` in recovered tracked snapshot, exit 0. No ignored stale JSON in that snapshot. |

The temporary recovery location is disposable; durable instructions/receipts are
under docs/colony-web and local artifacts/preservation/2026-11-06-pre-web. All archive
hashes and counts are in preservation-receipt.json. Validation details and generator
stdout are retained in validation.json and catalog-freshness.log beside the archives.

## Scope and limitations

No web app, gameplay port, new artwork, package install or deployment was performed.
No gameplay tests were run. The archived local JSON export remains stale; canonical
catalog.json.gz is current. Archive copies are local and ignored by Git, not uploaded
backups. Planning files/checker are uncommitted working-tree additions at this point.

The plan makes first playability explicit at P05; later content is not a prerequisite
for seeing the new game. It retains C# design invariants while replacing party turns,
desktop I/O and rendering. Vercel remains an alternative static host to Pages, not
an alternative programming stack. Specific hosting destination remains a release-time
question and does not block P01.

## Resume

Read PLAN.md, DECISIONS.md, PRESERVATION.md, PORT_MAP.md and PROMPTS.md in this directory's
parent. Paste the P01 goal into the implementation instance. The next report must
show real browser rendering/interaction and production builds at root/subpath, not
just installed packages. Review that report using the review prompt before P02.

```text
ROAG MIGRATION REPORT
Phase: P00
Status: COMPLETE
Source: 4d89585c13cfcb6874e48a0d8ef64c3a2a1001df plus new README.md, docs/colony-web/ and tools/verify_preservation.py
Report: docs/colony-web/reports/P00-report.md
Player-visible result: No gameplay changed; preservation and implementation instructions are ready.
Acceptance: Complete old source snapshot (2,393 files), whole asset workbench (1,164 source files), original integrity manifest (1,163 files), source tag, recovery exercise, detailed phases/prompts/report loop delivered.
Checks executed: Integrity checker passes; 38 catalog tests pass with zero skips; source/workbench contents match originals; restored-source check passes; deliberate changed/missing file probes fail correctly; full generator --check passes in restored tracked snapshot.
Preservation: archive/pre-web-colony-2026-11-06; portable archives and receipts under artifacts/preservation/2026-11-06-pre-web. Original tracked files unchanged.
Ported/changed rules: None implemented. User confirmed defeat on heart destruction or all actual colonist deaths. TypeScript, Phaser, Vite selected; Pages default with Vercel compatible.
Known gaps: No web app or deployment yet. Ignored assets/catalog.json is stale and retained; gzip is canonical. Local archives/tag are not uploaded. Planning/checker additions are uncommitted.
Run/reproduce: python3 -B tools/verify_preservation.py; follow PRESERVATION.md for isolated recovery. P01 will introduce web commands.
Recommendation: Proceed to P01 using PROMPTS.md, then review its evidence before generating the P02 or repair goal.
Questions: None blocking P01; deployment destination will be selected later.
END ROAG MIGRATION REPORT
```
