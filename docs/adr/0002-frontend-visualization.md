# ADR 0002: Frontend Visualization Architecture

Status: Accepted

Date: 2026-07-07

## Context

Kenjaku needs local visualization for training, testing, and gameplay artifacts. The current
repository already has these relevant surfaces:

- `src/kenjaku/browser_game.py`: static HTML/CSS/JS gameplay generator with tile
  rendering and no build step.
- `kenjaku benchmark-dashboard`: static HTML from benchmark JSON reports.
- `kenjaku training-dashboard`: static HTML from training metrics JSONL.
- `kenjaku replay-viewer`: static HTML from self-play trajectory JSONL.
- `kenjaku interpretability-overlay`: static HTML from decision snapshots.
- `kenjaku serve`: local artifact index/server for generated run outputs.

The project has no runtime frontend package manager, bundler, FastAPI dependency, or TensorBoard
dependency in `pyproject.toml`.

## Decision

Use Option A as the baseline: static artifact-generated HTML pages from JSON/JSONL run outputs,
served by `kenjaku serve` when a local HTTP server is useful.

Do not grow `browser_game.py` into one large app. Keep each artifact viewer owned by its command,
then extract shared tile/CSS/chart helpers only when duplication appears in at least two viewers.
Reuse `browser_game.py` tile semantics for gameplay views by extracting them into shared static
asset helpers before adding another board-heavy viewer.

Keep TensorBoard, React/Vite, and FastAPI/HTMX as optional later integrations, not the default
frontend architecture.

## Options

### Option A: Static HTML Generators

Generate self-contained HTML from local artifacts:

- Training metrics JSONL -> training curves and hyperparameter table.
- Benchmark report JSON -> model comparison tables.
- Self-play trajectory JSONL -> replay timeline.
- Decision snapshots -> interpretability overlays.

Pros:

- Matches the current dependency-free CLI model.
- Works with committed JSON/JSONL fixtures and private local run artifacts.
- Avoids a frontend build step for public docs and offline demos.
- Keeps artifacts shareable as files.
- Fits the existing `browser_game.py` pattern.

Cons:

- Shared UI code can drift if every command owns its own HTML helpers.
- Very large artifacts need pagination/windowing in generated HTML.
- Live training updates need explicit refresh or regeneration.

### Option B: TensorBoard

Write TensorBoard event files from training commands.

Pros:

- Good fit for scalar curves, histograms, and training-time inspection.
- PyTorch exposes `torch.utils.tensorboard.SummaryWriter` for event files.

Cons:

- Adds optional dependency and user workflow outside `kenjaku`.
- Does not cover gameplay/replay review well.
- Produces a separate artifact format alongside the project JSON/JSONL contract.

Use later as an optional export, not the primary UI.

### Option C: React + Vite App Under `web/`

Ship a static app that reads local JSON/JSONL artifacts.

Pros:

- Better for complex reusable client-side interactions.
- Vite production builds target static hosting.
- Component reuse can reduce duplicated dashboard code.

Cons:

- Adds Node/Vite toolchain and dependency maintenance.
- Introduces a second build/test stack for a Python-first repo.
- Browser file-access rules complicate direct local JSONL loading unless served.

Revisit when static generators share enough JS/CSS that a frontend build becomes cheaper than
maintaining inline assets.

### Option D: FastAPI + HTMX Server

Serve local artifacts with Python routes and HTML fragments.

Pros:

- Good for live refresh, large-artifact pagination, and server-side filtering.
- FastAPI supports static-file serving.
- HTMX can add interaction through HTML attributes instead of a full SPA.

Cons:

- Adds server dependencies and a running process to inspect static artifacts.
- More security surface around local file browsing.
- Less convenient for file-only sharing and GitHub Pages-style artifacts.

Keep `kenjaku serve` as a lightweight static server/index. Add FastAPI/HTMX only if live training
observation or large-artifact search cannot stay static.

## Consequences

- New visualization commands should emit deterministic, self-contained HTML and read only explicit
  local artifact paths.
- JSON/JSONL remains the stable contract between training/testing commands and viewers.
- Shared frontend code should be extracted from existing generators before adding a new frontend
  stack.
- Large viewers should use pagination or embedded data windows rather than rendering every row at
  once.
- Browser network access is not required for generated dashboards.

## Follow-Up Issues

- [#49 Training run dashboard](https://github.com/gongahkia/kenjaku/issues/49): Option A training
  metrics page from JSONL.
- [#50 Self-play replay viewer](https://github.com/gongahkia/kenjaku/issues/50): Option A gameplay
  trajectory viewer.
- [#51 Benchmark dashboard comparison](https://github.com/gongahkia/kenjaku/issues/51): Option A
  benchmark comparison page.
- [#52 Interpretability overlay pagination](https://github.com/gongahkia/kenjaku/issues/52):
  large static artifact pagination.
- [#53 Artifact serve dashboard](https://github.com/gongahkia/kenjaku/issues/53): local static
  artifact index/server.

Future follow-ups should cover shared static viewer helpers, tile renderer extraction from
`browser_game.py`, optional TensorBoard scalar export, and a React/Vite revisit only after repeated
viewer code duplication is measured.

## References

- [PyTorch TensorBoard docs](https://docs.pytorch.org/docs/stable/tensorboard.html)
- [Vite production build docs](https://vite.dev/guide/build)
- [Vite static deploy docs](https://vite.dev/guide/static-deploy)
- [FastAPI static files docs](https://fastapi.tiangolo.com/tutorial/static-files/)
- [HTMX documentation](https://htmx.org/docs/)
