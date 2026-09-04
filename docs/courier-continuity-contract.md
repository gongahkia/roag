# Courier continuity contract

`src/medieval/courier-continuity.ts` owns v1 assessment of an explicit permanent loss of the current active courier. `world.ts` owns the shared pure replay reducer and `storage.ts` owns the one transactional persistence bridge. This boundary has no renderer, browser input, prop, timer, RNG, generated-content, or hidden-person authority.

## Confirmation and permanent outcomes

The only outcomes are bounded adult-safe `death` and `departure` confirmations for exactly the current `activeCourierId` at the canonical current world minute. The confirmation ID is derived from outcome, courier ID, and minute; its ordered loss-evidence IDs and classified adult-only metadata reject malformed, stale, unsafe, child-related, or free-text substitutions.

Death changes only that validated adult persistent-person record to a permanent death at that exact minute, with existing dead-person idle/unavailable restrictions. Departure leaves the person record unchanged—including identity, possessions, relationships, memories, history, work, and no fabricated location—but records their canonical ID in mutable `departedCourierIds`, permanently excluding future courier eligibility. Neither result creates a replacement, revival, task reassignment, possession transfer, relationship/history rewrite, roster, person, or hidden fact.

## Continuity and terminal state

`initialCourierId` remains immutable-in-practice creation provenance. The successor is the first canonical `initialHouseholdActiveCrew()` member who is living and not departed, regardless of current temporary work availability. This deliberately differs from voluntary ledger switching, which still requires a distinct living *available* candidate at the tavern anchor. A continuing successor retains their own work/availability record; only `activeCourierId` and ownership of the already-authoritative current deck coordinate change. The transition is zero-time: it does not change minute, action sequence, immutable foundation evidence, RNG-derived output, or unrelated person state.

When no statically eligible living non-departed household member remains, active courier and navigation ownership clear to their empty v1 form. The reducer appends one `courier-loss-resolved` command and creates the existing read-only `crew-extinction` chronicle outcome; it never invents an invisible successor. Public active actions reject this replayable terminal source.

## Replay, validation, and persistence

Mutable state v14 / courier v3 contains fixed `initialCourierId`, optional current `activeCourierId`, and canonical ordered `departedCourierIds`. Causal replay projection v6 and its typed command retain the confirmation plus canonical continuation or crew-extinction result. Full `FoundationWorld` validation still owns immutable seed/configuration regeneration. State, person, and continuity validation use supplied validated immutable household evidence and strict cross-record checks; they do not add duplicate regeneration.

Valid v14/state-v13 and v14/state-v12 envelopes upgrade read-only: selected initial courier becomes the same active courier, departures start empty, navigation remains validated, and the causal checkpoint is deterministically rebased and replay-proven. Ambiguous, stale, malformed, unsafe, or replay-inconsistent data fails closed and is never rewritten on read.

`MedievalWorldRepository.resolveCourierContinuityLoss()` validates the submitted source then atomically replaces the exact stored active envelope for a continuing result. It rejects a stale/forged source without changing active world, index, or snapshots. For crew extinction it delegates to the existing atomic finalization transaction, replacing that exact active envelope with the read-only chronicle. IndexedDB layout remains v4; no store, migration, cache, or second persisted roster is introduced.

## Presentation limits and compatibility

Terminal presentation v9 distinguishes awaiting initial selection, active play, and a source-backed read-only crew-extinction state. At the physical `prop:task-ledger` anchor only, its compact readout lists the canonical household in order as active (with truthful current availability), available, committed, temporarily unavailable, permanently departed, or dead. A retained validated continuity command may state only that the active perspective continued after a recorded permanent loss; it never exposes a cause, location, evidence ID, possession, relationship, memory, health detail, hidden person, or loss control. The deck and detailed-renderer adapter v3 remain adapters with no continuity, persistence, replay, timer, input-execution, or roster authority. This contract introduces no player-triggered death/departure operation, rest, conversation, cargo, travel, recruitment, succession choice, or other prop action.

`FoundationWorld` remains v14, navigation remains v1, causal-history remains v4, the manifest/generator/RNG stream and immutable household are unchanged, and IndexedDB layout remains v4. Content safety retains the project boundaries against sexual violence, slavery, torture, and harm or endangerment of children.
