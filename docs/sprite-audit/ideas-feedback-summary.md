# Idea review received — 19 October 2026

The user-downloaded `roag-sprite-ideas-feedback.json` was exported at
`2026-10-18T16:15:43.164Z` (19 October, 00:15 Singapore time).
Its [verbatim archive](ideas-feedback.json) has SHA-256
`32c9fe58f71068c15c5610130305a7421bf4564440fefa14a691217cb3c8d0f5`.

The export validates against the [reviewed proposals](ideas.json), fingerprint
`5a915a054757e645a396a54b70bab32033339f847ab737fbd27f8c3deb10f3c9`,
and native mapping fingerprint `1202bc99da0ba099`. The original
[review page](ideas.html), source PNGs and literal comments remain unchanged.
The [structured decision record](ideas-decisions.json) records provenance,
votes, conditions and pending decisions. This is a feedback handoff; these
new features are **not implemented yet**.

## Exact results

| Area | Approved | Rejected | Discuss / redirected |
| --- | ---: | ---: | ---: |
| Enemies / encounters | 12 | 2 | 1 |
| Playable classes | 6 | 1 | 1 |
| Items / pickups | 6 | 0 | 0 |
| Rooms / interactions | 8 | 4 | 1 |
| Mechanical terrain | 3 | 1 | 0 |
| Environment kits | 14 | 15 | 1 |
| Presentation / art | 6 | 7 | 0 |
| **Total** | **55** | **30** | **4** |

Both held groups remain unanswered/excluded. **113/165** available art-pool
decisions were answered. **48** approved ideas also have both art approvals;
comments still constrain those approvals. Seven approved ideas have pending
art decisions: Web Weaver, signposts, camp, drowned sanctum, broken cavern,
stone columns and weapon icons.

No thumbnail preferences or priorities were selected. There are therefore no
*selected* actor conflicts, but this does not establish final distinct actor
assignments. Pools overlap; specific bindings still need to be chosen from
the permitted art and checked at game scale.

## Changes requested in comments

### Enemies and classes

- **Web Weaver:** keep mixed Tiny `122`. Reject 1-Bit `414`, `463` and Micro
  `60` for this proposal. Use drawn web lines and find a suitable 1-Bit body.
- **Hollow Wraith:** retain an attack and support phasing through walls. Use
  **only 1-Bit `416`** for its body, not the other proposed figures. The original
  idea is marked Discuss; the comment changes the requested behavior/art.
- **Pilgrim:** make the HP-spend vow a **boon**, not another playable class.
- **Cantor:** reject the class. Consider **1-Bit `33` and `824`** for boon/item
  artwork. This does not approve the class’s three-note mechanic.

The six approved classes are Duelist, Geomancer, Spiritbinder, Tinkerer,
Stormcaller and Wayfinder. The approved enemies are Grave Cantor, haunted
cache, Heavy Rammer, Web Weaver, Gulp Toad, Mirror Hexer, Silt Worm, Oath Twins,
Alarm Keeper, Ember Moth, Bone Splitter and Rotating Sentinel. Brood Keeper
and Echo Duelist are rejected. Wraith remains the explicitly redirected case.

### Items, interactions and terrain

- **Escape scroll:** exclude **1-Bit `525`** in both looks.
- **Charm well:** exclude **RPG `855`**.
- **Memorial:** exclude **1-Bit `24`** for this proposal.
- **Heat vents:** the user asks whether approved Tiny/mixed heat motifs can
  supply the monochrome counterpart. They can, using a derived conversion
  with preserved source pixels/alpha and nearest sampling, followed by visual
  verification; merely multiplying a coloured image by grey does not produce
  a true monochrome conversion.
- **Push floor:** inspect **all related Micro arrow cells**, not just the
  initial samples. Directional cues need exact semantic assignment.
- **Route signs:** exclude **RPG `22`, `425`, `737`, `794`, `851`, `908`**.
- **Camp:** exclude the proposed RPG tent-part assemblies (`616`, `617`,
  `618`, `619`, `674`, `675`); use the expressly accepted **Micro `117`** and
  **1-Bit `508`, `509`** alternatives.

All six item concepts are approved. The approved room/interactions are the
attunement forge, finite trader, memorial, bell sequence, falling fixture,
signposts, camp and Archivist contract. The approved terrain mechanics are
heat vents, push floor and fragile optional crossing. Practice rack, mirror
puzzle, prism relay, sparring trial and the sluice are rejected.

