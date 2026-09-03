Work in /Users/gabrielongtemasek/Desktop/personal/jomon. Do not commit.

First read, in order: LORE.md, TODO.md, README.md, relevant docs under docs/, relevant src/medieval source/tests, then git status --short. Preserve all existing changes. TODO.md is authoritative and must retain exactly one active [-] task.

Implement the exact active TODO task:

“Add a player-visible map legend/help surface without replacing in-world readability.”

Current authoritative context:

- FoundationWorld is v14 and MedievalWorldState is v12. WorldDeckNavigationState v1 is persisted/replayed in the full authoritative envelope. IndexedDB layout remains v4.
- terminal-presentation v5 produces the common renderer-independent 18×8 Jomon deck map: 113 static deck/hull cells plus an optional source-backed @ active-courier marker.
- The map is fixed-full-deck, all static deck cells are known, and no fog, panning, travel, prop actions, cargo, other actors, or detailed renderer implementation exists.
- The current map glyphs are closed-catalogue vocabulary: # hull boundary, = open/navigable deck, / gangplank, ) quay approach, @ active adult courier.
- Existing command help is opened by the remappable command-help control (default ?). Existing keyboard controls, non-colour cues, semantic palette tokens, accessibility text, and the renderer-independent detailed-renderer parity contract must remain authoritative.
- The canvas is an adapter only: it must not own copied deck geometry, glyph meanings, world state, persistence, time, input authority, or hidden knowledge.

Required implementation:

1. Add a compact, player-visible legend/help surface through the existing keyboard-first help flow or an equally bounded, remapping-compatible surface. It must be zero-time and dismissible with existing cancellation behavior.
2. Make the legend derive its glyph meanings and accessible text from validated terminal/glyph/deck-plan presentation contracts rather than duplicating raw symbols or world knowledge in the browser canvas.
3. Cover the current visible glyphs and truthful movement/collision context, while clearly stating current limits: known fixed local deck only; no cargo, NPC, hazard, travel, fog, or prop-action state.
4. Keep the map readable in-world without opening help. The legend supplements readability; it must not become the sole source for consequential map meaning.
5. Preserve semantic palette roles and paired non-colour cues. Include concise accessible text and source/time/freshness evidence where the existing presentation contract requires it.
6. Update the detailed-renderer adapter/parity contract and tests if the legend is consequential renderer-visible information. A future detailed renderer must receive equal legend/help information without gaining world, persistence, timer, or input-execution authority.
7. Add focused deterministic tests for canonical ordering, validation/fail-closed behavior, no hidden/omniscient data, zero-time guarantees, remapping compatibility, accessibility, and browser-visible keyboard operation.
8. Update relevant documentation, likely README.md, docs/jomon-deck-plan.md, terminal/detailed-renderer documentation, and the performance baseline. As a narrow documentation correction, replace the present-tense performance-baseline claim that terminal presentation has “zero materialized map cells” with the actual 113 static cells plus source-backed courier marker. Preserve dated historical records as historical rather than rewriting history.

Strict exclusions:

- No changes to FoundationWorld/MedievalWorldState versions, manifests, causal replay, world generation, navigation rules, saves, migration, IndexedDB layout/stores, RNG, workers, caches, prototype code, gameplay content, fog, panning, travel, prop actions, or detailed-renderer implementation.
- Do not add glyphs, arbitrary colours, mouse-first interaction, a theme selector, or a public content/mod API.
- Do not broadly refactor or raise global test timeouts. Do not change unrelated timeout budgets merely to make verification green.
- Respect all content boundaries in LORE.md, including no sexual violence, slavery, torture, or harm/endangerment of children.

Verification:

- Run focused relevant Vitest tests.
- Run:
  npx vitest run src/medieval/*.test.ts --maxWorkers=1 --no-file-parallelism
- Run:
  npx playwright test e2e/medieval-foundation.spec.ts --project=chromium
  because this is browser-visible.
- Run:
  npm run build
- Run:
  git diff --check
- Run:
  npm run benchmark:medieval-foundation
  only if performance or persistence behavior materially changes; otherwise explicitly report it as skipped.

Important audit baseline: a recent independent serial full-suite run produced 289/293 passing, with timeout-only failures in one performance-fixture test, one social-memory test, and two storage tests; the relevant isolated run passed 92/92. Record the exact result you obtain honestly. Do not describe the full suite as passed if it times out or fails.

Only after genuine verification: update TODO.md to mark this legend/help task [x], document decisions, compatibility, tests, skipped checks, warnings, and limitations, then activate exactly this one existing next task:

“Generate the initial household roster, roles, personal equipment, relationships, histories, eligibility, and active-crew representation wholly from the world seed and resolved configuration; define its reproducibility and validation contract.”

Stop after that. Report changed files, behavioral/schema/compatibility impact, all test results, skipped checks, warnings, unresolved issues, final git status, and the exact sole next active TODO task. No commit.
