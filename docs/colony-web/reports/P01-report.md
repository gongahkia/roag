# P01 browser foundation report

Date: 2026-11-06. Result: **COMPLETE**. P02 has not started.

## Identity and outcome

- Requested goal: implement P01 of PLAN.md under the Common contract, including a
  TypeScript/Phaser/Vite browser foundation, selected preserved art, deterministic
  exporter/provenance, pan/zoom/selection/inspector, root/subpath production browser
  verification, distribution/preservation checks, and this report. Stop at P01.
- Starting and ending HEAD: `88026d076ea2940d244d1f0e38e4644f9dd2ceb1` (`goodbye`).
  Starting tracked/untracked working tree was clean. The P00 preservation baseline
  remains `4d89585c13cfcb6874e48a0d8ef64c3a2a1001df`; these are distinct identities.
- Changes remain uncommitted. Exact changed-file list, sizes and SHA-256 hashes:
  `artifacts/colony-web/P01/changed-files.json`. Complete tracked and new-file patch:
  `artifacts/colony-web/P01/implementation.patch`. No commit, push or publication.
- Changed paths: new `web/` project and `.github/workflows/colony-web.yml`, updated
  `docs/colony-web/STATUS.md`, and this new report. Generated artwork, dependencies,
  distribution files, screenshots and logs are ignored.
- Player-visible result: open a woodland founding scene with six named founders,
  cottages, colony heart, hearth, food and crops. Drag, wheel, keyboard or buttons
  move the bounded camera and zoom. Select a founder by clicking the real canvas
  or choosing their name; read name, duty, location and original crop provenance.
  Credits are available through a keyboard-accessible native dialog and text file.
- Unavailable by design: simulation, autonomous movement, jobs, needs, combat, defeat
  execution and saves. Nothing here claims P02-P09 gameplay completion.

## Acceptance evidence

Paths in this section are relative to `artifacts/colony-web/P01/` unless specified.
All recorded commands exited 0. `validation.json` records actual cwd, complete argv,
exit code and elapsed time. On this workstation npm commands were executed through
`fnm exec --using=24.21.0 npm ...` to avoid a different Node on the login-shell PATH.

| P01 plan gate | Result | Actual command/scenario | Evidence and observed outcome |
| --- | --- | --- | --- |
| 1. Clean npm install, typecheck, tests and production build | PASS | In `web/`: `npm ci`; `npm run typecheck`; `npm test`; `npm run build` | `npm-ci.log`, `typecheck.log`, `tests.log`, `final-build.log`. Clean install, zero audit vulnerabilities reported, 8/8 local tests, strict TypeScript and Vite production build succeed. |
| 2. Production at `/` and `/roag/`, camera/selection and screenshot inspection | PASS | In `web/`: `npm run test:browser` rebuilds with explicit `--base=/` and `--base=/roag/`, then serves strict static directories on ports 4173/4174 | `final-browser.log`, `browser-results.json`, `browser-evidence.json`, 16 screenshots under `browser-results/`. 8/8 browser tests, zero skips/flakes. Both successful journeys recorded 18 local HTTP requests across initial load/reload, zero console/page/network errors and no requests outside the configured prefix. |
| 3. ID/crop manifest, selected output only, raw/compressed sizes | PASS | In `web/`: `npm run assets`; `npm run assets:check`; `npm test`; `npm run build`; browser runner's distribution checks | `assets.log`, `asset-check.log`, `tests.log`, `build-sizes.json`, `root-sizes.json`, `roag-sizes.json`. All 26 IDs/crops verified against canonical catalog hashes; deterministic four-file export; final distribution is nine allowed files. Negative tests reject corrupt/extra assets, research files and escaped base paths. Sizes below. |
| 4. Protected originals intact, exact commands/versions | PASS | From repository root: `python3 -B tools/verify_preservation.py` before/after; `python3 -B -m unittest discover -s tests/catalog -v` | `preservation-before.log`, `final-preservation.log`, `catalog-tests.log`, `versions.json`. 1,163 originals pass with zero failures; all 38 original catalog tests pass without changes or skips. |

Additional local startup smoke: `fnm exec --using=24.21.0 npm run dev -- --port 5173
--strictPort`, followed by HTTP requests for `/`, `/src/main.ts`, manifest and PNG:
all 200 (`dev-smoke.json`). Server stopped after checking. Production tests own and
stop their static servers; the local dev server is not the production evidence.

### What the tests establish

The eight local tests cover exact repeat-export bytes; every selected crop's
foreground RGBA and transparent gutters; corrupt/extra output and unknown IDs;
all authored terrain/people/structure bindings; rejection of research files,
altered artwork and incorrect deployment prefixes; every camera edge and integer
zoom; cursor-anchored zoom; unusually large resized displays; and stable distinct
founder IDs, hit areas and readonly presentation data.

