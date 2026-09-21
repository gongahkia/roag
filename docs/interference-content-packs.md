# Interference content packs

`content_packs/default/interference_text.json` supplies the player-facing prose for the eight stable cross-region interference event IDs. The engine-owned `interference` section in `content_packs/contract.json` lists every required key and its exact permitted placeholders.

A writing-focused author may change an event's title, cause, origin and destination consequence wording, its arrival notice, and its record and ledger templates. For example, `interference.notice` may use exactly `{title}` and `{origin}`.

Authors must not add, remove, or rename event keys or placeholders. They must not edit catalog event IDs, kinds, origin/destination IDs, cargo, triggers, ordering, probability, stock effects, route-risk effects, confidence changes, or arrival timing. The engine chooses and applies the interference; the pack provides its rendered wording.
