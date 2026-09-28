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