### Environment and presentation

- **Merge Crypt/Ossuary and Pale Sanctuary:** Crypt provides foreground
  objects/interactables/dressing; Pale Sanctuary provides the area’s
  background/ground material. Do not create two unrelated environments or
  use gravestones/door frames as repeating floor art. Whether this is a room
  family first or a full additional biome still needs a concrete design.
- **Monochrome counterparts:** allow the approved mixed source art, converted
  or touched up to monochrome, for drowned sanctum, broken cavern and stone
  columns; the heat-vent request follows the same policy. Keep approved native
  1-Bit art where it is suitable. This changes the earlier assumption that
  standalone must load only its original PNG, while preserving editable PNG
  bytes, stable IDs and presentation-only derived resources.
- **Weapon icons:** keep the approved pool except **Micro `78` and `79`**.
- **Bitmap labels:** the overall vote is **Reject**, despite approval of both
  art pools. Keep the current text workflow; do not infer implementation
  approval from the pool votes.
- Rejected bulk material, body-variant and UI-skin proposals remain rejected.
  The approved symbol library is not permission to revive those features.

Exclusions above apply to the stated proposal. For example, the user excluded
1-Bit `24` from the memorial while approving the Crypt art pool containing it;
do not silently turn that into a global ban or ignore the memorial exclusion.

### Replacement boon requests

**Oracle room is not wanted.** Its Discuss comment requests a dice boon:

> attack to be different on every dice roll

The requested art/results include faces **1–6**, **skull = instant kill** and
**cross = miss**. Define the seeded roll timing, six ordinary outcomes,
probabilities, multi-target behavior and proc interactions before coding.
Preview/render must not consume gameplay RNG, and saves must preserve the
resolved outcome and next deterministic continuation.

**Clarified in conversation on 19 October:** the user selected
**“Regular enemies only (recommended)”**. Skull may instantly kill ordinary
regular enemies; minibosses and the final boss are exempt. Special encounters
must not become eligible merely because they have an AI actor. The original
download is preserved unchanged; this later answer is recorded separately in
`ideas-decisions.json`.

The symbol-library comment also asks for **50+ distinct, materially
gameplay-changing boon ideas** with suitable icons, using art left unused
after the accepted assignments. No further approval round is required for
that brainstorming. Check against the **53 existing boons** to avoid renaming
existing mechanics, and retain the requested Pilgrim and Cantor-art directions.
This conditional design request is not a claim that 50 new boons already exist.

## Implementation order suggested by dependencies

No priorities were selected in the export; this order is an engineering plan,
not a user vote:

1. Preserve this feedback/review and establish the actual headless baseline.
   Record exact allowed/forbidden cells and select readable, compatible art.
2. Add shared deterministic mechanics with focused tests: legal player shove,
   temporary terrain lifetime/rollback, corpse/revival budget, finite carried
   charges and allied ownership. No free attacks from presentation time.
3. Implement approved enemies in small batches, with locked telegraphs,
   simultaneous primary HP batches, fog, reward caps and disk continuation.
   Find Web Weaver art and honor the Wraith redirect rather than the old draft.
4. Add the six approved classes and the finite item/room/terrain choices,
   building on those tested mechanics. Apply every sprite exclusion.
5. Integrate the combined Crypt/Sanctuary and other approved environment kits
   with separate ground/wall/prop roles, connected geometry, proper art scale,
   derived monochrome counterparts and published/loose/builtin fallback.
6. Define and implement the redirected boons once their rules are concrete.
   Inventory remaining approved icons and design the requested 50+ distinct
   boon concepts with the existing catalogue/proc system in mind.
7. Run headless/save/seed regressions after each gameplay batch. Existing user
   instruction still prohibits tests that open the LÖVE app; record GUI/physical
   input acceptance as outstanding unless that restriction is later lifted.

## Reproduce this read-only review

```sh
node tools/sprite_ideas_feedback.js docs/sprite-audit/ideas-feedback.json
make sprite-ideas-test
```

The first command validates and summarizes the downloaded records, including
literal comments. It does not execute comments or implement/publish features.
No LÖVE process, gameplay state, player save/profile or source PNG was changed
while receiving this feedback.
