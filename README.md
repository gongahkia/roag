# kenjaku

Open-source riichi mahjong AI research agent and replay-analysis toolkit.

The current milestone is Phase 0: build a small, tested Python core for tile/state/action
representation, then validate a compliant Tenhou log parsing path before any model training.

Live ladder automation is intentionally out of scope unless a platform grants explicit permission.
See `IDEAS.md` for the research roadmap and implementation log.

## Verify

```bash
PYTHONPATH=src python3 -m unittest discover -s tests
PYTHONPATH=src python3 -m kenjaku --version
PYTHONPATH=src python3 -m kenjaku inspect-tenhou data/fixtures/tenhou/minimal_4p.xml
```

## Data

Raw game logs and derived training datasets are local-only. See `docs/data-policy.md` and
`data/README.md` before adding fixtures or downloader scripts.
