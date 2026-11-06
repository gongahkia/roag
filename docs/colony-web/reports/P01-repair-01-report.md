# P01 repair 01: viewport-dependent zoom controls

Date: 2026-11-06. Result: **COMPLETE**. P02 has not started.

## Identity and outcome

Requested goal: repair P01's stale zoom-button state after viewport-only changes;
prove shrink/growth cases on both `/` and `/roag/`, retain existing camera/selection/
asset-failure behavior, run affected checks with the pinned toolchain, preserve all
originals and P01 evidence, report separately, and stop before P02. No push/deployment.

Starting and ending HEAD: `88026d076ea2940d244d1f0e38e4644f9dd2ceb1`. The 29 uncommitted
P01 source files were already present; **all 29 matched the original source-hash
inventory before editing**. They were snapshotted under
`artifacts/colony-web/P01-repair-01/before-source/`, with identity/hashes in
`before-source.json`. No earlier interrupted repair process or additional changes
were found. Original P01 report/evidence remain intact.

Repair changes: `web/src/presentation/ColonyScene.ts`, the browser regression in
`web/tests/browser/foundation.spec.ts`, evidence-directory configuration in
`web/playwright.config.ts` and the three build/test scripts, `web/README.md`, STATUS.md
and this report. The repair changes eight existing files and adds this report.
`repair.patch` records the incremental change from the starting uncommitted P01
state; `final-source.json` records the resulting full source inventory and hashes.
Both live under `artifacts/colony-web/P01-repair-01/`. No commit was created.

A player can now zoom out to the valid 100% minimum after shrinking the window from
1440 × 1000 at 200% to 390 × 844. Growth refreshes limits correctly, whether it
requires camera clamping or leaves the entire camera view unchanged. Selection,
integer zoom, bounds and pointer anchoring retain their existing behavior. There
is still no simulation, movement, jobs, combat or saving; this repair adds no P02 work.

## Acceptance evidence

Evidence paths below are relative to `artifacts/colony-web/P01-repair-01/`.
`validation.json` records exact cwd, argv, environment, exit statuses and elapsed
times. All final acceptance commands exited 0. The deliberate before-fix regression
exited 1 and is retained separately, not relabeled as a pass.

| Requirement / P01 gate | Result | Actual command/scenario | Evidence and observed outcome |
| --- | --- | --- | --- |
| Reproduce the confirmed defect | FAIL before fix, as expected | In `web/`, run the new regression on copies of original P01 production builds: `fnm exec --using=24.21.0 npx playwright test --grep 'viewport zoom controls'`, with evidence directory set to `before-browser/` | `regression-before.log`, `before-browser/browser-results.json`, two failure screenshots/traces. Both bases fail specifically at expected-enabled/received-disabled. Center `(390,294)` and zoom 2 remain identical after shrink; error arrays empty. |
| Desktop minimum and shrink with unchanged camera | PASS | New regression starts at 1440 × 1000, selects Ada and clicks zoom-out to 200%; shrinks to 390 × 844 without any intervening camera command | `browser-evidence.json`: at 200%, button changes from disabled to enabled while camera remains exactly `{zoom:2,x:390,y:294}`. Runs independently on both bases. |
| Click to the lower minimum | PASS | Click the newly enabled zoom-out button after shrink | Both deployments reach 100%, disable zoom-out at the valid minimum and preserve selected Ada. Screenshot `zoom-narrow-100-minimum.png` in each regression result directory. |
| Growth with changed and unchanged camera; maximum state | PASS | Grow from 100% to desktop, then shrink/grow again at 200% without camera changes; repeat shrink/growth at 600% | Both bases clamp 100% to 200% on growth. At unchanged 200%, growth disables zoom-out. At unchanged 600%, zoom-in stays disabled and zoom-out stays enabled. Exact views/button flags retained in `browser-evidence.json`. |
| Existing P01 camera, selection, responsive and missing-art journeys | PASS | In `web/`: `fnm exec --using=24.21.0 npm run test:browser`, with repair evidence directory | `production-browser.log`, `browser-results.json`: **10/10 tests**, zero skipped/flaky/unexpected outcomes. Includes all eight original journeys and the regression on each base. Successful journeys have no console/page/network errors. Missing-art tests retain deliberate 404 handling and safe controls. |
| Typecheck, local invariants and production builds | PASS | In `web/`: pinned `npm run typecheck`, `npm test`, `npm run build`, `npm run assets:check`; browser command rebuilds with explicit bases | `typecheck.log`, `local-tests.log`, `production-build.log`, `asset-check.log`, size manifests: 8/8 local tests and relative/root/subpath builds succeed. Checks include pixel/crop/gutter integrity, camera bounds/anchoring, stable selection and distribution exclusion. |
| Preservation before/after and retained P01 evidence | PASS | At repository root: `python3 -B tools/verify_preservation.py` before/after; compare snapshots and archive receipts | `preservation-before.log`, `preservation-after.log`, `integrity.json`: all 1,163 originals pass; protected manifest unchanged; all 122 original P01 evidence files unchanged; original P01 report unchanged; both preservation archives match their receipt hashes. |

