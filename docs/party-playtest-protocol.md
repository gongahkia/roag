# Party Rules moderated playtest protocol

This protocol is for a fresh 2–4 person local session or an equivalent private online room. It measures comprehension, pacing, social attribution, and campaign memory; it does not claim that the game is fun without recorded participant evidence.

## Setup

Use a production build and record the commit, platform, player count, seed, ruleset, whether the guide was reset, and whether the session was local or online. For local play, use one screen with the pass-the-device prompt enabled. For online play, use the same seed and record room connection/reconnect issues.

Before participants arrive, run:

```sh
npm run build
npm run simulate:party -- --seed moderated-baseline --players 4 --holes 9 --out output/playtests/moderated-baseline.json
npm run parity:party
```

Use Party Rules, standard slot flow, a nine-hole campaign, and no explanation beyond reading interface text aloud if asked. Let participants skip the guide if they choose; record that choice.

Before the match, open settings and enable **show local Party Rules diagnostics**. This is off by default and does not change gameplay. At the final table, select **download anonymous playtest telemetry**; the browser saves a `party-*.json` file in its configured Downloads directory. Record the filename and seed in the evidence record. The file uses `P1`, `P2`, and so on rather than player display names.

## Moderator prompts

Ask these at the indicated moments without suggesting an answer:

1. After the first slot reveal: “What made this hole turn out this way?”
2. Before each player’s first shot: “What are your available route choices?”
3. After the first card, collision, gadget, or recovery: “What changed that ball’s position?”
4. After the first shop: “What can you take with you, and what cannot you do here?”
5. After the first seam: “What part of the course is still active, and what part is memory?”
6. At the end: “Which player decision do you remember most, and why?”

Do not ask players to optimize. Let them negotiate, hesitate, take safe routes, or attack each other naturally.

## Observation sheet

For every hole, capture:

- slot phase duration, number of distinct player actions, reroll use, and whether players can state the final headline;
- each turn’s visible duration, timeout, card use, collision, recovery, and whether a cause receipt matches participant understanding;
- shop duration and whether a player can explain card target and expiry before purchase;
- any confusion, dead time, accessibility issue, or player-to-player attribution dispute;
- seam/reveal/final overview cost and whether older hazards are mistakenly assumed to remain active.

Export the local simulator report and the in-game anonymous telemetry file. Keep raw participant notes separate from generated telemetry.

## Exit questions

Ask each participant privately:

1. “When did you feel responsible for the course or another player’s outcome?”
2. “Which rule, card, or terrain element was unclear?”
3. “Did a loss feel caused by a decision, visible timing, another player, or the game? Which one?”
4. “Where did you wait with nothing meaningful to do?”
5. “Would you choose the safe, skill, or conflict route differently next time?”
6. “What one card, terrain idea, or mechanic would make this more like a chaotic party game without making it harder to read?”

Report findings as observations with participant count and session context. A simulator report verifies state flow; it is not evidence of social enjoyment.
