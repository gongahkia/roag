# Jomon engine baseline

Jomon ships a deterministic engine, a compact provisional `first-playable`
content pack, and a non-playable `template` pack.  The default launch selects
`first-playable`; selecting the template explicitly with `JOMON_CONTENT_PACK`
keeps **Join Game** disabled.

Mechanics decide rules, costs, collision, persistence, and deterministic
outcomes. Stable IDs identify entities and actions. A content pack provides
authored presentation and concrete system instances. Debug and ASCII Pygame
renderers draw the same immutable views and submit the same semantic commands.

Format 16 is the reset baseline. Saves from earlier content baselines are
rejected clearly. A compatible pre-operation format-16 pack can load with
empty feature/operation state; an operation-bearing save must contain its
validated operation state.

The first playable pack contains one local physically connected operation. It
is not a complete simulation: networking, crew continuity, schedules, economy,
survival, skill progression, and generated history remain future systems.

CYBER-02A adds optional persistent crew custody for playable packs. The active
operative is a stable roster identity, not a recreated player template. Death
allows explicit selection of another existing living member; ordinary carried
items remain on a dead member until physically recovered. Neural-memory
retention and learned-ability inheritance are intentionally absent.

The default first-playable Debug skin uses selected 16×16 frames from the
vendored Kenney 1-Bit Pack atlas through presentation-only `assets.json`
bindings. ASCII remains a separate BigBlueTerm/glyph skin. Missing or
unresolvable image bindings fall back to Debug primitives, and asset changes do
not affect mechanical fingerprints or saves.

## UX-01 operation shell

Both renderer skins now consume the same immutable contextual-action projection.
It only describes selected known terrain and currently visible people: action
availability, range, requirements, health, hostility, local recovery, and
operation relevance remain reducer-owned. Current terrain is remembered inside
the provisional Manhattan-radius-five visibility area; actors and remains are
never remembered and are not drawn outside that current area.

During a live operation, `Esc` opens Pause and `H` opens Help. Pause offers
Resume, Save, Settings, Help, Return to Title, and Quit; it does not imply an
in-app load screen. Ctrl+S is also available while paused or awaiting a
successor. `--new` opens setup without first creating a world; cancelling it
returns to Title.

The current subjective first-playable presentation bindings remain explicitly
provisional and await player review: `terrain.wall` = atlas `48,96,16,16`,
closed/open gates = `16,80,16,16` / `32,80,16,16`, base = `32,320,16,16`,
maintenance latch = `512,192,16,16`, objective cache = `80,112,16,16`, and
floor has no Debug atlas binding. ASCII uses `.`, `#`, `+`, `/`, `H`, `L`, and
`O` for those semantic identities. Friendly crew now use the neutral NPC icon,
while a hostile uses the threat icon; glyphs remain presentation only.
