# Initial household contract

`src/medieval/initial-household.ts` owns initial-household contract v1. It is the sole owner of the immutable six-member Jomon roster generated at zero-time: stable roster IDs and order, names, roles, role-owned equipment, histories, directed relationships, current static eligibility, the selection-ready active-crew projection, structural validation, exact recreation validation, and identity/history content-safety coverage.

## Reproduction and ownership

The roster is generated only from the normalized world seed, resolved configuration fingerprint, and existing `FOUNDATION_GENERATOR_VERSION` compatibility boundary. It retains the pre-contract `crew:<index>` stream and output exactly. The same valid manifest therefore recreates the same household; changing seed or resolved configuration is the only way to obtain a different initial household. There are no individual rerolls.

`FoundationCrewMember[]` remains the one immutable roster representation. `InitialHouseholdActiveCrewMember[]` is a discardable, renderer-independent projection of currently eligible initial members in canonical roster order. It contains only stable member ID, name, role, and bounded conversation value. It contains no persistent-person state, selected courier, world/map/frontier state, browser data, hidden knowledge, initial-world person seed, or frontier commitment, and it is never persisted as a second roster.

At zero-time all six canonical members are statically eligible. This is creation eligibility only: mutable life and availability remain persistent-person/world-state concerns. The mutable authoritative selected courier remains `state.courier.initialCourierId`; this contract neither sets it nor creates a selection action. Projecting the household neither advances time nor appends causal history, schedules simulation, changes navigation, mutates a record, or writes storage.

## Validation

Structural validation requires exactly `crew:0` through `crew:5` in order, distinct valid names, six distinct roles from the existing closed vocabulary, conversations 1–5, exact role-owned equipment and history values, current `eligible: true`, and every canonical directional relationship record to every other member. A directional link has a derived identity of `<member>:relationship:<target>` and requires its reciprocal directional link to exist. Its standing and basis are intentionally not required to equal those of its reciprocal link.

Identity and history records are audited through the existing content-safety policy. Missing, unsafe, malformed, unclassified, noncanonical, or non-reproducible data fails closed with stable diagnostics.

Exact seed/configuration regeneration is performed once at the complete immutable `FoundationWorld` boundary, which is also the manifest-recreation and IndexedDB load/save boundary. There, the supplied roster must equal the complete regenerated foundation evidence. Content-safety validation uses that same complete expected foundation state for its immutable comparison rather than issuing a second household regeneration. Mutable world-state and persistent-person validators receive the immutable roster as context, validate its strict supplied structure, and preserve their source-link checks without regenerating it. Causal replay likewise rejects malformed household inputs structurally; when it runs through normal `FoundationWorld` validation, replay is therefore bound to immutable evidence already exact-validated at the outer boundary. Persistent people continue to expand only from the validated immutable roster. Initial-world person seeds and frontier commitments remain non-instantiated and cannot become latent mutable household people through this contract.

## Compatibility

This is a validated ownership extraction, not a generated-content or persistence change. `FoundationWorld` stays v14, `MedievalWorldState` stays v12, `WorldDeckNavigationState` stays v1, the manifest/replay contracts stay unchanged, and IndexedDB layout stays v4. No migration, save-envelope field, generator version, RNG stream, selected-courier redesign, UI action, worker, cache, or browser-visible behavior is introduced.
