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

- [x] Tiered palette is bounded by reported colour and pair counts.
- [x] Eight regional families have visibly distinct, deterministic ground roles.
- [x] Fire, smoke, water, ice, mud, and collapse are distinguishable.
- [x] Actor identity always outranks terrain/material colour.
- [x] Items and information classes no longer borrow unrelated actor roles.
- [x] No gameplay fact is communicated by colour alone.
- [ ] Tests and render benchmarks pass without material regression.
- [x] 80x24, 100x32, resize recovery, monochrome fallback, and terminal cleanup
      receive explicit verification.

## Implemented result

The renderer now has 57 semantic roles.  Its 256-colour tier uses 46 distinct
foregrounds, the 16-colour tier uses 14, the original 8-colour tier uses seven,
and monochrome uses none.  Allocation is deterministic, never uses pair zero as
a mutable pair, never exceeds `COLOR_PAIRS - 1`, and collapses later shades to
an already allocated semantic family when pair capacity is scarce.

Every generated regional glyph from all eight families was audited through the
production terrain classifier.  Regional ground, vegetation, roads, earth,
stone, timber, structures, shallow water, deep water, ice, mud, fire, smoke,
collapse, salt, lime, ash, resin and oil are independent roles.  Player and
actor identity is applied after physical-cell classification, so a hostile in
water remains visibly hostile.  The classifier fast-paths the overwhelmingly
common regional glyphs and uses only constant dictionaries; profiling measured
about 0.22 ms per full 80x24 render for the classification function itself.

Weapon, armour, tool, technique/passive, consumable, commodity and relic
families no longer borrow hostile, structure, neutral or cargo roles.  Coloured
frames and headings establish panel hierarchy.  Status, events, targeting,
route details and labels, inventory feedback, dialogue, inspection prose, and
the startup screen use dedicated accents.  Text classifiers recognize explicit
`FACT`, `RUMOUR`, `FORECAST`, `REMEMBERED`, `BLOCKED`, `LEGAL`, and comparable
labels; colour supplements rather than replaces those words.

## Terminal verification

Real PTY sessions were run with terminfo profiles reporting 256, 16, 8 and zero
colours.  Captured control output confirmed xterm 256-colour indices, bright
ANSI 16-colour indices, basic ANSI colours, and an absence of colour sequences
under `vt100`, respectively.  In the 256-colour session a new seeded world was
created and Hearthford entered directly with `E`; meadow, mud, deep water,
road, trees, structures, exits, courier, status groups and commands were all
visibly distinct.  Quit confirmation and terminal restoration completed with
exit status zero.

A separate pseudo-terminal session started at 100x32, created a world, resized
to 70x20, observed the explicit `needs at least 80x24` warning, resized back to
100x32, observed the restored `STATUS` interface, and quit normally.  The
alternate-screen leave sequence was captured.  The ordinary 256-colour play
session ran at 80x24.

## Research boundary

The implementation consulted the official Python `curses` documentation and
the ncurses `curs_color(3X)` manual.  The relevant constraints were the
terminal-reported `COLORS` and `COLOR_PAIRS`, pair zero's special status, valid
pair numbering, and default-colour use.  No external game palette, colour
names, code, assets, or visual identity was copied.

- https://docs.python.org/3/library/curses.html
- https://invisible-island.net/ncurses/man/curs_color.3x.html

## Performance note

The initial benchmark and later full runs occurred under materially different
machine load, including a concurrent compiler consuming most of one CPU and a
reported thermal-pressure level of 100.  Raw files are retained rather than
silently normalized.  A same-machine interleaved 100-render comparison measured
the old renderer at 8.676 ms median and the completed renderer at 8.485 ms
median.  A fresh full benchmark after the competing compiler finishes remains
the final open gate; its machine-readable result will replace the provisional
`docs/performance-colour-final.json`.
