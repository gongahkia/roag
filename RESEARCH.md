# Research and implementation notes

Primary sources consulted for this pass (2026-09-19). No terrain/physics library,
third-party code snippets or game assets were imported. The design-specific rules,
quotas, generation masks, reactions, ecology and tests were written in this repo.

- Robert Nystrom, **Update Method**, author-maintained book source:
  https://raw.githubusercontent.com/munificent/game-programming-patterns/master/book/update-method.markdown
  Relevant to explicit sequential update order, storing state between updates and
  avoiding accidental same-tick execution/removal skips when entities spawn/die.
  Here newborn list entries are excluded from their spawning loop and cleanup is deferred.
- LÖVE community-maintained API definitions:
  https://raw.githubusercontent.com/love2d-community/love-api/master/love_api.lua
  Used for the existing keyboard/input/graphics adapter contracts. Real-runtime
  delivery of callbacks still needs workstation validation; mocks are not proof.
- Lua 5.1 reference manual:
  https://www.lua.org/manual/5.1/manual.html
  Language compatibility and table/iteration rules. Runtime code avoids newer-only
  syntax; executed native tests used the available Lua 5.4 library, not LuaJIT.
- RFC 8259, **The JavaScript Object Notation (JSON) Data Interchange Format**:
  https://www.rfc-editor.org/rfc/rfc8259
  Syntax reference for the existing local JSON map codec, not a security certification.
  Map-specific rules impose tighter name, size, palette and content bounds.

Workforce apportionment is explicitly defined in docs/WORKFORCE.md and unit-tested;
headcount allocation is not advertised as globally optimal scheduling. Procedural
layout names describe local methods, not claims of algorithmic novelty. The game
borrows high-level genre goals, not a claim to reproduce another game's engine.
