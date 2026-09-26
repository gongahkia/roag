# Workline content packs

`content_packs/default/worklines.json` supplies the player-facing wording for the four engine-owned regional worklines: `hearthford`, `greywash`, `greenwold`, and `whitecairn`. Its `text` object is a strict interface: every key and every permitted template placeholder is declared in `content_packs/contract.json`.

A writing-focused author may rewrite workline titles, branch labels, evidence/reward display text, inspections, requirement explanations, field-work reports, settlement consequences, and ledger-facing wording. For example, `workline.line.field` may change its prose while retaining its declared `{x}`, `{y}`, `{z}`, and `{requirement}` placeholders.

Authors must not add, remove, or rename keys or placeholders. They must not change workline IDs, branch keys, evidence and reward kinds, requirements, thresholds, material consumption, rewards, route or market effects, state mutations, selection order, or timing. The engine chooses the workline state and fills template values; the pack supplies only its words.
