# LegalActionOracleV1

`legal_action_oracle_v1(state, seat=...)` projects `legal_sandbox_actions` into a deterministic `LegalActionOracleV1` for either `tenhou-4p` or `tenhou-3p`.

The result carries the selected seat, complete ordered `ActionV1` actions, and a matching `LegalActionMaskV1`. The ordered actions preserve `tsumogiri` and consumed physical tiles. The fixed 276-slot mask is an action-kind/tile projection, so it intentionally does not distinguish physical consumed-tile variants.

The oracle requires an actionable non-terminal sandbox state. For a pending reaction window, pass the reacting seat; otherwise the current seat is used. Serialization is strict and validates that every action and mask coordinate matches the reported ruleset.
