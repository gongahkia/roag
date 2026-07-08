# Kenjaku Arcade-Card Theme

`kenjaku.frontend_theme` defines the shared static theme contract for the #80 frontend refresh.

The direction is original Kenjaku UI: dark table surface, high-contrast tiles/cards, score HUD chips, action badges, tactile hover/focus states, and readable dense research data. Do not use Balatro assets, logos, exact layouts, text, or protected trade dress.

## Contract

- CSS: `kenjaku_arcade_theme_css()`
- Metadata: `kenjaku_arcade_theme_contract()`
- Version: `kenjaku-arcade-card-theme-v0`

Token groups cover:

- table/background layers: `--kj-bg-void`, `--kj-bg-grid`, `--kj-table-felt`, `--kj-table-rail`
- surfaces/cards/tiles: `--kj-surface`, `--kj-surface-raised`, `--kj-card`, tile suit colors
- HUD/scoring/actions: `--kj-chip-gold`, `--kj-score-positive`, `--kj-score-negative`, `--kj-action`
- states: `--kj-disabled`, `--kj-error`, `--kj-warning`, `--kj-success`, `--kj-focus-ring`
- layout/accessibility: `--kj-breakpoint-mobile`, `--kj-min-control`, `--kj-readable-muted`
- motion/elevation: `--kj-motion-fast`, `--kj-motion-normal`, `--kj-shadow-hard`, `--kj-shadow-glow`

Primary classes:

- `kj-arcade-shell`
- `kj-table-surface`
- `kj-card`
- `kj-panel`
- `kj-tile`
- `kj-hud`
- `kj-chip`
- `kj-score-chip`
- `kj-action-badge`

State hooks:

- `is-selected`
- `is-disabled` or `aria-disabled="true"`
- `is-error`
- `kj-state--success`
- `kj-state--warning`
- `kj-state--error`
- `kj-score--positive`
- `kj-score--negative`
- `kj-score--neutral`

## Use

Generated static pages should embed the CSS from `kenjaku_arcade_theme_css()` into their existing HTML shell. This is theme-only: pages keep their current data contracts and can opt in incrementally.

The CSS is dependency-free and must stay self-contained. Do not add browser `@import`, remote fonts, image URLs, scripts, or other external network requirements.

## Guardrails

- Support current Safari, Chrome, Firefox, and Edge using static HTML, CSS grid/flexbox, `:focus-visible`, `prefers-reduced-motion`, and `prefers-contrast`.
- Check common widths at 390px mobile and 1280px desktop for upgraded generated pages.
- Keep interactive controls at least 34px tall and visibly labeled.
- Use visible focus rings for links, buttons, form controls, and custom focusable surfaces.
- Use `overflow-wrap:anywhere`, horizontal scroll wrappers, or truncation utilities for dense labels and paths.
- Use `prefers-reduced-motion: reduce` for nonessential animation and `prefers-contrast: more` for stronger borders/focus.
