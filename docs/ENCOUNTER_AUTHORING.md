# Composed encounters, preparation and thematic rewards

Gameplay content lives in `content/`; edit stable IDs and run the content validator before launching an explicit new run. Changed enemy composition or rewards change the gameplay content hash, so existing saves remain preserved and are not silently reinterpreted.

## Enemy groups

An `encounter` definition names one to four hostile character IDs, a tactics description and a reward ID. The first member is its persistent leader identity. Members retain their authored statistics at every party level. `game.vagabonds.encounters` selects the contextual pool for one planned group at each underground depth, 1–3; see [population selection, placement and auditing](ENCOUNTER_POPULATION.md). Discovery definitions can use `encounter` instead of an explicit `enemies` array; use an empty array when a composed group is selected.

The initial groups demonstrate three separate decisions:

| Group | Threat | Useful response |
| --- | --- | --- |
| Road wardens | Mender supports an armoured bruiser | Focus or stun the healer; exhaust its finite mana |
| Cinder ambush | Channeler prepares a party attack while a bowman targets weakness | Interrupt or defeat the channeler, or prepare guards |
| Hollow vigil | Bleed and dread accumulate | Remove the relevant attacker; heal bleed and calm stress |

`BehaviorRule.condition` accepts `always`, `ally-wounded`, `self-wounded` and `target-marked`. Conditions are evaluated when intentions are fixed at the beginning of a round. An allied healing action with `ally-wounded` selects a wounded ally, even when a healthy ally has fewer absolute HP; `self-wounded` selects the caster for an allied healing action. A `target-marked` attack selects a marked opponent. Losing a mark, receiving healing or falling later does not silently change an already declared target. Healing uses the same paid mana and targeting rules as other enemy actions.

Set `noRepeat: true` to exclude a behavior when its ability was that enemy's last actually executed action. Preparing starts an action; its saved release is still allowed next round. Stunned turns, interrupted releases and unaffordable releases do not overwrite action history. After an interrupted charge the caster must still execute a different action before it can prepare that attack again. The unconditional fallback cannot be marked `noRepeat`, so content validation rejects a rule that could fall through to the same forbidden ability. Limited-mana support and distinct ordinary attacks remain useful between special actions.

## Interruptible attacks

Set an enemy ability's `preparationTurns` to `1`. Its first action announces preparation through a `prepare` combat event, inflicts no damage and spends no mana. Next round retains the original target and releases the action, paying mana once. A fallen single target wastes the release; an all-enemy action still addresses the surviving opposing side. An unaffordable release is lost without making mana negative.

Stun immediately cancels preparation, including when the stun is cleansed before the enemy acts. Defeating the caster also removes its pending action. Guard remains ordinary damage protection and does not interrupt. The saved battle records the pending ability, fixed target and ready round, so saving and loading does not reroll an intention or restore a cancelled charge.

`EncounterGroupPreview.Prepare(world, encounterId)` creates a save-valid, durable fixture using normal combat resolution. Its default Cinder ambush order is Ranger, channeler, Warden, Arcanist, Vestal, bowman: guard with the Ranger, let the channeler prepare, then use Shield bash. `EncounterGroupPreview.Run(content, id, seed)` produces structured intentions, health frames and combat events for headless debugging. Fixture health is deliberately increased; generated-world enemies retain their authored values.

Run `./dev preview encounter encounter.cinder-ambush --seed 42` for the structured group report. Existing `preview encounter boss.<id>` commands continue to exercise bosses.

New ability IDs also need an additive mapping in `assets/combat-presentation.json`, whose presentation profiles are separate from gameplay content.

## Rewards

A `reward` definition supplies `gold`, `food`, `experience`, up to six optional item-ID `supplies`, `equipmentPool`, `equipmentChancePercent` and an optional guaranteed `signature` equipment item. Equipment pools are thematic alternatives; a victory selects at most one random entry per reward source. The signature is granted in addition to that entry. Equipment affixes use a separate seed stream and do not consume combat randomness. Supplies stop at the shared inventory limit of 9,999 per stack; the reward receipt reports the amount accepted.

Composed encounters and bosses each pay once for their persistent leader. Ungrouped mixed battles pay the reward attached to every defeated actor, so the first enemy does not determine everyone else's loot. Rewards are granted on victory; killing part of a group and retreating leaves its grant unpaid until the remaining members fall. Defeating the final guardian can finish the expedition with living attendants, and still grants that guardian's signature.

`RewardedEncounterIds` and unique `gear.7200.<source>.<choice>` identities persist with the world. Repeated victories or reloading cannot duplicate a source's grant. Conscious party members receive the authored experience; development choices are derived from their level. Habitat wildlife retains its ingredient remains and has an experience-only encounter reward.

Run `./dev preview reward reward.guardian-iron --seed 42` to grant the chosen profile through a real isolated victory, inspect generated gear, save/load in a temporary directory and replay the same source to verify duplicate protection. The JSON report includes `passed`, `saveRoundTripExact`, `sourceClaimPersisted`, `replayBlocked`, inventory changes and combat events. Player saves are never used.

Boss families share eight signature traditions: iron, cinder, river, thorn, night, storm, memory and stone. Tag signature items with `signature` to keep them out of generic cache pools and the unowned equipment catalogue. Every boss definition has an explicit reward reference. Ordinary character reward references fall back to `game.vagabonds.defaultReward` when omitted.

## Verification and bounds

`WorldEncounterGroupTests` exercises fixed strength, seeded composition, conditional support, preparation cost, interruption after actual cleansing, fixed fallen targets and exact saved continuation. `WorldEncounterRewardTests` covers thematic pools, mixed battles, partial victory then retreat/reload, unique gear claims, final-guardian signatures and malformed saved metadata. The native encounter fixture uses the same public preview helper, so its captured interaction is the same combat path tested headlessly.

`./dev audit-counterplay --seed 42` compares matched policies through the real resolver: stun versus allowing a charge, guard versus taking its full impact, and healer-first versus bruiser-first basic attacks. It reports HP, mana, stress, action counts, survival, enemy healing, decisions and ordered events. Charge fixtures deliberately give enemies enough health to observe the release; the healer comparison uses authored enemy health, a fixed initiative order and two attacking martial companions while both casters guard. This gives the support enemy time to act. `passed` checks bounded successful commands and the interruption/guard mechanisms. It does not claim either target-priority policy is optimal, or require a victory from either policy. Repeat across seeds and inspect the measurements before changing balance. The design rationale and primary sources are recorded in [ENCOUNTER_RESEARCH.md](ENCOUNTER_RESEARCH.md).

Groups are limited to four members; a battle grants at most four source rewards. Save validation rejects mismatched membership, unearned claims, invalid pending preparations and forged action history. Equipment remains subject to the shared unique-item registry and inventory bounds. Extend validation, reference indexing, the content schema, previews and these behavioural tests together when introducing a new condition or reward mechanic.
