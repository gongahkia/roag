# Colour-variety milestone

## Scope and guardrails

This pass expands Jomon's colour vocabulary without making colour authoritative.
Glyphs, labels, bold, underline, reverse video, and visible intent remain the
complete gameplay cues at 8-colour and monochrome terminals.  The palette must
remain readable on the default terminal background and must not add runtime
dependencies, animation, background work, or action-clock advancement.

Starting commit: `3969986bd911415631d48f6ef1ecf92d8d84cd26` on `main`, initially
clean and three commits ahead of `origin/main`.  Python is 3.14.7 on the
development machine.  No push is authorized.

## Audit

The existing renderer has fifteen semantic roles but only seven foreground
colours.  Large visual categories collapse together: terrain and structures are
both white, every water state is blue, every physical hazard is red, and weapon,
armour, cargo, passive, and consumable colouring borrows world-actor roles.
Most status, event, route-detail, inspection, and dialogue prose has no semantic
colour at all.  Consequently all eight regional families have distinct glyph
topology but much less distinct visual identity than their terrain warrants.

Python's curses interface exposes terminal-reported `COLORS` and `COLOR_PAIRS`,
and restricts pair numbers to the reported pair capacity.  Jomon will therefore
select a deterministic 256-, 16-, 8-, or monochrome plan from reported
capabilities and initialize only actually allocated pairs.  It will retain the
default background via `use_default_colors()`.

## Baseline

The committed machine-readable baseline is
`docs/performance-colour-baseline.json`, produced with:

```text
python3 -m jomon.benchmark --samples 20 --seed colour-variety
```

Key development-machine results are 6.265 ms median / 10.413 ms p99 for the
80x24 render sink and 31.851 ms median / 36.459 ms p99 for boarding input plus
render.  The final pass must remain within the existing ordinary-render budget
and within 20% of this baseline absent a visible justification.

## Implementation decisions

1. Add palette tiers and independent semantic roles for regional ground,
   vegetation, roads, stone, timber, shallow/deep water, ice, mud, fire, smoke,
   collapse, item families, frames, headings, facts, rumours, forecasts,
   warnings, successes, and unavailable choices.
2. Derive map colour from underlying terrain, active region, and sparse material
   state only after player/actor identity is resolved.  No extra simulation or
   per-cell allocation is permitted.
3. Colour status, event, route, inventory, inspection, and dialogue surfaces
   through small pure classifiers so presentation remains testable.
4. Preserve the current basic eight-colour mapping and bold fallbacks, then use
   brighter ANSI colours at 16 colours and a wider xterm-compatible palette only
   when the terminal reports at least 256 colours.
5. Verify palette completeness and pair exhaustion, every regional family,
   material overlays, semantic prose, 80x24 and 100x32 layouts, monochrome and
   8/16/256-colour plans, renderer performance, compile, and real PTY startup and
   restoration.

## Acceptance gates

- [ ] Tiered palette is bounded by reported colour and pair counts.
- [ ] Eight regional families have visibly distinct, deterministic ground roles.
- [ ] Fire, smoke, water, ice, mud, and collapse are distinguishable.
- [ ] Actor identity always outranks terrain/material colour.
- [ ] Items and information classes no longer borrow unrelated actor roles.
- [ ] No gameplay fact is communicated by colour alone.
- [ ] Tests and render benchmarks pass without material regression.
- [ ] 80x24, 100x32, resize recovery, monochrome fallback, and terminal cleanup
      receive explicit verification.

