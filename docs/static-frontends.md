# Static Frontend Helpers

`kenjaku.frontend_static` is the shared helper layer for generated HTML viewers.

Use it for new static viewers instead of hand-writing repeated document shells, sort scripts, badges, or simple SVG charts.

## Helpers

- `html_document(...)`: deterministic HTML shell with local stylesheet/script references and generator metadata.
- `static_base_css(include_theme=False)`: shared reset, wrapper, badge, and sortable button CSS. Set `include_theme=True` to include the arcade-card theme from `kenjaku.frontend_theme`.
- `sortable_header(...)` and `sortable_table_script(...)`: accessible table headers and deterministic client-side sorting.
- `badge(...)`: escaped status badge markup.
- `summary_item(...)`: escaped `dt`/`dd` summary item markup.
- `line_chart_svg(...)`: dependency-free inline SVG line chart.
- CSS utilities: `kj-static-fit`, `kj-static-truncate`, `kj-static-scroll`, and `kj-static-control-row` for dense responsive layouts.

## Rules

- Keep generated pages self-contained, or reference local assets copied beside the HTML.
- Do not add browser `http://`, `https://`, or `@import` dependencies.
- Keep text and controls bounded at mobile widths with helper overflow utilities or local section scroll wrappers.
- Preserve visible `:focus-visible` states for keyboard navigation.
- Cover reduced-motion and high-contrast hooks when adding shared CSS.
- Keep data contracts in generator modules; helpers should only handle static markup, CSS, and client-side presentation glue.
- Add a focused test for each new consumer proving it uses the shared helper metadata or helper output.

Current consumers:

- `kenjaku.browser_demo`
- `kenjaku.training_dashboard`

## Visual QA

Generate deterministic fixture pages and capture desktop/mobile screenshots under ignored `runs/`:

```bash
PWCLI="$HOME/.codex/skills/playwright/scripts/playwright_cli.sh" \
PYTHONPATH=src python3 scripts/visual_qa_static_pages.py --output-dir runs/visual-qa
```

The command writes pages, screenshots, and `visual-qa-manifest.json`. It fails on blank pages,
missing local assets, severe viewport overflow, or screenshots that are not written. It uses only
local fixture data and a caller-provided Playwright CLI; it does not fetch browser assets.

For a no-browser fixture-generation smoke:

```bash
PYTHONPATH=src python3 scripts/visual_qa_static_pages.py \
  --output-dir runs/visual-qa --generate-only
```
