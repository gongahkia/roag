# Fixtures

All checked-in fixture data here is synthetic or hand-written. No file in this directory is a real
Tenhou log or account-derived replay.

## `synthetic-bc/`

`synthetic-bc/` contains 200 deterministic Tenhou-shaped XML games for behavior-cloning and
benchmark smoke tests. The fixture is intentionally small enough to commit and large enough to
produce nontrivial discard benchmark metrics from a fresh checkout.

Regenerate it with:

```bash
python3.13 scripts/generate_synthetic_bc_fixture.py \
  --output-dir data/fixtures/synthetic-bc \
  --games 200 \
  --seed synthetic-bc-v1 \
  --turn-cycles 4 \
  --overwrite
```

The matching sandbox smoke seed is:

```bash
PYTHONPATH=src python3.13 -m kenjaku self-play-match-sandbox \
  --games 200 \
  --seed synthetic-bc-v1 \
  --ron-policy pass \
  --report runs/synthetic-bc/self-play-match.json
```

Expected fixture shape:

- XML files: 200
- Parsed rounds: 200
- Discard examples: 3200
- Directory size: < 3 MB
