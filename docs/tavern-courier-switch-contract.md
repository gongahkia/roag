# Tavern courier-switch contract

`src/medieval/tavern-courier-switch.ts` owns contract v1 for the one currently implemented operated Jomon prop: voluntary perspective switching at `prop:task-ledger`.

## Authority and eligibility

The immutable household remains the only roster authority. `initialHouseholdActiveCrew()` supplies its canonical static eligible order and its bounded `{ id, name, role, conversation }` selection data. The tavern contract intersects that view with validated persistent-person life and availability, excludes the current active courier and any canonical permanent departure, and exposes no initial-world person seed, frontier commitment, history, equipment, persistent-person detail, inferred location, hidden fact, browser state, or copied/persisted roster.

The physical source is the existing deck-plan binding `deck-prop-binding:prop:task-ledger`: prop `prop:task-ledger`, ledger kind, tavern area, coordinate `4,4`. A transition is available only when navigation names the current active courier and that courier is exactly at that anchor. Away from it, or when no alternate eligible living available household member exists, the contract returns a bounded unavailable result. Malformed household, person, navigation, prop binding, provenance, replay, or world evidence fails closed.

## Zero-time transition

Initial selection and current perspective are separate mutable concepts:

- `state.courier.initialCourierId` is fixed by zero-time creation selection and remains creation provenance.
- `state.courier.activeCourierId` is the current inhabited courier used by navigation, movement, fidelity, autonomy, conversation, delegation, terminal status, and the tavern transition.

`switchTavernCourier()` validates the complete current world, runs the shared pure reducer, appends exactly one typed `tavern-courier-switched` causal command, and returns a new full envelope. It retains initial selection, sets active courier and navigation to the target at the ledger anchor, and reconciles current-courier fidelity/autonomy projections at the same world minute. It does not advance world time or action sequence, draw randomness, alter immutable household/manifest/initial-world evidence, mutate unrelated people, make cargo or prop state, schedule simulation, or write storage itself.

Repeated selection of the active courier, unknown/noneligible/dead/unavailable targets, operations away from the anchor, missing initial selection, active/navigation mismatch, forged binding, malformed causal evidence, direct mutation, or replay mismatch are rejected without changing the submitted input. This task introduces no loss, succession, recruitment, rest, conversation, cargo, trade, travel, or other prop operation.

## Presentation and persistence

Terminal presentation v7 derives one keyboard-first ledger prompt from the validated world: source, current courier, canonical candidate list, availability reason, semantic palette state, paired non-colour cue, accessibility text, and source/time/freshness evidence. The remappable contextual control opens it; Arrow keys select a bounded candidate, Enter confirms, and Escape cancels with no mutation or time change. The canvas only renders and forwards typed intent. The detailed-renderer adapter receives the same projection but has no input, world, timer, persistence, or authority access.

`MedievalWorldState` v14 / courier v3 and causal replay projection v6 explicitly encode active courier and permanent departure exclusion. `FoundationWorld` remains v14, `WorldDeckNavigationState` remains v1, the immutable manifest and generator/RNG streams are unchanged, and IndexedDB remains layout v4 with no store or migration. A valid v14/v13 or v14/v12 full envelope is upgraded read-only: selected initial courier becomes active courier, departures begin empty, validated navigation is retained, and its checkpoint is deterministically rebased before full replay validation. Reads never overwrite the old record; an explicit later save may write the current full envelope only after validating its old source/index. Corrupt or ambiguous input remains stored and unavailable.
