# Travel content packs

`content_packs/<pack>/travel_text.json` supplies player-facing route-leg and
non-deck voyage wording. Its strict `text` object is declared in
`content_packs/contract.json` and accepts only the listed placeholders.

Authors may change journey frames, route availability explanations, departure and
arrival wording, cargo-loss narration, ordinary voyage outcomes, and requirement
messages. They may not change voyage-family IDs, raw equipment or cargo IDs, route
nodes or edges, duration, supply cost, outcome selection, RNG stages, time, or
state changes. The engine chooses a stable travel result before formatting it.

`vessel.json` remains the authority for route and voyage mechanics. Region and
route display values come from their existing presentation layers; item names use
the item resolver. Deck-crisis narration remains in `ship_crises.py` for Milestone
10C, and vehicle presentation is also deferred to that milestone.
