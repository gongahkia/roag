# Qud-style possibility audit

Audit date: 10 September 2026. Code inspected at `f109c8a` after the
Aftermath, Motion and Reciprocity implementation. This is a comparison of
design strengths, not a claim that Jomon should copy Caves of Qud's setting,
content, controls, or scale.

## Reference boundary

Freehold describes Caves of Qud as a hybrid handwritten and procedural world.
Its [official feature list](https://cavesofqud.com/) names more than 70 mutations and defects, 24 castes
and kits, more than 70 factions, fully equipped and embodied NPCs, pervasive
wall destruction, and a world approaching one million maps. The developers'
[systems-driven interview](https://unity.com/resources/systems-driven-design-in-caves-of-qud)
emphasises authored systems whose collisions let players make stories, small
composable object properties, layered generation, and giving AI and narrative
"interesting bones" rather than simulating every thought. Their
[object](https://freehold.atlassian.net/wiki/spaces/CQP/pages/23691295/Object%2BModding)
and [population](https://freehold.atlassian.net/wiki/spaces/CQP/pages/25690114/Encounter%2Band%2BPopulation%2BModding)
documentation shows how data-defined parts and population tables multiply new
content across existing systems. The developers' [history
paper](https://www.freeholdgames.com/papers/Generation_of_Mythic_Biographies_in_CavesofQud.pdf)
describes state-machine events and post-hoc causal rationalisation.

Jomon cannot honestly reach Qud's fifteen-plus years of breadth in a content
pass, and doing so would erase Jomon's narrower identity. The useful parity
target is **possibility density**: how often movement, equipment, actors,
materials, history and obligations combine into a decision the player can
understand and reuse.

## Findings

| Axis | Current Jomon evidence | Relative verdict | Highest-value remaining gap |
|---|---|---|---|
| Movement | Eight seamless four-level regions; terrain, water, load, injury, weather, sight, noise and climbing alter the ordinary move reducer. Known-route following uses that reducer and interrupts on changed information. | Strong scoped parity. Jomon has more consequential local footing than many roguelikes, but vastly less spatial breadth than Qud. | More *local route choices*, not more walking distance: contextual vault, haul, ferry, crawl and rappel opportunities, plus changed return routes. |
| Interaction | Eleven shared material verbs, doors, controls, stores, cargo, equipment, witnesses, vessel stations and environmental reactions are physical and persistent. | Strong but less universal. Important Jomon objects participate; Qud's component catalogue supports far more surprising object/ability combinations. | Give ordinary furniture, dropped cargo, neutral workers and barriers two or three more shared capabilities without an ECS rewrite. |
| Combat | Thirty-six weapon families, physical ammunition and hostile kit, local body conditions, target preview, cover/elevation, warned reactions, morale, surrender, theft, rescue, alarms and environmental control. | Tactically legible and reciprocal; narrower than Qud. Jomon trades Qud's mutation/skill/body breadth for clearer counterplay. | More player-authored turn sequences. Many of Jomon's 32 techniques are contextual modifiers rather than deliberately activated manoeuvres. |
| Encounter ecology | 72 validated standard signatures, 24 elite situations, eight persistent rivals, ten tactical roles and high-fidelity/coarse population layers. Groups fight, protect, scavenge and react to hazards. | Good systemic base, moderate experiential variety. The roster still resolves through four broad profiles, and default ranged assignment is overconcentrated on slings. | Authored mixed-situation grammars that combine factions, animals, work, weather and aftermath; tune the underlying profiles before adding another roster tier. |
| World causality | Five bounded events per region connect terrain, work, markets, institutions, testimony, treasure and quests. Sixteen ending-derived aftermath configurations create later physical work. | Near the intended Qud lesson at Jomon's scale: generated facts are visible and consequential. | A second late-campaign consequence layer so repaired places and institutions collide with one another, not only with their originating ending. |
| Stories and relationships | 20 regional lines, five cross-region arcs with eleven endings, 12 institutions, 38 persistent adults, separate trust/obligation/confidence and physical evidence. | Coherent but shallow in conversational expression. Qud's authored lore and faction breadth are orders of magnitude larger. | Conditional dialogue and unsolicited encounters that express existing memories; avoid adding disconnected quest labels. |
| Builds | 36 weapons, 36 armour pieces, 80 active passives/techniques, 51 tools/supplies/drinks, 16 relics and eight vessel refits. Twenty-four automated scenarios demonstrate cross-system effects. | Strong grounded combinatorics, below Qud's character-construction breadth. | Active technique loadouts and encounter contexts that make the same physical kit demand different plans. |
| Campaign breadth | Eight destinations, 16 route nodes, 62 persistent stores and 12 voyage families. | Not at Qud parity and not close by raw volume. | Denser revisits, local transformations and cross-region interference before any ninth major region. |
| Readability and iteration speed | Zero-time previews, fact/rumour/forecast distinctions, semantic glyphs, deterministic replay, focused audits, fast suite and 80x24 support. | A Jomon strength. The bounded terminal presentation makes consequences unusually inspectable. | Reduce repeated contact framing and surface the next useful contextual verb more directly. |

### Measured movement result

The pre-pass audit measured 116–182 manual inputs for a safe
landing–objective–landing path. On the final eight-region fixture, those paths
are now 110, 164, 182, 120, 148, 154, 140 and 164 ordinary actions. Once the
ground is remembered, two route invocations require four player commands while
executing exactly the same reducers and action-clock steps. That is a
96.4–97.8% input reduction, comfortably beyond the 60% gate. It removes dead
input without teleporting or suppressing danger.

## What this pass achieved

The completed pass closes the most important near-term comparison gaps:

- movement can be repeated safely without turning travel into a separate
  simulation;
- trained opponents now own, damage, lose and recover the same physical kind
  of equipment as the courier;
- all attacks select and explain their target, and six reach families support
  a named prepared reaction;
- regional endings produce revisitable terrain, work, services, populations
  and contracts rather than only ending text;
- every region gained an aftermath line and elite situation, while two new
  arcs make repaired scars and low-water refuges cross regional boundaries;
- travelling witnesses, reciprocal practices, finite preparations, relics,
  refits and voyage variants reuse the material, relationship and cargo rules.

This is enough to call Jomon systemically dense for its present footprint. It
is not enough to call it content-volume-equivalent to Qud.

## Completed content pass: situations and active mastery

The September 2026 pass moved Jomon meaningfully closer to Qud-like
possibility density by targeting **combinations per hour**, not catalogue
size. The numbered programme below is retained as the design record; all seven
items are implemented and verified in `situations-active-mastery-milestone.md`.

1. Add 24 authored encounter grammars—one per region and pressure band. Each
   must combine at least two existing groups, one physical duty, one material
   condition and one persistent consequence. Reuse the 72 actors before adding
   more.
2. Turn 12 learned practices into explicit, inspectable manoeuvres available
   through the existing attack, guard and field menus: controlled withdrawal,
   hook-and-pass, shield bind, high cast, braced advance, quiet takedown and
   similarly grounded actions. Each needs a setup, counter, physical condition
   and enemy consumer.
3. Add 24 mutable micro-sites—three per region—assembled from authored pieces
   and selected by history, season and aftermath. Each needs two approaches and
   at least one changed revisit, not another container at the end of a corridor.
4. Add eight cross-region interference events in which one institution,
   shipment, rival or ecological change enters another region. The event must
   use already recorded state and leave both places changed.
5. Add two late-campaign household stories and one all-region capstone that
   consume courier death/succession, vessel condition, institutional accounts
   and at least four regional outcomes. Do not gate immediate expedition play.
6. Give the 12 voyage variants one later echo each: a returned traveller,
   altered cargo claim, repaired deck scar, route rumour or rival preparation.
7. Play and rebalance the existing 24 builds across the new grammars. Only add
   equipment when a tested encounter exposes a missing tactical answer.

The completed gates include a 200-seed occurrence/variety audit, no encounter
grammar above 10% of all audited opportunities, three materially distinct
solutions per situation, real-PTY presentation in all eight regions, and the
existing latency/memory ceilings. Existing rival lifecycle tests remained
green; this pass did not falsely count them as new manual sessions. This
improved the part of Qud
worth approaching—recombinable situations—without importing mutations,
science-fantasy content, huge empty geography, unrestricted destruction, a
skill tree, or a general entity framework.

## Candid bottom line

Jomon remains much closer to Qud in **design method** than in **quantity**. Its
materials, physical ownership, causal histories and persistent obligations
already create legitimate emergent play. This pass materially reduced the
distance in authored combinations, deliberate techniques and late-campaign
cross-region recombination. A raw enemy/item/region count pass would make the
game longer to audit without making it more Qud-like.