## Port and design decisions

| Source | Retained invariant | Intentional adaptation | New test/player scenario |
| --- | --- | --- | --- |
| `web/src/presentation/ColonyScene.ts::applyView` | Camera is constrained and applied; unchanged serialized camera avoids redundant camera-data/zoom-label writes | Refresh the two viewport-dependent button states before the unchanged-view early return | Shrink and grow with identical center/zoom, plus click-to-minimum and maximum-state cases |
| Existing `view.ts`, UI commands and original P01 selection/asset boundaries | Integer zoom, cursor anchoring, bounds, selection and safe failure remain owned by existing presentation code | No change to their algorithms or content | Existing eight local tests and eight original browser journeys rerun |
| Existing build/test scripts and Playwright config | Default output remains P01 for existing callers | Optional `ROAG_EVIDENCE_DIR` redirects size reports, both builds/servers and browser artifacts together | Before/after browser runs stay under the repair directory; original 122 evidence files compare equal |

The runtime fix moves two existing button assignments above the cache return and
adds one explanatory comment. It does not remove the serialization optimization or
change the camera cache key/zoom rules. Supporting test configuration is necessary
to retain old evidence. Paths are relative to `web/` unless absolute. README.md
explains the override and resize regression. No dependencies, versions, package or
lockfile, selection input, artwork, content schema, product assumptions or
DECISIONS.md changed. No C#/Lua rules were ported.

Installed Phaser 3.90 ScaleManager resize behavior and Vite output-path types were
inspected locally. No new library API or framework was introduced. Existing pinned
toolchain remains Node **24.21.0**, npm **11.19.0**, TypeScript **5.9.3**, Phaser
**3.90.0**, Vite **8.3.6**, Playwright **1.65.0**, pngjs **7.0.0**.

## Preservation and scope

- Preservation checker: **PASS, 1,163 files, zero failures**, before and after repair.
  The manifest itself is also compared by SHA-256, not regenerated/rebaselined.
- Both local preservation archives still match `preservation-receipt.json`. Legacy
  source, original assets, archive/tag, catalog tools/tests and original P01 report
  remain unchanged. All 122 original P01 artifact files were compared by SHA-256.
- No artwork or alias was added. All four shipped game-asset files are byte-identical
  to original P01 output on both bases; 26 ID/crop bindings and 17,786 raw runtime
  asset bytes remain unchanged, including the 3,107-byte PNG.
- Nine-file distribution checks pass; full catalog, research atlas and archives remain
  excluded. Only application JS changes in the shipped scene; CSS/art remain intact.
- Changes outside `web/` are STATUS.md and this report. Existing CI is untouched.
- No deletion, source cutover, push, deployment, external write or P02 work occurred.

## Tests, browser, saves, and performance