Each deployment runs four browser tests: the complete production interaction
journey; resize/narrow-layout journey; missing manifest; missing PNG. The main
journey verifies nonblank canvas pixels, pointer selection after camera changes,
drag without accidental selection, wheel zoom, keyboard pan/reset/deselect,
min/max zoom, bounded drag, camera buttons, DOM keyboard-focus isolation, roster
focus, original ID displayed in provenance, native credits open/close, empty-ground
deselection and actual reload rendering. Resize checks real canvas dimensions,
click hit testing, all six name/role pairs and absence of horizontal page overflow.
Expected asset-404 probes are separate from clean journeys. They show a visible
alert and verify that clicking roster/zoom after texture failure raises no exception.

### Failures and fixes retained

- Initial shell setup attempts invoked npm from repository root and encountered a
  missing package.json; an empty new root lockfile was removed. A login shell in
  `web/` initially selected Node 26.9.0/npm 11.19.1 and was correctly rejected by the
  strict version gate. All reported acceptance runs explicitly used the pinned Node.
- `browser-attempt-1.log`: the distribution checker rejected valid URLs generated by
  an explicit `/` build. Fixed it to check confinement to the requested base while
  still rejecting root-absolute URLs in the default relative build. Added a negative
  distribution/base-path regression test.
- `browser-attempt-2.log` and `browser-attempt-2-results/`: test incorrectly counted
  Phaser data-URI fallback textures as HTTP traffic; now captures actual HTTP(S)
  traffic and checks both origin and prefix. Real resize defect: Phaser included the
  viewport's CSS border in its measurement, producing a two-pixel overflow. Replaced
  border with an outline; tests require canvas width to equal the viewport client width.
- `browser-attempt-3.log`: all eight browser tests passed after those fixes. Subsequent
  final review identified that the error-state string could admit scene commands
  before a failed texture had created selection graphics. Commands now require the
  exact ready state, and both missing-PNG browser cases click the controls to cover it.
- `final-build.log` and `final-browser.log`: final implementation passes after that
  repair. Earlier logs and failed traces remain available; no failed attempt was
  relabeled as a pass. Playwright output includes harmless host color-environment
  warnings; these are runner output, not browser-console errors.

## Port and design decisions

| Legacy source | Retained invariant | Intentional adaptation | New test/player scenario |
| --- | --- | --- | --- |
| `assets/catalog.json.gz`, preserved original PNGs; catalog contract tests | Original IDs, source paths, rectangles, neutral names, identity confidence and crop pixels remain authoritative | 26 local aliases selected independently of old game bindings; build-time black background removal in a derived sheet only | Byte/pixel/gutter checks and original ID/crop inspection in the browser |
| `assets/CREDITS.txt` | Credit actual shipped packs and preserve original sources | Include Urizen and Godot CC0 credits only; system text fonts mean no font files/licenses are shipped | Native credits dialog, local credits-file HTTP request |
| PORT_MAP.md browser/presentation boundary; no C#/Lua mechanic port in P01 | Render-only input/inspection do not own gameplay state | Readonly named-founder fixture; typed presentation commands own camera/selection only | Clicking, focusing, panning and reloading the scene; no simulated progress claim |

P01 uses an authored 64 × 48 scene (768 × 576 world pixels), six founders and integer
camera zoom steps, normally 2–6 on desktop. The 128 × 128 map in DECISIONS.md is a
future measured default, not a P01 generation promise. Large displays raise minimum
zoom enough to remain within camera bounds. These are presentation choices within
P01; DECISIONS.md and confirmed defeat semantics are unchanged.

No new art is drawn or generated. Simple backgrounds and selection outlines are
presentation overlays. No neighboring crops are treated as animation frames;
collision, worker behavior and species are not inferred. No ECS, server framework,
Next.js, generated rules, Lua or C# runtime was introduced. Pure camera/selection
helpers are separate from Phaser, and there is no simulation module yet.

