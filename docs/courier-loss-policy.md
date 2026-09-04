# Courier loss policy assessment

`src/medieval/courier-loss-policy.ts` is a pure, compiled-TypeScript v1 assessment contract. It remains policy-only. `src/medieval/courier-continuity.ts` now consumes its death obligations within the separately documented permanent-loss reducer; this policy itself does not record a death, choose a courier, transfer a task or possession, change a relationship, create a person, append causal history, finalize a chronicle, or mutate `FoundationWorld`.

## Ownership and canonical inputs

The request contains the existing `PersistentPersonValidationContext` and persistent-person snapshot. `validatePersistentPeople()` remains the authority for life records, adult status, static crew source links, work availability, and bounded person state. The action time uses the existing canonical minute shape and must equal the persistent-person context's `worldTime`; browser clocks and date-shaped values reject.

The request's loss confirmation is a bounded adult-only event classification with ordinal evidence IDs. Its courier must be a statically eligible Jomon crew member whose validated persistent person is `dead` at that exact canonical action-world minute. This boundary has no death reducer, so it neither supplies nor changes that snapshot.

The only finalization reason it can emit is the existing `crew-extinction` `ChronicleReason`. Its `read-only-chronicle-finalization-intent` points to the existing read-only chronicle path; a future world/storage owner must decide when and how to call `finalizeWorldAsChronicle()`.

## Permanent loss and succession

Confirmed courier loss is always `permanent-loss-required`. Every assessment carries these future-owner obligations:

- retain the confirmed death record;
- do not create an automatic replacement or resurrection;
- do not automatically reassign tasks, erase relationships, transfer possessions, or rewrite history.

Successor evaluation begins with the existing immutable crew's `eligible` flag and then reads the matching persistent person's life and work availability. It returns two ordinal-ID ordered sets:

- `immediateSuccessorCandidates`: eligible living crew currently `available`;
- `eligibleLivingCrew`: the full eligible living set, including those currently `committed` or `unavailable` as `temporarily-unavailable`.

An unavailable survivor prevents crew extinction even when no immediate successor exists. Only an empty `eligibleLivingCrew` set produces the canonical `crew-extinction` finalization intent. This contract does not select or switch a successor.

## Safeguard reservations

Prevention and revival are distinct and both return `unavailable` in every v1 assessment. A future reservation can only change the explanation from `no-current-safeguard-authority` to `future-ultra-rare-reservation-only`; it cannot change permanent loss or grant behavior.

Such a reservation must cite the existing `mystical-effect-policy.ts` v1 audit output, exactly one of its named prevention/revival purposes, an ultra-rare `future-courier-loss-safeguard` reservation, the bounded availability/use/cost/audit information already carried by that audit record, and a revision-matched known source fact from the existing effect-model vocabulary. This consumes the prior policy's deferral boundary without adding a generic spell, mage, casting rule, universal safety net, or loss outcome. Any future implementation still needs its own source, cost, action, replay, safety, and persistence decision.

## Validation and safety

The contract is closed and fail-closed. It rejects malformed, duplicate, stale, noncanonical, ungrounded, unauthorized, unavailable, or contradictory loss/safeguard input with typed diagnostics. It delegates full person validation to the persistent-person owner and known-fact/action-time validation to `effects.ts`.

Loss metadata remains subject to the project-wide metadata-first content-safety policy. It requires adult-only scope and affirmatively excludes sexual violence, slavery, torture, and harm/endangerment of children. It creates no child participant, hidden person, hidden frontier fact, player-facing copy, coercive control, or private world knowledge.

## Compatibility and deferred work

This policy module itself changes no `FoundationWorld`, manifest, RNG, IndexedDB v4 layout, generated content, UI, renderer, browser path, combat, or prototype behavior. The continuity implementation has its own explicitly versioned mutable/replay contract (`MedievalWorldState` v14 / courier v3, replay projection v6) while retaining `FoundationWorld` v14 and layout v4; see [`courier-continuity-contract.md`](courier-continuity-contract.md).

Courier death mutation, explicit prevention/revival actions, voluntary succession, household consequence reducers, and actual chronicle finalization remain future owners. Jomon integrity, disaster, rescue, collapse, and terminal vessel loss remain a separate later policy task.
