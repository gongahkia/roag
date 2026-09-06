# Deeper Hearthford milestone

## Implementation plan

This owner-play milestone keeps the existing direct Python architecture and
extends the one Hearthford expedition rather than introducing another world
layer.

1. Replace the single regional map with 8–12 persistent compact rooms. Three
   bounded functions create Hearthford's hub, the branching Reedwood, and the
   linear mill route with side rooms. Validate reciprocal exits, objective
   reachability, and the physical route home.
2. Consolidate zero-time courier, weapon, gear, and support selection in the
   tavern `C` popup. Add a small authored equipment catalogue, role techniques,
   finite discoveries, and explicit qualitative combination rules.
3. Place five direct threat profiles, one mixed encounter, environmental
   controls, and a seed-rare machinery encounter in those rooms. Keep movement,
   attack, guard, gear, negotiation, and interaction as the action vocabulary.
4. Persist changed rooms, returned equipment, discoveries, contact memories,
   and a deterministic three-item visiting merchant through the existing
   single JSON save.
5. Add semantic colour roles with glyph fallbacks, suppress routine movement
   messages, and retain explicit intent, pressure, discovery, and consequence
   messages.
6. Add focused standard-library tests, run the full verification commands, and
   exercise the requested paths in a real PTY. Record only verification that
   was actually performed below before the final assessment commit.

The milestone does not add a procedural-generation toolkit, encounter or item
DSL, generalized menus, another settlement, onboard attacks, save migrations,
or broader simulation.

## Assessment

Pending implementation and verification.