Pinned versions: Node **24.21.0**, npm **11.19.0**, Phaser **3.90.0**, Vite **8.3.6**,
TypeScript **5.9.3**, Playwright **1.65.0**, pngjs **7.0.0**, @types/node **24.19.2**,
@types/pngjs **6.0.5**. The lockfile records all transitive versions. PNG decoding is
a development/export dependency and never enters the browser graph. Official docs
consulted: [Vite build/base](https://vite.dev/guide/build),
[Phaser 3.90 camera API](https://docs.phaser.io/api-documentation/3.90.0/class/cameras-scene2d-camera),
[Phaser configuration](https://docs.phaser.io/phaser/concepts/game),
[Playwright servers](https://playwright.dev/docs/test-webserver) and
[browser installation](https://playwright.dev/docs/browsers). Installed types/CLI
were verified with successful compilation/build. The Sites skill was used only for
its local-preview workflow; no Site registration or publishing was performed.

## Preservation and scope

- Read-only original-file verification passed before and after implementation:
  **1,163 files, zero failures**. The additional 38-test preserved catalog suite passes.
- No protected file, original PNG, original content, old source/test/tool or archive
  was changed/deleted. The global AGENTS.md symlink was not edited or replaced.
- `web/content/asset-selection.json` is the tracked selection. Generated
  `web/public/game-assets/manifest.json` records original ID, source path, source-file
  SHA-256, original rectangle/RGBA hash, original name/confidence and packed frame
  for every alias, plus canonical gzip hash, image hash and transformation rule.
- Founder crops: `urizen-onebit-r00-c105`, `c117`, `c106`, `c112`, `c104`, `c108`
  (each shorthand after the first shares the `urizen-onebit-r00-` prefix). Terrain,
  structures, wheat/apple/carrot and Godot icon IDs are explicit in the selection.
- Derived PNG: **160 × 80, 3,107 bytes**, SHA-256
  `0a9985e7e21b147f021b00590fc06a48c913a734abcf9b5dee309ded4e0850cd`.
  Four runtime files total **17,786 bytes raw**. All are ignored and reproducible.
- Shipped runtime files are exactly `colony.png`, `colony-atlas.json`, `manifest.json`,
  `CREDITS.txt`. The compact colony atlas contains only selected frames. No full
  catalog, research atlas/browser, archives, original full spritesheet, font, old
  save, source map, legacy runtime or research document appears in the distribution.
- Source/workbench preservation archives and tag remain local as described in P00.
  No recovery/deletion/cutover was required; no uploaded backup is claimed.
- Changes outside `web/` are this report, STATUS.md and the scoped web CI workflow.
  The existing .NET workflow remains in place. CI has not run on GitHub in this round;
  its corresponding local checks did run. There is no deployment job.

## Tests, browser, saves, and performance

Test platform: Linux x86_64, WSL2 kernel 6.18.33.2, glibc 2.43; Playwright Chromium
**158.0.8127.18** with its Ubuntu 24.04 fallback build. Playwright notes this host OS
is not officially supported; the installed browser actually ran successfully.
Viewports: **1440 × 1000**, **960 × 720**, **390 × 844**. Initial desktop canvas:
1046 × 710, **48 distinct RGBA colors** on both deployments. Eight browser tests,
zero skipped/unexpected/flaky outcomes. Successful page journeys have zero errors;
404 probes intentionally generate missing-art responses.

Screenshots were opened and visually inspected, not merely captured. All 16 final
screenshot paths are listed in `browser-evidence.json`. Representative final files:

- `browser-results/foundation-production-artw-bc9d9-ds-UI-isolation-and-credits-root/02-selected-provenance.png`
- `browser-results/foundation-production-artw-bc9d9-ds-UI-isolation-and-credits-roag/05-credits.png`
- `browser-results/foundation-resize-and-narr-04170--inspector-and-all-founders-roag/resize-960.png`

Additional inspected final captures include the 390px layout and 400% zoom; inspected
successful captures in `browser-attempt-3-results/` include 200% and 600% zoom.
Exact reviewed image paths and observations are in `visual-review.json`.
Observed crisp crop edges and intact selection
outline; readable names/roles/provenance and credits; no sheet gutters, neighboring
sprites or horizontal page overflow visible. Narrow layout stacks the map and
information; this is mouse-driven responsive testing, not a touch-device certification.

| Production variant | Files | Raw bytes | gzip bytes | Brotli bytes |
| --- | ---: | ---: | ---: | ---: |
| Default relative `web/dist/` | 9 | 1,238,887 | 330,195 | 265,290 |
| Explicit `/` | 9 | 1,238,882 | 330,196 | 265,288 |
| Explicit `/roag/` | 9 | 1,238,907 | 330,206 | 265,295 |

Totals sum independently compressed files: gzip level 9 and Brotli quality 11;
they are reproducible size measurements, not claims about the server's HTTP
compression settings. Per-file sizes/hashes are in the three size JSON artifacts.
Phaser's shared vendor bundle is 1,194,425 raw bytes and dominates the total.
No active-AI, frame-time/throughput, long-run memory or save-growth benchmark was
required or measured. Six static residents, no jobs/events, no scenario seed or
simulation commands exist. Reload reconstructs the fixture; no save/continuation
or visibility-time behavior is implemented or claimed.

Remaining verification limits: Chromium only; no Firefox/Safari/WebKit, real touch
hardware, screen-reader audit, public-host URL or live GitHub Actions run. There is
no known blocking P01 defect. P02 functionality remains unavailable intentionally.

## Resume and recommendation

Use Node 24.21.0/npm 11.19.0. Exact local commands from repository root:

```sh
cd web
fnm exec --using=24.21.0 npm ci
fnm exec --using=24.21.0 npm run dev
# Open the printed http://127.0.0.1:5173/ URL; drag/scroll; click a white founder.
# Select Ada Rowan in the roster to focus her, then expand Artwork provenance.
```

With that Node already selected, bare `npm ...` commands are equivalent. Build and
reproduce all acceptance checks from `web/`:

```sh
npm run assets
npm run assets:check
npm run typecheck
npm test
npm run build
npx playwright install chromium
npm run test:browser
python3 -B ../tools/verify_preservation.py
python3 -B -m unittest discover -s ../tests/catalog -v
```

`npm run preview` serves the relative production build. The browser runner instead
builds both explicit bases and starts strict servers itself; ports 4173/4174 must
be free. After running it, inspect the exact static variants manually using:

```sh
node scripts/serve-production.mjs root   # http://127.0.0.1:4173/
node scripts/serve-production.mjs roag   # http://127.0.0.1:4174/roag/
```

Minimal Linux CI may need `npx playwright install --with-deps chromium`. The web
workflow already includes it and runs preservation before/after checks. See
`web/README.md` for layout, exporter and dependency-boundary details.

Required repairs before proceeding: **none identified**. Next useful action is
review of this P01 report, code and evidence against the plan. If accepted, P02's
separate goal should establish fixed-tick determinism, movement/navigation, world
reachability, saved continuation, reload and hidden-tab behavior. P02 was not
started here. No unresolved product/access question blocks P01.

A fresh session should read STATUS.md, this report, DECISIONS.md, PRESERVATION.md,
PORT_MAP.md, PROMPTS.md, PLAN.md and web/README.md before making new changes.

## Paste-back block

```text
ROAG MIGRATION REPORT
Phase: P01
Status: COMPLETE
Source: HEAD 88026d076ea2940d244d1f0e38e4644f9dd2ceb1 plus uncommitted changes; exact file hashes/list and patch at artifacts/colony-web/P01/changed-files.json and implementation.patch.
Report: docs/colony-web/reports/P01-report.md; STATUS.md updated.
Player-visible result: A preserved-art woodland colony scene with six named founders, terrain, buildings, food and crops. Drag, wheel, WASD/arrows or accessible buttons pan/zoom a bounded camera. Click a founder or roster name to inspect name, duty, location and original sprite ID/crop. Credits open in a native dialog. Responsive desktop/narrow layouts work.
Acceptance: All four P01 gates pass. Clean npm installation, strict typecheck, eight local tests and production build succeed. Eight real Chromium browser tests pass across separately built / and /roag/ deployments, with zero skipped/flaky tests. Successful journeys report zero console/page/network errors; missing-manifest/PNG tests deliberately verify visible failure and safe controls. All 26 crop IDs, rectangles and source pixel hashes validate; deterministic export and distribution rejection tests pass. Preservation passes for 1,163 originals; all 38 existing catalog tests pass.
Checks executed: In web/, using Node 24.21.0/npm 11.19.0: npm ci; npm run assets; npm run assets:check; npm run typecheck; npm test; npm run build; npx playwright install chromium; npm run test:browser. Repository root: python3 -B tools/verify_preservation.py and python3 -B -m unittest discover -s tests/catalog -v. Acceptance commands exit 0. Exact cwd/argv/exits are in artifacts/colony-web/P01/validation.json; final-browser.log, browser-evidence.json and 16 screenshots retain evidence. Screenshots were opened at desktop/narrow sizes and multiple integer zoom levels.
Preservation: Original artwork, sources, content, tools, tests and archives remain intact. Four generated runtime asset files total 17,786 bytes, including a 3,107-byte PNG. Full catalogs/research atlas/archives are excluded. /roag/ distribution: 1,238,907 raw bytes; 330,206 gzip; 265,295 Brotli.
Ported/changed rules: Presentation only; no gameplay rules ported. Dependencies are pinned with a lockfile. Scoped web CI added; existing .NET workflow retained.
Known gaps: Static fixture only; no simulation, movement, jobs, combat or saves. Chromium on WSL2 tested; other engines, physical touch, screen readers and active-AI performance untested. CI has not run remotely; no publication or push occurred.
Run/reproduce: cd web; fnm exec --using=24.21.0 npm ci; fnm exec --using=24.21.0 npm run dev. Open http://127.0.0.1:5173/, select Ada Rowan, then drag/scroll and expand Artwork provenance. npm run build and npm run test:browser reproduce production verification with the pinned Node selected.
Recommendation: Review P01, then proceed to a separately authorized P02 if accepted. P02 has not started.
Questions: None blocking P01.
END ROAG MIGRATION REPORT
```
