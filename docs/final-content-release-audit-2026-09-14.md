# Final content and release audit — 2026-09-14

This audit covers the repository-root Python 3.11+ Jomon game: its authored
catalogues, terminal presentation, vessel and regional stories, and tavern
games. The commands in this report ran with Python 3.14.7. Historical milestone
reports describe earlier trees and are not evidence for this revision.

## Content and replay breadth

The current content audit passed with eight regions; 20 regional questlines;
five cross-region arcs; 24 elite situations, mixed situations, and mutable
micro-sites; eight sanctums and eight bosses; 88 standard, mechanically
distinct enemies; eight named rivals; 102 physical containers; 38 persistent
nonhostile characters; 12 voyage families, 12 stateful voyage variants, and
12 voyage echoes; 72 weapons, 36 armour pieces, 48 passives, 32 techniques,
16 relics, 11 vessel refits, and 24 executable build scenarios spanning six
pressure families. Every declared content minimum passed. The audit also
checks that every weapon and armour row has a physical production path, and
that every legendary object has a physical cache.

[Inference] This is a credible ten-hour replayable-content floor rather than a
short linear outing. A broad run has eight distinct regional geographies,
twenty lines, sanctum work, voyages, household and tavern activity, with route,
opposition, equipment, and aftermath choices that change between seeds and
replays. The shortest landing–objective–landing fixtures alone range from 110
to 182 ordinary in-world actions per region; optional content and decision
time sit outside that floor.

No automated check can honestly turn actions or catalogue counts into a human
stopwatch promise. A timed human all-region campaign and a materially different
replay remain the only way to certify an exact advertised duration. Record
active play separately from idle time, together with completed lines, side
work, deaths, returns, and repeated routes.

## In-world language

The terminal pass replaced presentation labels such as `SPATIAL INVENTORY`,
`PAPER DOLL`, generic status panes, and implementation-style route wording with
Jomon's pack, chart, chronicle, watch, and courier records. These shared HUD
labels now live in `jomon/data/world_text.json` under `interface_labels`, where
they are validated at load time rather than being scattered through rendering
code. Tavern reward annotations now use the fictional table's language rather
than describing deck-design concepts.

`tests.test_catalog` rejects developer, feature, gameplay, implementation,
mechanics, player, tutorial, UI, and UX vocabulary in authored main-world copy;
it deliberately excludes `visuals.json` semantic glyph-role identifiers, which
are rendering data rather than displayed prose. The tavern-game test applies
the same check to generated reward annotations. Internal Python identifiers,
save validation errors, and JSON field names are not player-facing text.

## Content ownership and maintainability

Authored material is grouped by editable domain under `jomon/data/`: people,
recruitment, character profiles, goods, actors, geography, history, terrain
variation, practices, situations, production, circuits, chemistry, reports,
quests, spells, skills, vessel activity, aftermath, equipment, visuals, and
vehicles. The later additions retain their own `history.json`, `practices.json`,
`character_profiles.json`, `arc_relics.json`, and `recruitment.json` catalogues.
`jomon/data/README.md` records the ownership boundary and fields whose rule
reducers intentionally remain in Python. Loader reconstruction tests compare
each catalogue with its runtime adapters, so externalising content does not
silently change saves or behavior.

The Dullest Dungeon tavern contest is separately packaged in
`jomon/dumbest_dungeon/data/`; its office presentation assets and game catalogue
are already isolated from Jomon's region data. Its rule schema remains code and
data infrastructure, while table-facing card text stays in its in-world office
fiction.

## Verification

- JSON decoding, duplicate-key and schema checks, `git diff --check`, `ruff
  check jomon tests`, and `python3 -m compileall -q jomon tests` passed.
- The current content audit passed with zero failures. Focused catalog,
  terminal, character-creation, preparation, and tavern-game regressions are
  rerun after the language changes.
- The final 1,000-world systemic audit, complete verification suite, and full
  unit-test discovery are recorded below once the current runs finish.

## Release boundary

The automated evidence supports the tested technical paths and the authored
content breadth. Native macOS/WSL terminal behavior and a complete manual
campaign have not been exercised in this pass. Do not convert the inferred
content-duration result into a precise marketing claim until a timed playtest
is recorded.