Exact acceptance commands executed from `web/` with the existing installed, pinned
Node dependencies (the runner supplied the absolute form of the evidence path):

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P01-repair-01
fnm exec --using=24.21.0 npm run typecheck
fnm exec --using=24.21.0 npm test
fnm exec --using=24.21.0 npm run build
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 npm run test:browser
```

At repository root, before and after: `python3 -B tools/verify_preservation.py`.
`dependencies.log` records installed direct package versions; `toolchain.log` and
`npm-version.log` record Node/npm. `npm ci`, catalog and .NET suites were not rerun:
no dependency/lockfile/catalog/legacy changes were made. Earlier clean-install and
38-test catalog evidence remains in the original P01 report; it is not claimed as
a new repair-run result.

Browser: Playwright Chromium **158.0.8127.18**, Linux x86_64/WSL2. Production mounts
are `http://127.0.0.1:4173/` and `http://127.0.0.1:4174/roag/`; strict static servers
were started and stopped by Playwright. No local development server substituted for
production. Viewports include 1440 × 1000, 960 × 720 and 390 × 844. New regression
captures prove exact unchanged camera values, button properties and selected name;
all six founder names/roles, drag, wheel, keyboard, empty-ground selection, credits,
reload and missing-art safety remain covered by the original journeys.

There are 22 final screenshots and two retained before-fix failure traces. These
three repair screenshots were opened with `view_image` and visually inspected:

- `browser-results/foundation-viewport-zoom-c-bf7ce-leaves-the-camera-unchanged-root/zoom-shrink-200-enabled.png`: 390px layout at 200%, enabled minus button, Ada still selected and readable inspector.
- `browser-results/foundation-viewport-zoom-c-bf7ce-leaves-the-camera-unchanged-roag/zoom-narrow-100-minimum.png`: button click reached 100%, disabled minus button, crisp selected artwork and unchanged readable DOM inspector.
- `browser-results/foundation-viewport-zoom-c-bf7ce-leaves-the-camera-unchanged-root/zoom-grow-200-disabled.png`: grown desktop at 200%, correctly disabled minus button, Ada selection persists.

`visual-review.json` retains reviewed image hashes/observations. All screenshot paths
and structured before/after observations are in `browser-evidence.json`. Runner
color-environment warnings are harmless terminal output, not browser errors. The
missing-art tests intentionally generate 404s; successful journeys and new regression
have empty error arrays and HTTP requests confined to their configured origin/base.

| Production variant | Files | Raw bytes | gzip bytes | Brotli bytes |
| --- | ---: | ---: | ---: | ---: |
| Relative `web/dist/` | 9 | 1,238,887 | 330,196 | 265,298 |
| Explicit `/` | 9 | 1,238,882 | 330,193 | 265,293 |
| Explicit `/roag/` | 9 | 1,238,907 | 330,205 | 265,298 |

Compression totals sum each file independently at gzip level 9/Brotli quality 11;
actual HTTP transfer depends on hosting. Per-file hashes/sizes are in
`build-sizes.json`, `root-sizes.json`, `roag-sizes.json` under the repair directory.
No frame-time, active-AI throughput, long-run memory or save benchmark was required
or measured. There is no simulation seed, gameplay command replay or save schema in
P01. Reload still rebuilds the static fixture; saved continuation is not claimed.

Remaining limits: Chromium/WSL2 only; no physical touch, other browser engines,
screen-reader audit, hosted URL or remote CI execution. No known remaining defect
in the requested repair was found. The original P01 scope limits still apply.

## Resume and recommendation

Start locally from repository root:

```sh
cd web
fnm exec --using=24.21.0 npm run dev
```

Open the printed `http://127.0.0.1:5173/` URL. At 1440 × 1000, select Ada and zoom out
to 200% (minus disabled). Resize to 390 × 844: minus should enable without moving
or zooming the camera; click it to reach 100% (minus disabled). Grow to desktop:
zoom clamps to 200%. Shrink/grow at 200% again: camera stays the same while minus
switches enabled/disabled. At 600%, plus remains disabled through both sizes.

