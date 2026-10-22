# Composed encounters, preparation and thematic rewards

Gameplay content lives in `content/`; edit stable IDs and run the content validator before launching an explicit new run. Changed enemy composition or rewards change the gameplay content hash, so existing saves remain preserved and are not silently reinterpreted.

## Enemy groups

An `encounter` definition names one to four hostile character IDs, a tactics description and a reward ID. The first member is its persistent leader identity. Members retain their authored statistics at every party level. `game.vagabonds.encounters` chooses the pool used for one group on floor one or two of each dungeon site. Discovery definitions can use `encounter` instead of an explicit `enemies` array; use an empty array when a composed group is selected.

The initial groups demonstrate three separate decisions:

| Group | Threat | Useful response |
| --- | --- | --- |
| Road wardens | Mender supports an armoured bruiser | Focus or stun the healer; exhaust its finite mana |
| Cinder ambush | Channeler prepares a party attack while a bowman targets weakness | Interrupt or defeat the channeler, or prepare guards |
| Hollow vigil | Bleed and dread accumulate | Remove the relevant attacker; heal bleed and calm stress |

`BehaviorRule.condition` accepts `always` or `ally-wounded`. Conditions are evaluated when intentions are fixed at the beginning of a round. The wounded condition does not rewrite a previously declared action mid-round. Healing uses the same paid mana and targeting rules as other enemy actions.

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

Groups are limited to four members; a battle grants at most four source rewards. Save validation rejects mismatched membership, unearned claims and invalid pending preparations. Equipment remains subject to the shared unique-item registry and inventory bounds. Extend validation, reference indexing, the content schema, previews and these behavioural tests together when introducing a new condition or reward mechanic.
