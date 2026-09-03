# Semantic palette contract

The current medieval browser uses the original, application-owned semantic palette in `src/medieval/palette.ts`. Palette contract v2 is a single default full-colour, dark autumnal scheme: soot/olive ground and panels, parchment text, moss readiness/action states, river-toned navigation, amber warnings, and restrained rust risk/error states. It replaces the former fixed sixteen-colour terminal restriction; it is not a historical-terminal palette or a selectable theme.

## Ownership and roles

`JOMON_PALETTE` is the sole source of raw colour values. Browser bootstrap exposes its generated CSS custom properties and uses `consoleGround` for browser theme metadata. The canvas uses `consoleGround`, `panelSurface`, and `panelBorder` for its surfaces; it uses the text/status tokens for normal, muted, title, selection, action, ready, waiting, warning, risk, error, water, route, and neutral semantic roles.

Terminal presentation, ASCII glyph data, and the deferred detailed-renderer adapter hold only `JomonPaletteToken` references. They also retain their existing text, ASCII glyph, ARIA, keyboard, and non-colour cues. A colour therefore never becomes the only signal for selection, readiness, warning, risk, or an action. This document grants no detailed-renderer implementation and no palette authority to a renderer.

## Accessibility checks

The closed `JOMON_PALETTE_CONTRAST_REQUIREMENTS` matrix is tested with a deterministic WCAG relative-luminance calculation. Every current small-text role is required to meet at least 4.5:1 against `panelSurface`, the sole canvas text surface. Borders are checked against both adjacent dark surfaces, and selection, water, route, ready, waiting, and risk graphical cues against both dark surfaces, at a minimum of 3:1. An authored palette edit must update this versioned contract and leave `validateJomonPaletteContrast()` empty.

The colours are original values selected for this application. Dark autumnal terminal palettes, including Monokai, Gruvbox, and Everforest, were only high-level tonal references; this code does not copy their named palettes, values, branding, layouts, source, or assets.

## Compatibility boundary

Palette v2 affects only browser presentation and metadata. Tokens remain renderer-neutral, while raw values remain local to the palette module. It does not change world generation, content safety, glyph assignment, controls, manifests, replay, FoundationWorld, persistence envelopes, IndexedDB layout v4, or saved-world compatibility. No migration, preference record, public theme/mod surface, or user palette input exists.