The acceptance block above reproduces checks without overwriting P01 evidence.
With the pinned Node selected, bare `npm ...` commands are equivalent. For manual
inspection of the exact production outputs after the browser command, from `web/`:

```sh
export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P01-repair-01
fnm exec --using=24.21.0 node scripts/serve-production.mjs root
# http://127.0.0.1:4173/
# In another terminal, using the same variable:
fnm exec --using=24.21.0 node scripts/serve-production.mjs roag
# http://127.0.0.1:4174/roag/
```

The browser suite needs ports 4173/4174 free. A fresh environment should first run
`fnm exec --using=24.21.0 npm ci` and
`fnm exec --using=24.21.0 npx playwright install chromium`; those are setup commands,
not additional checks claimed to have run in this repair.

Required repairs: none identified within this goal. Recommendation: review this
repair report and retained red/green evidence, then issue a separate P02 goal only
if accepted. No product decision/access question blocks this repair. Read STATUS.md,
this report, original P01 report, PROMPTS.md, PLAN.md, DECISIONS.md, PRESERVATION.md,
PORT_MAP.md and web/README.md before subsequent implementation.

## Paste-back block

```text
ROAG MIGRATION REPORT
Phase: P01-repair-01
Status: COMPLETE
Source: HEAD 88026d076ea2940d244d1f0e38e4644f9dd2ceb1 plus existing uncommitted P01 implementation and repair. Source snapshots/hashes, repair.patch and final-source.json are under artifacts/colony-web/P01-repair-01/.
Report: docs/colony-web/reports/P01-repair-01-report.md; STATUS.md links this latest repair. Original P01 report and evidence are retained.
Player-visible result: Zoom buttons now refresh after every resize even when camera position and zoom stay unchanged. Starting at desktop 200%, shrinking to 390x844 enables minus immediately; clicking reaches 100%. Growing clamps to the desktop minimum or refreshes buttons without moving an already valid camera. Selection persists.
Acceptance: The regression failed on both original production builds with the reported stale disabled button and exact unchanged camera {x:390,y:294,zoom:2}. After moving the two button updates before the camera-cache return, all 10 production browser tests pass on / and /roag/, with zero skips/flakes. This includes shrink/growth with unchanged views, growth requiring clamping, maximum-zoom states, and all existing camera, selection, responsive and missing-art journeys. Successful journeys have no unexpected console/page/network errors. Eight local invariant tests, strict typecheck, relative and both explicit-base builds, asset checks and preservation all pass.
Checks executed: From web/, export ROAG_EVIDENCE_DIR=../artifacts/colony-web/P01-repair-01, then use fnm exec --using=24.21.0 with npm run typecheck; npm test; npm run build; npm run assets:check; npm run test:browser. From repository root run python3 -B tools/verify_preservation.py before/after. Final commands exit 0. validation.json records exact argv/cwd/environment/exits; browser-evidence.json records camera/button observations. Three repair screenshots were opened and inspected; 22 final screenshots and before-fix failure traces remain separate from original P01 evidence.
Preservation: All 1,163 originals pass. The preservation manifest, original P01 report, all 122 original P01 evidence files and both preservation archives are unchanged. Selected artwork remains byte-identical; full research assets remain excluded.
Ported/changed rules: Two runtime assignments moved, browser regression added, optional evidence-directory override documented. Dependencies, lockfile, camera algorithms, content and gameplay rules unchanged.
Known gaps: Chromium on WSL2 only; no other engines, physical touch, remote CI, deployment or performance benchmark. No dependency reinstall, catalog or .NET rerun was needed. Simulation and saves remain unavailable.
Run/reproduce: cd web; fnm exec --using=24.21.0 npm run dev. Open http://127.0.0.1:5173/, select Ada, zoom to 200%, shrink to 390x844, click minus, then grow and repeat at 200%.
Recommendation: Review this repair, then authorize P02 separately if accepted. P02 has not started; nothing was pushed or deployed.
Questions: None.
END ROAG MIGRATION REPORT
```
