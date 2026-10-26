# Sprite review feedback · 18 October 2026

## Implementation

The approved round-four mechanics and art are implemented in `src/review_features.lua`,
`src/game.lua`, `src/classes.lua`, and the two native sprite resolvers. The mixed
look loads the approved Tiny Dungeon and RPG source sheets from
`assets/kenney/approved/`; the 1-Bit look uses approved cells from its existing
sheet. Generated floors now place optional key vaults, timed plate gates,
ore, braziers, ward flasks, pillars, and biome scenery. Shade, Hookrunner,
Trapper, and Ring of Reversal are available in the normal game. The rejected
spore pod, bomb, and moving cart have no behavior. These mechanics use plain
saved state; old saves remain loadable without newly generated review objects.


This records all four completed reviews of [the interactive page](review.html).
Earlier votes and answers remain available in their own tabs.
The downloaded `roag-sprite-feedback.json` had schema `roag-sprite-review/1`,
referenced audit commit `3b58b1b9`, and contained every expected proposal ID.
It was exported at `2026-10-18T02:13:29.494Z`. There were 10 comments and no
individual preferred-cell selections. Votes are preserved below; comments
refine their meaning but do not silently reverse them.
The source download has SHA-256
`a9a4c6dcdab2d6d26e5e0d1478826f10334876a6b2bf012ba540dd68d25767ae`.

| Proposal | Idea vote | Art vote | First-round interpretation |
| --- | --- | --- | --- |
| Secret crack clues | Approve | Approve | Ready for visual implementation. |
| Attack center and impact | Approve | Approve | Ready, while preserving dotted tactical outlines. |
| Class attack symbols | Approve | Approve | Ready for existing attacks; no equipment system implied. |
| Trees and overgrowth | Approve | Approve | Ready as biome dressing. |
| Fungal growth and spore pod | Approve | Discuss | The proposed 1-Bit 315–317 silhouettes read as overhanging slime. Find a different standalone motif before using them as mushrooms. Decoration and a hittable pod remain separate implementation steps. |
| Fragile ore vein | Approve | Discuss | Use 1-Bit **6** for the intact vein and **524** for the collected/depleted state; do not assume 525 is accepted. The reward behavior still needs balancing. |
| Lit and unlit brazier | Approve | Discuss | 1-Bit **738–739** are suitable; exclude **740**. 1-Bit **570** reads as a fireplace and is a candidate for the same light mechanic. |
| Key and optional vault | **Reject** | Approve | Comment says the idea is liked but the mixed Kenney art is wrong. Treat this contradiction as **unresolved** pending clarification and replacement art review. |
| Pressure switch and timed gate | **Reject** | Approve | Same vote/comment contradiction as the vault. Do not implement the timed gate yet. |
| Instant flask pickup | Discuss | Approve | Micro **106–107** are suitable; exclude **123**. Decide the pickup effect before implementing. |
| Volatile vessel | Reject | Reject | Drop the bomb interpretation. Micro **89** and 1-Bit **337–340** read as rings or boon imagery; 1-Bit **570** reads as a fireplace. |
| Hooded NPC or enemy | Approve | Approve | The sprite pair is accepted. Choose NPC versus enemy and its role before adding an entity. |
| Water crossing | Discuss | Discuss | Reinterpret the Caves pieces as tracks. Abandoned mine-rail dressing in a cave is the smallest option; a moving train or cart is a separate, unapproved mechanic. |
| Pillar or cover | Discuss | Discuss | Construct the whole pillar from sections in both looks before showing it. Collision and cover rules are still undecided. |

## Concrete art corrections

- **Pillars:** Caves top/base pairs are **145/174, 146/175, 147/176,
  148/177, 149/178**. In 1-Bit, the unused cells **542, 591, 640** form a
  plausible top/middle/base column in one source-sheet column. Verify the
  assembled silhouette at game scale before approving it.
- **Tracks:** Caves **445–446, 474–475, 503–504** make a three-row,
  two-column rail-like assembly. 1-Bit **245** and **294** read as vertical
  rail/ladder motifs. A decorative mine rail can use these without changing
  water walkability or pathfinding.
