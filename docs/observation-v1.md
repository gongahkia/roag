# ObservationV1

`kenjaku-observation-v1` is the strict JSON boundary for a policy consumer. Its version is encoded in `kind`; a future incompatible shape must use a new kind.

The payload contains the actor's `hand` and `drawn_tile`, plus public table state: ruleset, seats, scores, wall counts, discards, melds, Kita tiles, visible dora, riichi state, and pending reaction state. `hand_sizes` exposes opponent counts only.

It never contains opponent hands, wall/dead-wall order, ura-dora indicators, or furiten state that is private to another seat. `ObservationV1.from_sandbox_state(state, seat=...)` enforces this projection. `from_dict` rejects missing and unknown fields; `to_json` uses deterministic key ordering.

Both `tenhou-4p` and `tenhou-3p` are validated against their legal tile sets. Sanma-only Kita tiles are North (`N`).
