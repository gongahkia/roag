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
```

Do not commit the resulting database or exported XML files.

## Mahjong Soul

Mahjong Soul ranked automation is out of scope unless written permission is obtained. Replay
analysis should start from user-provided replay URLs or local exports, and tests should use
synthetic fixtures.

## Fixtures

Fixtures under `data/fixtures/` must be synthetic unless a future commit documents why a real
fixture is redistributable. Synthetic fixtures should be intentionally small and should cover parser
edge cases rather than model quality.