- **Rings:** Before adding another boon to the existing 52, consider using
  Micro **89** and 1-Bit **337–340** as pack-specific jewelry art for a
  circular or protective boon. `Expanding Sigil`, `Pulse Step` and `Betrayal
  Ward` are existing gameplay candidates. This is a new art proposal, not an
  approved replacement or a new mechanic.

All indices in these corrections are currently nonempty and unbound in the
relevant art look, according to [tiles.csv](tiles.csv). The proposed pillar
and rail assemblies are visual hypotheses; source adjacency alone does not
prove they look correct in ROAG's renderer.

## Alternate Kenney art to review for gates and switches

The present repository has only Micro Roguelike, Caves & Dungeons and 1-Bit.
The existing Micro and Caves key/gate/switch proposal was declined in the
comments. I inspected these **external** Kenney archives in a temporary
directory; none has been added to the project or approved for use:

| Pack | Why it is worth a second review | Limit |
| --- | --- | --- |
| [Roguelike/RPG pack](https://kenney.nl/assets/roguelike-rpg-pack) | 16×16, CC0, large dungeon and object sheet with many door and fixture alternatives; closest broad search for the mixed look. | Exact key, gate and switch cells still need a visual shortlist. |
| [Tiny Dungeon](https://kenney.nl/assets/tiny-dungeon) | 16×16, CC0, compact set with visibly distinct dungeon doors, barriers and floor fixtures. | More saturated style than current Caves terrain; no exact matching key confirmed yet. |
| [Roguelike Indoors](https://kenney.nl/assets/roguelike-indoors) | CC0 architectural and furnishing set with additional gates, rails and wall pieces. | Best for architecture, less convincing as a complete key/switch solution. |

Do not substitute new pack art into the mixed look until the actual cells and
style fit have been shown for review. Standalone 1-Bit remains a separate,
self-contained choice.

## After the first review

At this point, the four visual changes could proceed together. The vault and
timed-gate votes still conflicted with their comments. The second review below
resolves those conflicts and supersedes this first-round decision boundary.

## Second review · 18 October 2026

The downloaded `roag-sprite-feedback-round2.json` has schema
`roag-sprite-review/2`, audit commit `3b58b1b9`, and export time
`2026-10-18T02:30:27.943Z`. Its SHA-256 is
`757437e8297a499a7f603fc73b450222496bca66cfd500a4e5b914b0ddc217d3`.
It contains all ten follow-up records and preserves the fourteen original
decision records exactly. Nine follow-ups have a selected option; the hooded
character has a free-text answer instead. The first two answers override the
earlier crossed votes on *idea intent*; the original vote data stays intact.

| Topic | User's second-round direction | What it means now |
| --- | --- | --- |
| Key vault | Keep the vault idea; replace mixed art | The optional-vault mechanic is wanted. The proposed mixed Micro/Caves art is not accepted. Show exact alternative cells before binding art. Keep the main route independent of its key. |
| Timed gate | Keep the gate idea; replace mixed art | The timed-gate mechanic is wanted. Find and review new mixed-look gate/switch art. Timing, persistence and safe routing still need a concrete design. |
| Fungal growth | Mushroom scenery only | Do not add the hittable spore pod. The 1-Bit #315–317 silhouettes are still unsuitable as mushrooms; find a better pair before dressing both looks. |
| Ore vein | Breaking it drops currency | Use 1-Bit #6 intact and #524 depleted; exclude #525. Set drop amount and placement through a later balance pass. |
| Brazier | Create a light or safe zone | A gameplay zone is wanted, but its exact effect, radius and duration are unspecified. Keep 1-Bit #738–739; #570 is a separate fireplace candidate; exclude #740. |
| Flask pickup | Grant a one-hit ward | Use Micro #106–107, exclude #123. Define when the ward expires and how it interacts with existing defensive boons. |
| Hooded actor | New playable character with a different ability | This free-text answer replaces the NPC/enemy options. The paired Micro #8 / 1-Bit #79 art is accepted. Design a distinct class kit before adding the character; a name, attack geometry and ability behavior are still open. |
| Mine rails | Abandoned mine-rail scenery | No moving train or cart mechanic. Assemble the pieces as cave dressing without changing walkability. |
| Pillars | Blocking cover; assembled art looks good | Both looks should show full columns. Cover collision and projectile behavior still need rules; a three-tile Caves assembly is possible (see below). |
| Rings | Design a new ring boon | The volatile bomb idea remains rejected. Revisit Micro #89 and 1-Bit #337–340 as possible art after defining a new boon effect; this is not approval of a specific sprite binding. |

### Three-tile Caves pillar check

The Caves sheet has an unused plain shaft cell **#177**. Stacking **#145**
(capital), **#177** (shaft) and **#174** (base) produces a visually continuous
three-tile column in a source-cell composite. The matching 1-Bit proposal is
**#542/#591/#640**. All six cells are nonempty and unbound in
[tiles.csv](tiles.csv). The updated [review page](review.html) shows both
assemblies at the same three-cell height. This answers the sheet question;
the assembled Caves version has not yet been inspected by the user in the
game renderer.

### Second-round decision boundary (superseded by the third review)

- The four first-round visual improvements, ore currency drops, decorative
  mine rails and the one-hit ward pickup have a clear direction. Their exact
  placement, balance and integration can be designed against existing rules.
- The fungal scenery needs replacement 1-Bit mushroom art. Vault and timed
  gate need exact replacement mixed-look art. Do not treat the new pack shortlist
  above as art approval.
- The playable character needs a genuinely distinct ability proposal; the ring
  boon needs an effect proposal. The brazier's light/safe zone needs a specific
  rule. Pillar cover needs movement and projectile rules and a visual check of
  the new three-tile Caves assembly at game scale.
- The spore pod, volatile vessel and moving cart are not selected mechanics.

## Third review · 18 October 2026

The downloaded `roag-sprite-feedback-round3.json` has schema
`roag-sprite-review/3`, audit commit `3b58b1b9`, and export time
`2026-10-18T02:48:38.501Z`. Its SHA-256 is
`be4439fcf785445eac95932e250ca5c483837543530e8abcfddc7534820ed374`.
It contains all twelve third-round cards and preserves the first fourteen
decisions and ten follow-up records exactly as exported in round two.

| Topic | Third-round answer | Current interpretation |
| --- | --- | --- |
| Playable classes | Shade selected; comment requests Hookrunner **and** Trapper as two more classes | Three new classes are wanted. Shade uses approved Micro #8 / 1-Bit #79. The other two class kits were described on the page, but their actor art needs a separate approval. The single-select Shade button does not exclude the two classes named in the comment. |
| Ring boon | Ring of Reversal | Use Micro #89 and 1-Bit #337. The proposed evade-and-answer effect is the selected starting design. |
| Brazier | Beacon | A two-cell radius stops mist from blocking sight for 300 simulated time; it grants no defense or unexplored-map reveal. |
| Pillar | Permanent solid cover | A one-cell footprint blocks movement, projectiles and sight; preserve a route around it. Both three-cell art assemblies were approved. |
| Ward flask | Full block, short window | Ignore one complete hostile hit within three committed player actions. Limit to one active pickup ward; confirm interaction with existing boon ward during implementation. |
| Vault | User asks for variable outcomes: currency and health, clue and currency, a free boon, enemies, and possibly others | Keep the optional side vault. A single fixed reward is not the direction. The exact outcome pool and whether a hostile outcome still guarantees a reward need one concise decision. |
| Timed gate | Either faction opens it for 300 simulated time; re-triggering refreshes the timer | Keep the exit independent of this gate; display state and countdown. |
| Vault art | RPG gray key #671 and plain barred door #158/#215 approved | These are review-only sources until runtime assets are intentionally imported. |
| Gate art | Button selected Tiny #54/#55 plate and #45 door; comment says **both** this and RPG #959/#961 plate with #161/#218 gate look good | Treat **both** art sets as approved. The old single-select control failed to express the user's choice; it is corrected in the page. |
| Fungal art | 1-Bit #103 and #54 | Two mushroom scenery variants, with no hittable pod. |

The three class concepts already shown to the user are distinct starting kits:

| Class | F attack | R signature | X movement |
| --- | --- | --- | --- |
| Shade | Two-cell thrown blade | Place one adjacent decoy that draws the next enemy attack aimed at the player | Swap with the decoy once |
| Hookrunner | Adjacent knife | Pull the first ordinary foe along a three-cell straight hook line up to two clear cells; bosses take a hit without moving | Short sidestep |
| Trapper | Adjacent knife | Place one visible snare on an adjacent open tile; the first hostile entering takes damage and loses its next move | Short roll |

These are design starting points from the review card, including provisional
range and timing. Implementing Shade requires an explicit enemy-targeting rule;
implementing Trapper requires visible snare state. The art review below does
not change those class requests.

### Fourth-round art shortlist

The current Micro sheet has one free, complete actor silhouette in its main
character row; that is already assigned to Shade. Hookrunner and Trapper
therefore need different mixed-look source art or newly authored Micro-scale
sprites. The [fourth-round cards](review.html) showed these candidate pairs
alongside alternatives; the selected pairs are recorded below:

- Hookrunner: Tiny Dungeon #111 (masked figure) with unbound 1-Bit #80;
  alternate Tiny #84 with 1-Bit #78.
- Trapper: Tiny Dungeon #112 (green scout) with unbound 1-Bit #77;
  alternate Tiny #88 with 1-Bit #222.

The Tiny Dungeon figures are 16px source cells while existing mixed-look
characters are 8px Micro cells. They must be evaluated at the game's fixed
world-cell size before binding. The alternatives are visual hypotheses, not
assertions that a sprite's official name or intended role matches a class.

The fourth round also asked whether a guarded vault must still contain a
reward. The page imports round-three JSON and exports all four rounds as
`roag-sprite-review/4`.

## Fourth review · 18 October 2026

The downloaded `roag-sprite-feedback-round4.json` has schema
`roag-sprite-review/4`, audit commit `3b58b1b9`, and export time
`2026-10-18T02:54:25.808Z`. Its SHA-256 is
`b1f567adf7feefa755e1c5c7fce1ca85f04ff6de6a92c96b0e8a521e3924ad28`.
It preserves all fourteen original decisions and ten second-round follow-ups
exactly. Eleven third-round records are unchanged; the gate art record now has
the explicit **both** selection, consistent with the third-round comment. All
three fourth-round cards were answered, with no additional conditions.

| Decision | Final selection | Implementation meaning |
| --- | --- | --- |
| Hookrunner actor art | **Masked runner**: Tiny Dungeon #111 and 1-Bit #80 | Add Hookrunner as a third new class alongside Shade and Trapper, using this pair for the two looks. |
| Trapper actor art | **Green scout**: Tiny Dungeon #112 and 1-Bit #77 | Add Trapper using this pair. Keep the snare role distinct from Ranger's bow attacks. |
| Vault outcomes | **Wild card** | Randomly choose among currency + health, clue + currency, a free boon, and an ambush. The ambush may be the only outcome; do not add a guaranteed item after it. The vault remains optional and the key stays on the same floor. |
| Timed-gate art | **Both sets** (corrected control) | RPG #959/#961 plate and #161/#218 gate, plus Tiny #54/#55 plate and #45 door are approved alternatives for mixed rooms. The 1-Bit look remains independent. |

The approved Tiny Dungeon actors are 16px source cells; existing Micro actors
are 8px. Render all actors within the same world-cell footprint and check
silhouette scale during implementation. This is a technical fit check, not a
new approval request. The initial class ranges, durations and reward rates
can be tuned through playtesting without changing the approved concepts.

### Implementation boundary

The first four approved visual changes, mushroom scenery, currency ore, mist
beacon, ward flask, optional variable vault, timed gate, decorative mine rails,
solid pillars, Ring of Reversal, and three new playable classes now have a
decision path. The rejected spore pod, volatile bomb and moving cart remain
out of scope. The review has not itself modified runtime behavior.
