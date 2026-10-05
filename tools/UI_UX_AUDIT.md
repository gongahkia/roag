# Gameplay Screen UX Audit

Audit date: 2026-10-05. Scope: every runtime screen reachable through `src/app/app.lua`, plus the field HUD and its contextual state. This is an interaction audit, not a redesign of simulation rules.

## Decision rules

- A player should be able to answer: **where am I, what mode am I in, what will my next input do, does time advance, and what persists?**
- Campaign and legacy/run-based mode may share rendering, but their copy must not imply the other mode's rules.
- A screen that pauses play must say so. A field posture such as Build stance must say that it does not.
- The screen that changes ownership, death state, zone, or body must explicitly explain the consequence before returning control.

## Audit results

| Surface | Player question | Current clarity / action |
| --- | --- | --- |
| Title | Which game am I resuming or starting? | `CONTINUE CAMPAIGN` and `LEGACY RUN` already distinguish persistent Campaign from compatibility mode. Keep this distinction in title copy. |
| Campaign slots / replacement | Which save will change? | Good: slot selection and replacement are explicit. No modal action needed. |
| Legacy onboarding | Is this the same as Campaign? | Corrected copy: it now identifies itself as a legacy run and points out that Campaign has anchor-based succession. |
| Help | What are Campaign's essential rules? | Corrected stale floor-only reconstruction/death copy; added an explicit Campaign death-and-anchor section. |
| Normal Campaign field HUD | Where will death send me? What does the field control now? | Added an always-visible local/remote respawn-anchor status. Existing HUD already shows facing loadout, faced `U` context, build state, and controls. |
| Build stance | Does the map pause? What does `E` do? | Good: HUD names the selected recipe/cost/validity and remaps controls. It intentionally remains a live field posture. |
| Inventory / loadout | Can I safely organize cargo and bindings? | Added `PAUSED` to the heading. Existing controls describe drag, rotate, drop, loadout, and close. |
| Corpse salvage | Is this loot deliberate and is the world stopped? | Added an explicit pause label. Dual grids, drag preview, rotation, keyboard fallback, and close controls already make transfer ownership clear. |
| Storage | Is moving cargo safe / free? | Added an explicit pause label. It remains a compact list transfer view; it does not change storage ownership or persistence rules. |
| Reconstruction | Can I leave? Does it cost time? | Added `PAUSED` copy and made `Escape` finish/close equivalently to `F`. It remains the only body-installation surface. |
| Services / final hub | What am I buying and can I afford it? | Existing labels show price, stock, sold state, affordability, and scrap. No rules change needed. |
| Campaign succession | Why did the world suddenly change? Where is my old body? | **Fixed:** new acknowledgement screen names the death site and active anchor, distinguishes retained world state from lost charms, and explains how to move the future anchor. It pauses before returning control. |
| Legacy route / curse / body-ability menu | Which choice advances the old route? | Existing title/footer language is direct. These are legacy-only compatibility surfaces and should not appear in ordinary Campaign flow. |
| Research / Fallen Archive | Is this live inventory or permanent profile data? | Existing subtitles correctly distinguish permanent account progression and non-live historical bodies. |
| Game over / victory | Is this Campaign succession? | Legacy terminal screens stay terminal; Campaign now uses the distinct succession screen instead. |

## Deliberate boundaries

- This pass does **not** turn storage into another spatial-grid UI. Its transfer semantics are already authoritative and persistent; changing its presentation needs a separate playtest-driven decision.
- No extra map, waypoint, fast-travel, fog, or objective system was added. The succession screen provides enough immediate recovery context without inventing an unrelated navigation feature.
- No simulation timing changed. Inventory-like overlays pause; Build stance remains turn-based but live; the succession acknowledgement is a post-death presentation pause only.

## Human playtest questions

- After seeing a succession screen once, can a new player accurately predict where the next body will appear and what remains recoverable?
- Does `RESPAWN ANCHOR: THIS ZONE / REMOTE ZONE` make the field HUD clearer without becoming visual noise?
- Do the pause labels remove uncertainty in inventory, salvage, storage, and reconstruction?
- Are any field actions still surprising because their current mode is not obvious from the HUD?
