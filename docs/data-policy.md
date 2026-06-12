# Data Policy

Kenjaku should be reproducible without turning the repository into a log mirror.

## Principles

- Keep source data local unless its license and platform policy clearly permit redistribution.
- Prefer scripts, manifests, checksums, and tiny synthetic fixtures over checked-in datasets.
- Record provenance for every experiment: downloader version, command, date range, filters, and seed.
- Keep player/account-derived data out of git unless it is explicitly public and safe to redistribute.
- Treat live-service automation as a permissioned integration, not a default research method.

## Tenhou

Tenhou publishes log access mechanisms, but its manual and current downloader ecosystem impose
real constraints. Phase 0 should use `Apricot-S/houou-logs` for local validation only:

```bash
# Install outside this repo or in a local virtual environment.
uv tool install git+https://github.com/Apricot-S/houou-logs

# Import IDs from a manually downloaded archive.
houou-logs import data/raw/tenhou/2024.db data/raw/tenhou/scraw2024.zip

# Download a tiny local sample. Do not run concurrent download sessions.
houou-logs download data/raw/tenhou/2024.db --players 4 --length h --limit 100

# Export downloaded XML into an ignored local directory for Kenjaku CLI runs.
houou-logs export data/raw/tenhou/2024.db data/raw/tenhou/xml/4p-hanchan \
  --players 4 --length h --limit 100
```

Do not commit the resulting database or exported XML files. See `docs/local-tenhou-eval.md` for a
local evaluation workflow.

Generated reports and model artifacts belong under ignored local paths such as `runs/` and
`models/`. If aggregate results are useful for project history, record only summary counts,
metrics, and source commands in `TODO.md` or docs; do not copy raw XML, database contents, player
records, or full generated artifacts into git without a separate redistribution review.

## Mahjong Soul

Decision as of 2026-06-12: Mahjong Soul replay sharing is not treated as permitted for Kenjaku
offline analysis, evaluation, training, demo, or redistribution unless Yostar or another
rights-holder with authority grants explicit written permission for the exact intended use.

Evidence reviewed on 2026-06-12:

- Official Mahjong Soul site footer links to `https://mahjongsoul.yo-star.com/terms_of_service`
  and `https://mahjongsoul.yo-star.com/privacy_policy`.
- The terms grant only a limited personal, noncommercial service-use license and require use only
  as permitted by service features.
- The service rules prohibit copying, distributing, or disclosing service material by automated or
  non-automated scraping, prohibit high-volume automated access, and prohibit access through
  technology or means not provided or authorized by the service.
- The proprietary-rights section includes in-game chat transcripts, character profile information,
  and recordings of games played using a Yostar client in Yostar content. No public research,
  dataset, replay-export, or replay-redistribution license was found.
- The privacy policy says Yostar collects service usage and actions, including gameplay activity,
  interactions with others, user content, and a unique user ID used to track played games. Its
  displayed last revision date is May 27, 2025.

Allowed workflow: reject Mahjong Soul replay manifest rows by default. Accept them only when
`permission.status` is `explicit_permission` and `permission.scope` covers every requested
`intended_uses` value. Accepted rows remain local-only unless the explicit scope also covers demo or
redistribution.

Blocked workflow: do not scrape Mahjong Soul, automate Mahjong Soul clients, fetch ranked replay
data, mirror replay-share URLs, or use user-provided/public Mahjong Soul links for Kenjaku analysis
without explicit permission. Use synthetic fixtures for tests.

## Replay Intake Manifests

Use `kenjaku replay-intake-review` before adding replay URLs or local exports to an analysis queue.
The command accepts a JSON manifest with `kind: "kenjaku-replay-manifest-v0"` and `items` containing
`id`, `platform`, `uri`, `intended_uses`, and a `permission` block. Accepted rows can be written to
JSONL with `--accepted-output` for later offline tooling.

Supported `permission.status` values are:

- `local_synthetic`: fixture or generated replay data; allowed for analysis, evaluation, training,
  demo, and redistribution.
- `user_provided`: replay supplied by the user; allowed only for analysis and evaluation by
  default.
- `public_replay`: publicly available replay reference; allowed only for analysis and evaluation by
  default.
- `explicit_permission`: requires an explicit `permission.scope` list and is accepted only for uses
  covered by that scope.
- `unknown` and `denied`: rejected.

Tenhou redistribution is rejected by the intake gate. Mahjong Soul replay intake requires explicit
permission for every intended use. The intake command is a provenance and permission review step
only; it does not fetch live-service data or automate a client.

Use `kenjaku replay-share-plan` on accepted intake JSONL before any demo or redistribution work.
The share planner checks that the accepted row's original `intended_uses` and `permission.scope`
cover the requested `--intent demo|redistribution`. It writes a local plan/report only; it does not
post URLs, upload files, or call platform APIs.

## Fixtures

Fixtures under `data/fixtures/` must be synthetic unless a future commit documents why a real
fixture is redistributable. Synthetic fixtures should be intentionally small and should cover parser
edge cases rather than model quality.
