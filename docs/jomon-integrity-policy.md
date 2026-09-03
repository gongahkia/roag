# Jomon integrity policy assessment

`src/medieval/jomon-integrity-policy.ts` is a pure, compiled-TypeScript v1 assessment contract for future Jomon integrity owners. It reads the existing `WorldJomonState`, canonical action-world minute, known-fact evidence, and an incident declaration. It does not mutate integrity, spend resources, assign labour, advance time, add causal history, choose a courier, call `finalizeWorldAsChronicle()`, or write to a repository.

## Authority and bands

`WorldJomonState` remains the sole source of the vessel ID, operational status, location, capacity, and bounded integer integrity. The policy validates only the exact existing read-only state shape needed for assessment; it does not make a second Jomon model. Its input action time uses the existing `minute` shape accepted by `effects.ts`; wall-clock and browser-shaped input reject.

Bands use integer-only comparisons, in this order:

| Band | Canonical condition |
| --- | --- |
| `sound` | Current integrity equals maximum. |
| `weathered` | Below maximum and at least three quarters intact. |
| `damaged` | Below three quarters and at least one half intact. |
| `critical` | Above zero and below one half intact. |
| `collapsed` | Current integrity is zero. |

An `ordinary-condition` may only be sound or weathered and carries no incident evidence. A `partial-disaster` may only be damaged or critical. It has no terminal result. A `terminal-collapse` may only be collapsed and requires explicit collapse evidence before it can produce terminal Jomon loss.

## Evidence and recovery obligations

Partial-disaster and terminal-collapse evidence is bounded to six ordinal-ID ordered entries. Its first grounded cause is explicit; every later entry cites an earlier evidence ID, forming a finite acyclic causal chain. Every reference must name a revision-matched known fact. A partial disaster additionally requires at least one material, labour, action-time, or risk cost evidence and a recovery-trade-off evidence reference.

For a partial disaster, the output identifies these future-owner obligations:

- grounded cause;
- material, labour, action-time, or risk cost;
- recovery trade-off; and
- inspectable causal chain.

Damaged Jomon emits only a future repair requirement. Critical Jomon emits that repair requirement and a separate future non-mystical rescue requirement. Neither is a task, resource transfer, repair result, or action. A later owner must make the material state, labour, time, risk, causal-history, replay, and persistence decisions explicitly.

## Collapse, terminal loss, and safeguards

Only a validated `terminal-collapse` at the `collapsed` band yields `confirmed-jomon-loss`. Its sole finalization output is a `read-only-chronicle-finalization-intent` with the existing `jomon-loss` `ChronicleReason` and existing read-only chronicle path. An intent is an explanation for a future world/storage owner—not a `WorldChronicle`, save, command, or invocation of repository finalization. Partial damage can never emit it.

An optional safeguard reference must be a valid existing `mystical-effect-policy.ts` v1 audit record with the ultra-rare `future-jomon-loss-safeguard` reservation, a revision-matched source fact, finite conditions/costs, and explicitly named causal-chain, adverse-counterplay, and recovery-trade-off evidence. The output retains those IDs but is always `unavailable` with `future-ultra-rare-reservation-only`. It cannot prevent collapse, repair or revive Jomon, override terminal loss, introduce generic casting, or create a universal safety net.

## Validation, safety, and compatibility

Malformed, stale, duplicate, noncanonical, ungrounded, contradictory, impossible, unsafe, or unauthorized data fails closed with typed diagnostics. Incident metadata is validated through the existing content-safety policy with `not-applicable` participant scope, so this policy creates no child participant and preserves affirmative exclusion of sexual violence, slavery, torture, and harm/endangerment of children. Outputs contain only supplied IDs and assessment facts; they cannot expose hidden people, frontier facts, or world knowledge.

This changes no `FoundationWorld`, `WorldChronicle`, manifest, replay, causal-history, world-state schema, save envelope, IndexedDB v4 layout, valid-v3 loading behavior, migration, renderer, UI, gameplay action, RNG, worker, cache, packed data, generated disaster, or prototype contract. Courier loss remains a separate policy/assessment boundary. Actual disaster, repair, rescue, collapse mutation, terminal finalization, and chronicle export remain deferred owner work.
