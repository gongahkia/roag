# Retained ability and talent integration

The user approved class-appropriate boon unlocks for imported active skills on 2026-11-02. Native class moves remain available. No separate mana or innate talent progression is introduced. This is an implementation/preservation inventory, not full-game acceptance.

`RetainedBoons` explicitly maps eight additional player skills and all 22 imported talent definitions. The original 103 definitions, 20 starter entries and 99 normal entries remain intact in `BoonCatalog.Original`; the integrated catalog has 133 definitions and 129 normal entries. Original-only reference fixtures retain their original pool. New production survival runs use the integrated pool; continuing saves retain their exact saved available pool. Existing assigned boons never transfer or reroll. All new illustrations use the source definitions' existing stored universal-atlas aliases, verified against the embedded sprite manifest.

| Original skill | Eligible native classes | Native behavior |
| --- | --- | --- |
| Shield bash | Sentinel, Adventurer, Geomancer | Adjacent direct strike; interrupts a real locked attack and applies one second of stun without refunding committed recovery. |
| Stand fast | Sentinel, Geomancer, Spiritbinder | Visible nearby allies lose stress and receive a finite two-point ward at common strength, expiring at their next own readiness. Guard stays a deliberate paid facing action. Actual ward shielding credits its caster. |
| Hunter's mark | Ranger, Trapper, Wayfinder | Visible clear shot within six tiles; surviving hit target is marked for three own turns. Direct attacks gain four power; actual bonus damage credits the marker, bounded by actual loss. |
| Volley | Ranger, Trapper | Direct attack against visible hostiles within six clear tiles. Bodies block individual shots; no cross-floor or remote targets. |
| Ember | Arcanist, Alchemist | Clear visible shot within six tiles plus three ordinary burning ticks. Reuses committed native burn timing/refresh and causal ownership. |
| Chain arc | Arcanist, Stormcaller | Direct lightning against visible hostiles within four clear tiles. A Stormcaller spends one existing charge; no separate resource is created. |
| Mend | Spiritbinder, Alchemist | Conscious ally within three clear tiles: six HP at common strength, stops ongoing bleed, never revives or removes wounds. Actual healing/source credit and recipient excess-healing ward use the medicine/camp healing path. |
| Absolution | Spiritbinder, Sentinel | Visible allies within three clear tiles lose 20 stress plus owned calming bonuses. Free rejection if nobody needs calming. |

Active skills use the existing paid action queue, class special recovery plus converted former mana cost, physical private sight, actual terrain/body lines, native direct attacks/fate/critical/focus/boon events, ownership and shared continuing saves. L/Menu → Skills opens illustrated free inspection, then target review and paid confirmation. Inspected recovering members cannot spend another member's turn. Existing input strips/action grid remain removed.

All 22 talents become owned boon effects. Original Warden talents fit Sentinel/Adventurer/Geomancer/Berserker; Ranger talents fit Ranger/Trapper/Wayfinder; Arcanist talents fit Arcanist/Stormcaller/Alchemist; Vestal talents fit Spiritbinder/Alchemist/Sentinel. The six formerly innate talents can be acquired by any class. Bonuses cover wounded/marked/stunned damage, area/fire damage, initiated cooperative damage, healing, speed, calming, physical ally cover and converted initiation-cost discounts. Helpers keep their own undiscounted assistance cost. These affect future actions, never existing readiness. Rarity/duplicates add strength; each combined effect is capped at 20. Source talent identities remain in the explicit mapping; actors do not regain a parallel `Talents` layer.

Native bleed, mark and rally ward conditions retain source/action/ability identity, own-turn duration, validation, corpse/history retention and causal-chain retention. Native ongoing bleed is distinct from five-own-turn downed bleed-out. Egg preparation reduces actual bleed ticks; tea and resolve gear reduce native dread stress. At this checkpoint **natural imported enemy dread/bleed casts remain pending their controller integration**; backend effect tests do not prove those encounters are playable.

| Other original ability IDs | Preservation path / remaining work |
| --- | --- |
| `longsword`, `arrow`, `wand`, `smite` | Adapted to retained native class basic moves and spatial geometry; extra old basic-action buttons are unnecessary. |
| `guard`, `medicine`, `retreat` | Paid native facing Guard, paid conscious medicine, physical paid movement/escape. Retreat cannot magically end pursuit or extract the party. |
| `twin-strike`, `shared-aid` | Delivered native cooperation: both owners pay recovery, recovering helpers extend existing readiness, actual bounded contribution credit. |
| `dread`, `rend`, `sting`, `hex`, `interrupt`, `web-snare` | Native dread/bleed/mark/stun consumers now exist. Actual imported species/boss rules, role targeting and rendered casts remain open. |
| `strike`, `nip`, `sweep`, `drain`, `scorch`, `cinder-surge`, `recover`, `purify`, `rally-host` | Still require imported creature/boss native controllers and their authored target/support/healing/status/phase behavior. Do not treat the player-skill checkpoint as completing these. |

This inventory covers all 32 authored ability definitions. The 48 retained Vagabonds boss definitions and their behavior/reward/site integration remain open alongside the four approved additional boss concepts. Broader world acquisition, all retained species/routines, trade/economy/calendar/ecology, continuous complexes, campaign/profile and final rendered/Linux gates remain required.
