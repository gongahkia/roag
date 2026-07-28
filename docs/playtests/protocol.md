# Spatial playtest protocol

Use this for Mine, Sea Caves (`caverns`), then every biome. A pending template is not a completed playtest record.

## Session

1. Use a fresh local profile and record the seed. Do not reveal the biome name or target thesis.
2. Run a 15-minute session or until the participant reaches an objective, makes a route choice, uses terrain, and reads one encounter. Record an early stop and why.
3. Ask the participant, without prompts, for route chosen, landmark recall, threat comprehension, terrain use, boon relevance, encounter read, confusion, memorable moment, fun rating/reason, and a description of the place's spatial thesis without its name.
4. Write only an anonymous participant ID. Keep the record local; do not upload telemetry or free-text notes.
5. Transcribe the answers into `records.json`. Automated/autoplay observations may diagnose a build but cannot be entered as a human session.

## Pilot and routing rule

- Run Mine and Sea Caves pilots first. Apply only protocol fixes before expanding to the remaining biomes.
- Complete each biome with at least one human session. `npm run playtest:check:complete` is the closure gate.
- A game-blocking failure once, or the same `opacity`/`unfairness` code from two anonymous participants, requires a GitHub fix issue. Give equivalent findings the same concise `failureModes[].code`, then put its number on the matching `failureModes[].issue`; do not close the biome epic with an unlinked recurrent finding.
- `npm run balance:report` includes the local per-biome playtest summary beside generated balance telemetry. It does not treat pending human records as a generated-map failure.

## Research discipline

Each record has a source-grounded note and a separate fantasy-invention note. The source note is an inspiration constraint, not a claim that the game depicts a real Jomon site. Verify a new claim against a primary or institutional source before adding it. The initial anchors use [UNESCO's Jomon property record](https://whc.unesco.org/en/list/1632) and the [official Jomon site](https://jomon-japan.jp/en/learn/jomon-culture).

## Record schema

`records.json` is the canonical local record. A session has `sessionKind: "human"`, all response fields, a 1–5 fun rating, a blind thesis answer plus facilitator assessment (`clear`, `partial`, or `missed`), and structured failure modes (`kind`, stable `code`, `detail`, optional GitHub `issue`). Validate structure with `npm run playtest:check`; require completed human records with `npm run playtest:check:complete`.
