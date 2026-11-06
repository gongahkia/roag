# Ithica expedition: curated runtime bindings

This table includes retained fantasy-preview aliases. Current lunar use is listed separately in [ithica-sprites.md](ithica-sprites.md) and `catalog.json` → `sprites[].ithica`. A preview binding does not imply current lunar use.

Every RPG semantic alias below references an unchanged ID/crop in the source audit. Casting and palette are game decisions, not new author identities. The native viewer additionally registers all 7,115 occupied sprites; registration alone does not make them gameplay bindings.

Edit `assets/ithica-expedition-bindings.json`, then run `python3 tools/build_game_catalog.py`. Inspect crops in [the atlas](../atlas.html) or the native F2 viewer. Original publisher-name evidence remains in [catalog.json.gz](../catalog.json.gz).

**291 semantic aliases, 94 font glyphs, 319 distinct runtime-bound IDs.**

| Alias | Stable ID | Source identity / working label | Evidence | Game interpretation / adaptation |
|---|---|---|---|---|
| abbot | urizen-onebit-r17-c203 | enemy / monk / prayer | author_metadata_visible_rgb_match / high | Author-matched monk/prayer figure cast as the Hollow Abbot boss. |
| animal-bones | urizen-onebit-r38-c003 | bones / animal | author_metadata_visible_rgb_match / high | Render as animal bones; preserve the source identity and keep mechanics in game data. |
| anvil | urizen-onebit-r25-c028 | anvil | author_metadata_visible_rgb_match / high | Render as anvil; preserve the source identity and keep mechanics in game data. |
| apple | urizen-onebit-r17-c003 | food / apple | author_metadata_visible_rgb_match / high | Render as apple; preserve the source identity and keep mechanics in game data. |
| arc | godot-white-thunder | thunder | publisher_filename / high | Render as arc; preserve the source identity and keep mechanics in game data. |
| atlas | godot-white-grid | grid | publisher_filename / high | Render as atlas; preserve the source identity and keep mechanics in game data. |
| bandit | urizen-onebit-r43-c105 | enemy / bandit / male | author_metadata_visible_rgb_match / high | Render as bandit; preserve the source identity and keep mechanics in game data. |
| banner | urizen-onebit-r23-c031 | Awards, accessories, holy symbols and furniture R23 C031 | visual_region_and_silhouette / low | Heraldic/prop silhouette used for the party rally ability; no cloth simulation. |
| barrel | urizen-onebit-r36-c011 | furniture / barrel; index=0 | author_metadata_visible_rgb_match / high | Render as barrel; preserve the source identity and keep mechanics in game data. |
| bat | urizen-onebit-r14-c008 | animal / bat | author_metadata_visible_rgb_match / high | Render as bat; preserve the source identity and keep mechanics in game data. |
| bed | urizen-onebit-r36-c000 | furniture / bed / single | author_metadata_visible_rgb_match / high | Render as bed; preserve the source identity and keep mechanics in game data. |
| bee | urizen-onebit-r15-c003 | animal / bee | author_metadata_visible_rgb_match / high | Render as bee; preserve the source identity and keep mechanics in game data. |
| bones | urizen-onebit-r38-c002 | bones / remains | author_metadata_visible_rgb_match / high | Render as bones; preserve the source identity and keep mechanics in game data. |
| book | urizen-onebit-r37-c000 | book / clear | author_metadata_visible_rgb_match / high | Render as book; preserve the source identity and keep mechanics in game data. |
| bookcase | urizen-onebit-r36-c002 | furniture / bookcase | author_metadata_visible_rgb_match / high | Render as bookcase; preserve the source identity and keep mechanics in game data. |
| boulder | urizen-onebit-r05-c020 | floor / boulders | author_metadata_visible_rgb_match / high | Author-confirmed boulder cluster used as nonblocking scenery, rather than adding an obstacle or mining resource. |
| bow | urizen-onebit-r10-c026 | weapon / bow | author_metadata_visible_rgb_match / high | Author-confirmed bow used for arrow/volley ability icons. |
| brazier | urizen-onebit-r22-c027 | light / brazier | author_metadata_visible_rgb_match / high | Render as brazier; preserve the source identity and keep mechanics in game data. |
| bridge | urizen-onebit-r40-c012 | bridge; orientation=H | author_metadata_visible_rgb_match / high | Render as bridge; preserve the source identity and keep mechanics in game data. |
| broken-column | urizen-onebit-r03-c027 | column / antique / broken | author_metadata_visible_rgb_match / high | Author-confirmed broken antique column used as nonblocking collapsed-room scenery outside the safe cross. |
| button-mushroom | urizen-onebit-r10-c003 | flora / mushroom / button | author_metadata_visible_rgb_match / high | Render as button mushroom; preserve the source identity and keep mechanics in game data. |
| campfire | urizen-onebit-r42-c000 | bonfire | author_metadata_visible_rgb_match / high | Render as campfire; preserve the source identity and keep mechanics in game data. |
| candle | urizen-onebit-r42-c007 | light / candle | author_metadata_visible_rgb_match / high | Render as candle; preserve the source identity and keep mechanics in game data. |
| cave-mouth | urizen-onebit-r03-c030 | cave | author_metadata_visible_rgb_match / high | Author-confirmed cave silhouette used as scenery; it does not create a fourth playable area or transition. |
| cherry | urizen-onebit-r17-c006 | food / cherry | author_metadata_visible_rgb_match / high | Render as cherry; preserve the source identity and keep mechanics in game data. |
| chest | urizen-onebit-r19-c026 | furniture / chest | author_metadata_visible_rgb_match / high | Render as chest; preserve the source identity and keep mechanics in game data. |
| chest-open | urizen-onebit-r19-c027 | furniture / chest / open | author_metadata_visible_rgb_match / high | Render as chest open; preserve the source identity and keep mechanics in game data. |
| coffin | urizen-onebit-r19-c029 | Chests, bows, siege and workshop parts R19 C029 | visual_region_and_silhouette / low | Unconfirmed chest/workshop silhouette used as a coffin-shaped crypt prop; this is not an author label. |
| coin | urizen-onebit-r46-c001 | money / coin | author_metadata_visible_rgb_match / high | Render as coin; preserve the source identity and keep mechanics in game data. |
| column | urizen-onebit-r03-c026 | column / antique | author_metadata_visible_rgb_match / high | Render as column; preserve the source identity and keep mechanics in game data. |
| cracked-wall | urizen-onebit-r02-c001 | Structural tile R02 C001 | visual_region_and_silhouette / low | Visually fractured structural pattern used for collapsed masonry; its exact publisher variant remains unconfirmed. |
| cross | urizen-onebit-r25-c032 | chapel / cross | author_metadata_visible_rgb_match / high | Render as cross; preserve the source identity and keep mechanics in game data. |
| crystal | urizen-onebit-r22-c002 | mineral / crystal; index=0 | author_metadata_visible_rgb_match / high | Author-confirmed diagonal crystal becomes decorative ruin treasure; no mining/crafting/reward interaction is implemented. |
| dead-tree | urizen-onebit-r10-c005 | flora / tree / dead | author_metadata_visible_rgb_match / high | Render as dead tree; preserve the source identity and keep mechanics in game data. |
| egg | urizen-onebit-r17-c008 | food / egg | author_metadata_visible_rgb_match / high | Render as egg; preserve the source identity and keep mechanics in game data. |
| elder | urizen-onebit-r01-c187 | Figure row 01, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as elder; preserve the source identity and keep mechanics in game data. |
| ember | urizen-onebit-r42-c006 | fire | author_metadata_visible_rgb_match / high | Render as ember; preserve the source identity and keep mechanics in game data. |
| exit | godot-white-door | door | publisher_filename / high | Render as exit; preserve the source identity and keep mechanics in game data. |
| fishing-rod | urizen-onebit-r31-c003 | tool / fishingrod | author_metadata_visible_rgb_match / high | Render as fishing rod; preserve the source identity and keep mechanics in game data. |
| flagstone | urizen-onebit-r05-c004 | floor / flagged; index=0 | author_metadata_visible_rgb_match / high | Render as flagstone; preserve the source identity and keep mechanics in game data. |
| flower | urizen-onebit-r10-c001 | flora / flower; index=1 | author_metadata_visible_rgb_match / high | Render as flower; preserve the source identity and keep mechanics in game data. |
| fountain | urizen-onebit-r40-c020 | fountain | author_metadata_visible_rgb_match / high | Render as fountain; preserve the source identity and keep mechanics in game data. |
| gate | urizen-onebit-r01-c012 | door / closed / bars | author_metadata_visible_rgb_match / high | Render as gate; preserve the source identity and keep mechanics in game data. |
| gate-open | urizen-onebit-r01-c000 | door / open | author_metadata_visible_rgb_match / high | Render as gate open; preserve the source identity and keep mechanics in game data. |
| grass | urizen-onebit-r05-c015 | floor / grass; index=0 | author_metadata_visible_rgb_match / high | Render as grass; preserve the source identity and keep mechanics in game data. |
| grass-alt | urizen-onebit-r05-c016 | floor / grass; index=1 | author_metadata_visible_rgb_match / high | Render as grass alt; preserve the source identity and keep mechanics in game data. |
| grave | urizen-onebit-r39-c008 | gravestone / cross | author_metadata_visible_rgb_match / high | Render as grave; preserve the source identity and keep mechanics in game data. |
| grave-obelisk | urizen-onebit-r25-c035 | gravestone / obelisk | author_metadata_visible_rgb_match / high | Render as grave obelisk; preserve the source identity and keep mechanics in game data. |
| grave-slab | urizen-onebit-r39-c007 | gravestone | author_metadata_visible_rgb_match / high | Render as grave slab; preserve the source identity and keep mechanics in game data. |
| grit | urizen-onebit-r05-c017 | floor / grit | author_metadata_visible_rgb_match / high | Author-confirmed grit becomes eroded-room ground and scorched grass presentation. Collision remains explicit Ground data. |
| hazard-corruption | urizen-onebit-r09-c001 | flora / leafs | author_metadata_visible_rgb_match / high | Author's leaf silhouette represents magical blight. Growth, source lifetime and stress costs are game rules. |
| hazard-fire | urizen-onebit-r42-c006 | fire | author_metadata_visible_rgb_match / high | Author's fire crop marks spreading magical fire. Campfire props remain safe rest locations and do not ignite it. |
| hazard-spores | urizen-onebit-r10-c003 | flora / mushroom / button | author_metadata_visible_rgb_match / high | Author's button mushrooms represent a spore cloud/source overlay; poison is game casting, not a publisher property. |
| healer | urizen-onebit-r01-c175 | Figure row 01, profession column 71: cross-shaped prop | visual_region_and_silhouette / low | Cross-shaped held prop; Sable's class is a game interpretation. |
| heart | godot-white-heart | heart | publisher_filename / high | Render as heart; preserve the source identity and keep mechanics in game data. |
| innkeeper | urizen-onebit-r01-c176 | Figure row 01, profession column 72: raised goblet-like prop | visual_region_and_silhouette / low | Render as innkeeper; preserve the source identity and keep mechanics in game data. |
| journal | godot-white-scroll | scroll | publisher_filename / high | Render as journal; preserve the source identity and keep mechanics in game data. |
| key | urizen-onebit-r45-c007 | key | author_metadata_visible_rgb_match / high | Render as key; preserve the source identity and keep mechanics in game data. |
| lantern | urizen-onebit-r42-c005 | light / lantern | author_metadata_visible_rgb_match / high | Render as lantern; preserve the source identity and keep mechanics in game data. |
| leaf | urizen-onebit-r09-c001 | flora / leafs | author_metadata_visible_rgb_match / high | Render as leaf; preserve the source identity and keep mechanics in game data. |
| lever | urizen-onebit-r17-c056 | lever / right | author_metadata_visible_rgb_match / high | Render as lever; preserve the source identity and keep mechanics in game data. |
| mage | urizen-onebit-r00-c187 | Figure row 00, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Staff-like held prop; no walking/casting animation sequence is assumed. |
| mana | urizen-onebit-r04-c032 | potion / mana | author_metadata_visible_rgb_match / high | Render as mana; preserve the source identity and keep mechanics in game data. |
| mark | godot-white-target | target | publisher_filename / high | Render as mark; preserve the source identity and keep mechanics in game data. |
| meat | urizen-onebit-r17-c001 | food / meat | author_metadata_visible_rgb_match / high | Render as meat; preserve the source identity and keep mechanics in game data. |
| merchant | urizen-onebit-r00-c198 | Figure row 00, profession column 94: bag-like hand prop | visual_region_and_silhouette / low | Render as merchant; preserve the source identity and keep mechanics in game data. |
| mushroom | urizen-onebit-r10-c002 | flora / mushroom / toadstool | author_metadata_visible_rgb_match / high | Render as mushroom; preserve the source identity and keep mechanics in game data. |
| path | urizen-onebit-r05-c001 | floor / dirt | author_metadata_visible_rgb_match / high | Render as path; preserve the source identity and keep mechanics in game data. |
| pickaxe | urizen-onebit-r31-c001 | tool / pickaxe | author_metadata_visible_rgb_match / high | Render as pickaxe; preserve the source identity and keep mechanics in game data. |
| pine | urizen-onebit-r08-c003 | flora / tree / spruce | author_metadata_visible_rgb_match / high | Render as pine; preserve the source identity and keep mechanics in game data. |
| poplar | urizen-onebit-r08-c004 | flora / tree / poplar | author_metadata_visible_rgb_match / high | Author's poplar is a collidable/occluding Tree appearance in the cellular forest. |
| potion | urizen-onebit-r04-c030 | potion / health | author_metadata_visible_rgb_match / high | Render as potion; preserve the source identity and keep mechanics in game data. |
| rabbit | urizen-onebit-r16-c003 | animal / rabbit | author_metadata_visible_rgb_match / high | Render as rabbit; preserve the source identity and keep mechanics in game data. |
| ranger | urizen-onebit-r01-c119 | Figure row 01, profession column 15: curved bow-like prop | visual_region_and_silhouette / low | Bow-like held prop; Vesper's role and abilities are game data. |
| ration | urizen-onebit-r17-c000 | food / mushroom | author_metadata_visible_rgb_match / high | Render as ration; preserve the source identity and keep mechanics in game data. |
| reed | urizen-onebit-r09-c006 | flora / cane | author_metadata_visible_rgb_match / high | Render as reed; preserve the source identity and keep mechanics in game data. |
| relic | urizen-onebit-r13-c027 | magic / orb | author_metadata_visible_rgb_match / high | Render as relic; preserve the source identity and keep mechanics in game data. |
| resident | urizen-onebit-r00-c105 | Figure row 00, profession column 01: hammer-like hand tool | visual_region_and_silhouette / low | Render as resident; preserve the source identity and keep mechanics in game data. |
| resolve | godot-white-brain | brain | publisher_filename / high | Render as resolve; preserve the source identity and keep mechanics in game data. |
| roag-boon-aftershock | godot-white-thunder | thunder | publisher_filename / high | Render as roag boon aftershock; preserve the source identity and keep mechanics in game data. |
| roag-boon-alchemical-flip | godot-white-potion | potion | publisher_filename / high | Render as roag boon alchemical flip; preserve the source identity and keep mechanics in game data. |
| roag-boon-anchor-maul | godot-white-hammer | hammer | publisher_filename / high | Render as roag boon anchor maul; preserve the source identity and keep mechanics in game data. |
| roag-boon-backswing | godot-white-sword | sword | publisher_filename / high | Render as roag boon backswing; preserve the source identity and keep mechanics in game data. |
| roag-boon-backtrail-mantle | godot-white-path_follow | path follow | publisher_filename / high | Render as roag boon backtrail mantle; preserve the source identity and keep mechanics in game data. |
| roag-boon-battle-march | godot-white-boots | boots | publisher_filename / high | Render as roag boon battle march; preserve the source identity and keep mechanics in game data. |
| roag-boon-beacon-afterglow | godot-white-flag | flag | publisher_filename / high | Render as roag boon beacon afterglow; preserve the source identity and keep mechanics in game data. |
| roag-boon-beggars-cap | godot-white-hat | hat | publisher_filename / high | Render as roag boon beggars cap; preserve the source identity and keep mechanics in game data. |
| roag-boon-betrayal-ward | godot-white-shield | shield | publisher_filename / high | Render as roag boon betrayal ward; preserve the source identity and keep mechanics in game data. |
| roag-boon-bishops-wake | godot-white-triangle | triangle | publisher_filename / high | Render as roag boon bishops wake; preserve the source identity and keep mechanics in game data. |
| roag-boon-bloodrush | godot-white-heart | heart | publisher_filename / high | Render as roag boon bloodrush; preserve the source identity and keep mechanics in game data. |
| roag-boon-borrowed-guard | godot-white-shield | shield | publisher_filename / high | Render as roag boon borrowed guard; preserve the source identity and keep mechanics in game data. |
| roag-boon-bramble-compact | godot-white-tree | tree | publisher_filename / high | Render as roag boon bramble compact; preserve the source identity and keep mechanics in game data. |
| roag-boon-brokers-seal | godot-white-coin | coin | publisher_filename / high | Render as roag boon brokers seal; preserve the source identity and keep mechanics in game data. |
| roag-boon-cinder-stopper | godot-white-glow | glow | publisher_filename / high | Render as roag boon cinder stopper; preserve the source identity and keep mechanics in game data. |
| roag-boon-circuit-chorus | godot-white-signal | signal | publisher_filename / high | Render as roag boon circuit chorus; preserve the source identity and keep mechanics in game data. |
| roag-boon-cluster-rupture | godot-white-area_damage | area damage | publisher_filename / high | Render as roag boon cluster rupture; preserve the source identity and keep mechanics in game data. |
| roag-boon-counterweight | godot-white-gear | gear | publisher_filename / high | Render as roag boon counterweight; preserve the source identity and keep mechanics in game data. |
| roag-boon-crescent-reach | godot-white-sword | sword | publisher_filename / high | Render as roag boon crescent reach; preserve the source identity and keep mechanics in game data. |
| roag-boon-crossfire-omen | godot-white-target_2 | target 2 | publisher_filename / high | Render as roag boon crossfire omen; preserve the source identity and keep mechanics in game data. |
| roag-boon-crosswind | godot-white-wind | wind | publisher_filename / high | Render as roag boon crosswind; preserve the source identity and keep mechanics in game data. |
| roag-boon-crowdbreaker | godot-white-hammer | hammer | publisher_filename / high | Render as roag boon crowdbreaker; preserve the source identity and keep mechanics in game data. |
| roag-boon-crownfire | godot-white-glow | glow | publisher_filename / high | Render as roag boon crownfire; preserve the source identity and keep mechanics in game data. |
| roag-boon-crystal-harmony | godot-white-gem | gem | publisher_filename / high | Render as roag boon crystal harmony; preserve the source identity and keep mechanics in game data. |
| roag-boon-detonation-bloom | godot-white-area_damage | area damage | publisher_filename / high | Render as roag boon detonation bloom; preserve the source identity and keep mechanics in game data. |
| roag-boon-distant-bastion | godot-white-tower | tower | publisher_filename / high | Render as roag boon distant bastion; preserve the source identity and keep mechanics in game data. |
| roag-boon-doorward-lamp | godot-white-door | door | publisher_filename / high | Render as roag boon doorward lamp; preserve the source identity and keep mechanics in game data. |
| roag-boon-earthshaker | godot-white-hammer | hammer | publisher_filename / high | Render as roag boon earthshaker; preserve the source identity and keep mechanics in game data. |
| roag-boon-emberbrand | godot-white-glow | glow | publisher_filename / high | Render as roag boon emberbrand; preserve the source identity and keep mechanics in game data. |
| roag-boon-executioners-thread | godot-white-skull | skull | publisher_filename / high | Render as roag boon executioners thread; preserve the source identity and keep mechanics in game data. |
| roag-boon-expanding-sigil | godot-white-expand | expand | publisher_filename / high | Render as roag boon expanding sigil; preserve the source identity and keep mechanics in game data. |
| roag-boon-fan-of-blades | godot-white-sword | sword | publisher_filename / high | Render as roag boon fan of blades; preserve the source identity and keep mechanics in game data. |
| roag-boon-far-witness | godot-white-visibility | visibility | publisher_filename / high | Render as roag boon far witness; preserve the source identity and keep mechanics in game data. |
| roag-boon-fateful-dice | godot-white-dice | dice | publisher_filename / high | Render as roag boon fateful dice; preserve the source identity and keep mechanics in game data. |
| roag-boon-field-dressing | godot-white-heart | heart | publisher_filename / high | Render as roag boon field dressing; preserve the source identity and keep mechanics in game data. |
| roag-boon-final-spark | godot-white-star | star | publisher_filename / high | Render as roag boon final spark; preserve the source identity and keep mechanics in game data. |
| roag-boon-flashpoint | godot-white-particle | particle | publisher_filename / high | Render as roag boon flashpoint; preserve the source identity and keep mechanics in game data. |
| roag-boon-fourfold-ledger | godot-white-grid | grid | publisher_filename / high | Render as roag boon fourfold ledger; preserve the source identity and keep mechanics in game data. |
| roag-boon-fracture-dividend | godot-white-hammer | hammer | publisher_filename / high | Render as roag boon fracture dividend; preserve the source identity and keep mechanics in game data. |
| roag-boon-froststep-catalyst | godot-white-freeze | freeze | publisher_filename / high | Render as roag boon froststep catalyst; preserve the source identity and keep mechanics in game data. |
| roag-boon-funeral-coin | godot-white-skull | skull | publisher_filename / high | Render as roag boon funeral coin; preserve the source identity and keep mechanics in game data. |
| roag-boon-grasping-hand | godot-white-hand | hand | publisher_filename / high | Render as roag boon grasping hand; preserve the source identity and keep mechanics in game data. |
| roag-boon-grave-dividend | godot-white-skull | skull | publisher_filename / high | Render as roag boon grave dividend; preserve the source identity and keep mechanics in game data. |
| roag-boon-grounding-rod | godot-white-thunder | thunder | publisher_filename / high | Render as roag boon grounding rod; preserve the source identity and keep mechanics in game data. |
| roag-boon-guardians-vow | godot-white-shield | shield | publisher_filename / high | Render as roag boon guardians vow; preserve the source identity and keep mechanics in game data. |
| roag-boon-harriers-response | godot-white-follow | follow | publisher_filename / high | Render as roag boon harriers response; preserve the source identity and keep mechanics in game data. |
| roag-boon-hollow-crown | godot-white-trophy | trophy | publisher_filename / high | Render as roag boon hollow crown; preserve the source identity and keep mechanics in game data. |
| roag-boon-hollow-sigil | godot-white-circle | circle | publisher_filename / high | Render as roag boon hollow sigil; preserve the source identity and keep mechanics in game data. |
| roag-boon-hourglass-clasp | godot-white-time | time | publisher_filename / high | Render as roag boon hourglass clasp; preserve the source identity and keep mechanics in game data. |
| roag-boon-knights-gambit | godot-white-follower | follower | publisher_filename / high | Render as roag boon knights gambit; preserve the source identity and keep mechanics in game data. |
| roag-boon-linebreaker-pawn | godot-white-character | character | publisher_filename / high | Render as roag boon linebreaker pawn; preserve the source identity and keep mechanics in game data. |
| roag-boon-long-wick | godot-white-light_bulb | light bulb | publisher_filename / high | Render as roag boon long wick; preserve the source identity and keep mechanics in game data. |
| roag-boon-longblade-geometry | godot-white-expand | expand | publisher_filename / high | Render as roag boon longblade geometry; preserve the source identity and keep mechanics in game data. |
| roag-boon-mirror-dividend | godot-white-mirror | mirror | publisher_filename / high | Render as roag boon mirror dividend; preserve the source identity and keep mechanics in game data. |
| roag-boon-mist-lullaby | godot-white-audio | audio | publisher_filename / high | Render as roag boon mist lullaby; preserve the source identity and keep mechanics in game data. |
| roag-boon-mist-veil | godot-white-visibility_off | visibility off | publisher_filename / high | Render as roag boon mist veil; preserve the source identity and keep mechanics in game data. |
| roag-boon-needle-flight | godot-white-projectile | projectile | publisher_filename / high | Render as roag boon needle flight; preserve the source identity and keep mechanics in game data. |
| roag-boon-opening-cut | godot-white-sword | sword | publisher_filename / high | Render as roag boon opening cut; preserve the source identity and keep mechanics in game data. |
| roag-boon-ordered-ranks | godot-white-grid | grid | publisher_filename / high | Render as roag boon ordered ranks; preserve the source identity and keep mechanics in game data. |
| roag-boon-overflow-chalice | godot-white-receptacle | receptacle | publisher_filename / high | Render as roag boon overflow chalice; preserve the source identity and keep mechanics in game data. |
| roag-boon-packed-powder | godot-white-canon | canon | publisher_filename / high | Render as roag boon packed powder; preserve the source identity and keep mechanics in game data. |
| roag-boon-pain-dividend | godot-white-hitbox | hitbox | publisher_filename / high | Render as roag boon pain dividend; preserve the source identity and keep mechanics in game data. |
| roag-boon-passing-ember | godot-white-glow | glow | publisher_filename / high | Render as roag boon passing ember; preserve the source identity and keep mechanics in game data. |
| roag-boon-patient-rebuke | godot-white-paused | paused | publisher_filename / high | Render as roag boon patient rebuke; preserve the source identity and keep mechanics in game data. |
| roag-boon-perfect-guard | godot-white-shield | shield | publisher_filename / high | Render as roag boon perfect guard; preserve the source identity and keep mechanics in game data. |
| roag-boon-phantom-lunge | godot-white-visibility_off | visibility off | publisher_filename / high | Render as roag boon phantom lunge; preserve the source identity and keep mechanics in game data. |
| roag-boon-pilgrims-vow | godot-white-parchment | parchment | publisher_filename / high | Render as roag boon pilgrims vow; preserve the source identity and keep mechanics in game data. |
| roag-boon-pommel-return | godot-white-reset | reset | publisher_filename / high | Render as roag boon pommel return; preserve the source identity and keep mechanics in game data. |
| roag-boon-prospectors-heart | godot-white-gem | gem | publisher_filename / high | Render as roag boon prospectors heart; preserve the source identity and keep mechanics in game data. |
| roag-boon-pulse-step | godot-white-boots | boots | publisher_filename / high | Render as roag boon pulse step; preserve the source identity and keep mechanics in game data. |
| roag-boon-purging-seal | godot-white-clear | clear | publisher_filename / high | Render as roag boon purging seal; preserve the source identity and keep mechanics in game data. |
| roag-boon-pursuit-spark | godot-white-follow | follow | publisher_filename / high | Render as roag boon pursuit spark; preserve the source identity and keep mechanics in game data. |
| roag-boon-ration-reserve | godot-white-meat | meat | publisher_filename / high | Render as roag boon ration reserve; preserve the source identity and keep mechanics in game data. |
| roag-boon-recurve | godot-white-reset_2 | reset 2 | publisher_filename / high | Render as roag boon recurve; preserve the source identity and keep mechanics in game data. |
| roag-boon-reprisal-nova | godot-white-star | star | publisher_filename / high | Render as roag boon reprisal nova; preserve the source identity and keep mechanics in game data. |
| roag-boon-resonant-wounds | godot-white-heart | heart | publisher_filename / high | Render as roag boon resonant wounds; preserve the source identity and keep mechanics in game data. |
| roag-boon-rhythm-of-steel | godot-white-audio | audio | publisher_filename / high | Render as roag boon rhythm of steel; preserve the source identity and keep mechanics in game data. |
| roag-boon-rime-disk | godot-white-freeze | freeze | publisher_filename / high | Render as roag boon rime disk; preserve the source identity and keep mechanics in game data. |
| roag-boon-ring-of-reversal | godot-white-ring | ring | publisher_filename / high | Render as roag boon ring of reversal; preserve the source identity and keep mechanics in game data. |
| roag-boon-road-flask | godot-white-potion | potion | publisher_filename / high | Render as roag boon road flask; preserve the source identity and keep mechanics in game data. |
| roag-boon-rooks-wake | godot-white-tower | tower | publisher_filename / high | Render as roag boon rooks wake; preserve the source identity and keep mechanics in game data. |
| roag-boon-scavengers-hook | godot-white-drag | drag | publisher_filename / high | Render as roag boon scavengers hook; preserve the source identity and keep mechanics in game data. |
| roag-boon-secret-apple | urizen-onebit-r17-c003 | food / apple | author_metadata_visible_rgb_match / high | Render as roag boon secret apple; preserve the source identity and keep mechanics in game data. |
| roag-boon-sentry-quartermaster | godot-white-robot | robot | publisher_filename / high | Render as roag boon sentry quartermaster; preserve the source identity and keep mechanics in game data. |
| roag-boon-shared-agony | godot-white-duplicate | duplicate | publisher_filename / high | Render as roag boon shared agony; preserve the source identity and keep mechanics in game data. |
| roag-boon-spirit-shelter | godot-white-human_controller | human controller | publisher_filename / high | Render as roag boon spirit shelter; preserve the source identity and keep mechanics in game data. |
| roag-boon-split-thrust | godot-white-duplicate | duplicate | publisher_filename / high | Render as roag boon split thrust; preserve the source identity and keep mechanics in game data. |
| roag-boon-splitflight | godot-white-projectile | projectile | publisher_filename / high | Render as roag boon splitflight; preserve the source identity and keep mechanics in game data. |
| roag-boon-spoils-aegis | godot-white-shield | shield | publisher_filename / high | Render as roag boon spoils aegis; preserve the source identity and keep mechanics in game data. |
| roag-boon-static-footsteps | godot-white-boots | boots | publisher_filename / high | Render as roag boon static footsteps; preserve the source identity and keep mechanics in game data. |
| roag-boon-stone-backstop | godot-white-wall | wall | publisher_filename / high | Render as roag boon stone backstop; preserve the source identity and keep mechanics in game data. |
| roag-boon-stonewake | godot-white-shovel | shovel | publisher_filename / high | Render as roag boon stonewake; preserve the source identity and keep mechanics in game data. |
| roag-boon-storm-conductor | godot-white-thunder | thunder | publisher_filename / high | Render as roag boon storm conductor; preserve the source identity and keep mechanics in game data. |
| roag-boon-sunderers-mark | godot-white-target | target | publisher_filename / high | Render as roag boon sunderers mark; preserve the source identity and keep mechanics in game data. |
| roag-boon-temporal-echo | godot-white-time | time | publisher_filename / high | Render as roag boon temporal echo; preserve the source identity and keep mechanics in game data. |
| roag-boon-thorn-mirror | godot-white-mirror | mirror | publisher_filename / high | Render as roag boon thorn mirror; preserve the source identity and keep mechanics in game data. |
| roag-boon-threefold-remedy | godot-white-potion | potion | publisher_filename / high | Render as roag boon threefold remedy; preserve the source identity and keep mechanics in game data. |
| roag-boon-turncoat-spark | godot-white-reset | reset | publisher_filename / high | Render as roag boon turncoat spark; preserve the source identity and keep mechanics in game data. |
| roag-boon-twin-seals | godot-white-duplicate | duplicate | publisher_filename / high | Render as roag boon twin seals; preserve the source identity and keep mechanics in game data. |
| roag-boon-vent-mantle | godot-white-wind | wind | publisher_filename / high | Render as roag boon vent mantle; preserve the source identity and keep mechanics in game data. |
| roag-boon-volatile-vial | godot-white-potion | potion | publisher_filename / high | Render as roag boon volatile vial; preserve the source identity and keep mechanics in game data. |
| roag-boon-ward-ledger | urizen-onebit-r37-c000 | book / clear | author_metadata_visible_rgb_match / high | Render as roag boon ward ledger; preserve the source identity and keep mechanics in game data. |
| roag-boon-windup-hunter | godot-white-time | time | publisher_filename / high | Render as roag boon windup hunter; preserve the source identity and keep mechanics in game data. |
| roag-boss-resonator | urizen-onebit-r25-c036 | bell | author_metadata_visible_rgb_match / high | Render as roag boss resonator; preserve the source identity and keep mechanics in game data. |
| roag-boss-valve | urizen-onebit-r00-c068 | door / valve / metal | author_metadata_visible_rgb_match / high | Render as roag boss valve; preserve the source identity and keep mechanics in game data. |
| roag-boss-web | urizen-onebit-r39-c004 | web | author_metadata_visible_rgb_match / high | Render as roag boss web; preserve the source identity and keep mechanics in game data. |
| roag-class-adventurer | urizen-onebit-r00-c107 | Figure row 00, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag class adventurer; preserve the source identity and keep mechanics in game data. |
| roag-class-alchemist | urizen-onebit-r09-c112 | Figure row 09, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag class alchemist; preserve the source identity and keep mechanics in game data. |
| roag-class-arcanist | urizen-onebit-r00-c187 | Figure row 00, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag class arcanist; preserve the source identity and keep mechanics in game data. |
| roag-class-berserker | urizen-onebit-r03-c180 | Figure row 03, profession column 76: broad axe-like tool | visual_region_and_silhouette / low | Render as roag class berserker; preserve the source identity and keep mechanics in game data. |
| roag-class-duelist | urizen-onebit-r12-c192 | Figure row 12, profession column 88: diagonal blade-like tool | visual_region_and_silhouette / low | Render as roag class duelist; preserve the source identity and keep mechanics in game data. |
| roag-class-geomancer | urizen-onebit-r19-c174 | Figure row 19, profession column 70: large hammer-like tool | visual_region_and_silhouette / low | Render as roag class geomancer; preserve the source identity and keep mechanics in game data. |
| roag-class-hookrunner | urizen-onebit-r02-c196 | Figure row 02, profession column 92: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag class hookrunner; preserve the source identity and keep mechanics in game data. |
| roag-class-ranger | urizen-onebit-r01-c119 | Figure row 01, profession column 15: curved bow-like prop | visual_region_and_silhouette / low | Render as roag class ranger; preserve the source identity and keep mechanics in game data. |
| roag-class-sentinel | urizen-onebit-r00-c113 | Figure row 00, profession column 09: bucket or rectangular shield-like prop | visual_region_and_silhouette / low | Render as roag class sentinel; preserve the source identity and keep mechanics in game data. |
| roag-class-shade | urizen-onebit-r13-c107 | Figure row 13, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag class shade; preserve the source identity and keep mechanics in game data. |
| roag-class-spiritbinder | urizen-onebit-r17-c169 | Figure row 17, profession column 65: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag class spiritbinder; preserve the source identity and keep mechanics in game data. |
| roag-class-stormcaller | urizen-onebit-r07-c187 | Figure row 07, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag class stormcaller; preserve the source identity and keep mechanics in game data. |
| roag-class-tinkerer | urizen-onebit-r02-c105 | Figure row 02, profession column 01: hammer-like hand tool | visual_region_and_silhouette / low | Render as roag class tinkerer; preserve the source identity and keep mechanics in game data. |
| roag-class-trapper | urizen-onebit-r15-c119 | Figure row 15, profession column 15: curved bow-like prop | visual_region_and_silhouette / low | Render as roag class trapper; preserve the source identity and keep mechanics in game data. |
| roag-class-wayfinder | urizen-onebit-r21-c108 | Figure row 21, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag class wayfinder; preserve the source identity and keep mechanics in game data. |
| roag-enemy-alarm-keeper | urizen-onebit-r23-c175 | Figure row 23, profession column 71: cross-shaped prop | visual_region_and_silhouette / low | Render as roag enemy alarm keeper; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ash-marshal | urizen-onebit-r16-c192 | Figure row 16, profession column 88: diagonal blade-like tool | visual_region_and_silhouette / low | Render as roag enemy ash marshal; preserve the source identity and keep mechanics in game data. |
| roag-enemy-banner-captain | urizen-onebit-r06-c173 | Figure row 06, profession column 69: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy banner captain; preserve the source identity and keep mechanics in game data. |
| roag-enemy-banner-follower | urizen-onebit-r04-c107 | Figure row 04, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag enemy banner follower; preserve the source identity and keep mechanics in game data. |
| roag-enemy-bastion-bulwark | urizen-onebit-r19-c113 | Figure row 19, profession column 09: bucket or rectangular shield-like prop | visual_region_and_silhouette / low | Render as roag enemy bastion bulwark; preserve the source identity and keep mechanics in game data. |
| roag-enemy-bellkeeper | urizen-onebit-r42-c110 | enemy / ghost | author_metadata_visible_rgb_match / high | Render as roag enemy bellkeeper; preserve the source identity and keep mechanics in game data. |
| roag-enemy-bodyguard-brute | urizen-onebit-r30-c174 | Figure row 30, profession column 70: large hammer-like tool | visual_region_and_silhouette / low | Render as roag enemy bodyguard brute; preserve the source identity and keep mechanics in game data. |
| roag-enemy-bone-fragment | urizen-onebit-r38-c002 | bones / remains | author_metadata_visible_rgb_match / high | Render as roag enemy bone fragment; preserve the source identity and keep mechanics in game data. |
| roag-enemy-bone-splitter | urizen-onebit-r10-c104 | Figure row 10, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy bone splitter; preserve the source identity and keep mechanics in game data. |
| roag-enemy-canal-lancer | urizen-onebit-r11-c108 | Figure row 11, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag enemy canal lancer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-canal-lockmaster | urizen-onebit-r29-c108 | Figure row 29, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag enemy canal lockmaster; preserve the source identity and keep mechanics in game data. |
| roag-enemy-cinder-bellkeeper | urizen-onebit-r21-c175 | Figure row 21, profession column 71: cross-shaped prop | visual_region_and_silhouette / low | Render as roag enemy cinder bellkeeper; preserve the source identity and keep mechanics in game data. |
| roag-enemy-cinder-mender | urizen-onebit-r17-c112 | Figure row 17, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag enemy cinder mender; preserve the source identity and keep mechanics in game data. |
| roag-enemy-crystal-bombardier | urizen-onebit-r23-c112 | Figure row 23, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag enemy crystal bombardier; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ember-crucible | urizen-onebit-r25-c174 | Figure row 25, profession column 70: large hammer-like tool | visual_region_and_silhouette / low | Render as roag enemy ember crucible; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ember-moth | urizen-onebit-r15-c003 | animal / bee | author_metadata_visible_rgb_match / high | Render as roag enemy ember moth; preserve the source identity and keep mechanics in game data. |
| roag-enemy-extraction-pursuer | urizen-onebit-r35-c104 | Figure row 35, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy extraction pursuer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-forge-conjurer | urizen-onebit-r16-c187 | Figure row 16, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag enemy forge conjurer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-glass-charger | urizen-onebit-r22-c108 | Figure row 22, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag enemy glass charger; preserve the source identity and keep mechanics in game data. |
| roag-enemy-gloom-strider | urizen-onebit-r07-c107 | Figure row 07, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag enemy gloom strider; preserve the source identity and keep mechanics in game data. |
| roag-enemy-grave-cantor | urizen-onebit-r10-c169 | Figure row 10, profession column 65: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy grave cantor; preserve the source identity and keep mechanics in game data. |
| roag-enemy-gulp-toad | urizen-onebit-r31-c104 | Figure row 31, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy gulp toad; preserve the source identity and keep mechanics in game data. |
| roag-enemy-haunted-cache | urizen-onebit-r19-c026 | furniture / chest | author_metadata_visible_rgb_match / high | Render as roag enemy haunted cache; preserve the source identity and keep mechanics in game data. |
| roag-enemy-heavy-rammer | urizen-onebit-r30-c104 | Figure row 30, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy heavy rammer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-hollow-regent | urizen-onebit-r28-c169 | Figure row 28, profession column 65: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy hollow regent; preserve the source identity and keep mechanics in game data. |
| roag-enemy-hollow-wraith | urizen-onebit-r26-c104 | Figure row 26, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy hollow wraith; preserve the source identity and keep mechanics in game data. |
| roag-enemy-index-keeper | urizen-onebit-r27-c177 | Figure row 27, profession column 73: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy index keeper; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mend-acolyte | urizen-onebit-r21-c112 | Figure row 21, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag enemy mend acolyte; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mire-binder | urizen-onebit-r33-c177 | Figure row 33, profession column 73: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy mire binder; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mirror-hexer | urizen-onebit-r17-c177 | Figure row 17, profession column 73: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy mirror hexer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mossbound-guard | urizen-onebit-r06-c113 | Figure row 06, profession column 09: bucket or rectangular shield-like prop | visual_region_and_silhouette / low | Render as roag enemy mossbound guard; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mossbreaker | urizen-onebit-r29-c174 | Figure row 29, profession column 70: large hammer-like tool | visual_region_and_silhouette / low | Render as roag enemy mossbreaker; preserve the source identity and keep mechanics in game data. |
| roag-enemy-mycelial-matriarch | urizen-onebit-r18-c187 | Figure row 18, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag enemy mycelial matriarch; preserve the source identity and keep mechanics in game data. |
| roag-enemy-oath-twin-a | urizen-onebit-r20-c108 | Figure row 20, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag enemy oath twin a; preserve the source identity and keep mechanics in game data. |
| roag-enemy-oath-twin-b | urizen-onebit-r20-c107 | Figure row 20, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag enemy oath twin b; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ossuary-captain | urizen-onebit-r10-c104 | Figure row 10, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy ossuary captain; preserve the source identity and keep mechanics in game data. |
| roag-enemy-pack-chorister | urizen-onebit-r08-c175 | Figure row 08, profession column 71: cross-shaped prop | visual_region_and_silhouette / low | Render as roag enemy pack chorister; preserve the source identity and keep mechanics in game data. |
| roag-enemy-pin-archer | urizen-onebit-r04-c119 | Figure row 04, profession column 15: curved bow-like prop | visual_region_and_silhouette / low | Render as roag enemy pin archer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-prism-curator | urizen-onebit-r24-c187 | Figure row 24, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag enemy prism curator; preserve the source identity and keep mechanics in game data. |
| roag-enemy-rotating-sentinel | urizen-onebit-r19-c192 | Figure row 19, profession column 88: diagonal blade-like tool | visual_region_and_silhouette / low | Render as roag enemy rotating sentinel; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ruin-skitter | urizen-onebit-r15-c002 | animal / spider | author_metadata_visible_rgb_match / high | Render as roag enemy ruin skitter; preserve the source identity and keep mechanics in game data. |
| roag-enemy-rushwing | urizen-onebit-r14-c008 | animal / bat | author_metadata_visible_rgb_match / high | Render as roag enemy rushwing; preserve the source identity and keep mechanics in game data. |
| roag-enemy-shield-bearer | urizen-onebit-r05-c113 | Figure row 05, profession column 09: bucket or rectangular shield-like prop | visual_region_and_silhouette / low | Render as roag enemy shield bearer; preserve the source identity and keep mechanics in game data. |
| roag-enemy-siege-wisp | urizen-onebit-r26-c112 | Figure row 26, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag enemy siege wisp; preserve the source identity and keep mechanics in game data. |
| roag-enemy-silt-worm | urizen-onebit-r34-c104 | Figure row 34, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy silt worm; preserve the source identity and keep mechanics in game data. |
| roag-enemy-sluice-maw | urizen-onebit-r42-c108 | enemy / slime | author_metadata_visible_rgb_match / high | Render as roag enemy sluice maw; preserve the source identity and keep mechanics in game data. |
| roag-enemy-snare-scribe | urizen-onebit-r27-c112 | Figure row 27, profession column 08: small vessel | visual_region_and_silhouette / low | Render as roag enemy snare scribe; preserve the source identity and keep mechanics in game data. |
| roag-enemy-spore-herald | urizen-onebit-r18-c108 | Figure row 18, profession column 04: long pole-like tool | visual_region_and_silhouette / low | Render as roag enemy spore herald; preserve the source identity and keep mechanics in game data. |
| roag-enemy-thorn-warden | urizen-onebit-r25-c187 | Figure row 25, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag enemy thorn warden; preserve the source identity and keep mechanics in game data. |
| roag-enemy-thornspitter | urizen-onebit-r32-c104 | Figure row 32, profession column 00: empty hands / neutral stance | visual_region_and_silhouette / low | Render as roag enemy thornspitter; preserve the source identity and keep mechanics in game data. |
| roag-enemy-threadmother | urizen-onebit-r15-c002 | animal / spider | author_metadata_visible_rgb_match / high | Render as roag enemy threadmother; preserve the source identity and keep mechanics in game data. |
| roag-enemy-vault-lurker | urizen-onebit-r14-c107 | Figure row 14, profession column 03: short blade-like tool | visual_region_and_silhouette / low | Render as roag enemy vault lurker; preserve the source identity and keep mechanics in game data. |
| roag-enemy-ward-weaver | urizen-onebit-r17-c187 | Figure row 17, profession column 83: raised staff-like prop | visual_region_and_silhouette / low | Render as roag enemy ward weaver; preserve the source identity and keep mechanics in game data. |
| roag-enemy-web-weaver | urizen-onebit-r11-c169 | Figure row 11, profession column 65: unidentified held prop / stance | visual_region_and_silhouette / low | Render as roag enemy web weaver; preserve the source identity and keep mechanics in game data. |
| roag-room-mine-rail | urizen-onebit-r02-c052 | railway; orientation=H | author_metadata_visible_rgb_match / high | Render as roag room mine rail; preserve the source identity and keep mechanics in game data. |
| roag-room-pillar | urizen-onebit-r03-c026 | column / antique | author_metadata_visible_rgb_match / high | Render as roag room pillar; preserve the source identity and keep mechanics in game data. |
| roag-service-bell | urizen-onebit-r25-c036 | bell | author_metadata_visible_rgb_match / high | Render as roag service bell; preserve the source identity and keep mechanics in game data. |
| rock-floor | urizen-onebit-r05-c009 | floor / rocks | author_metadata_visible_rgb_match / high | Render as rock floor; preserve the source identity and keep mechanics in game data. |
| ruin-wall | urizen-onebit-r02-c000 | wall / stone | author_metadata_visible_rgb_match / high | Author-confirmed stone wall used as masonry for generated room boundaries; sockets belong to the 7x7 room. |
| save | godot-white-save | save | publisher_filename / high | Render as save; preserve the source identity and keep mechanics in game data. |
| shield | urizen-onebit-r12-c026 | shield | author_metadata_visible_rgb_match / high | Render as shield; preserve the source identity and keep mechanics in game data. |
| shrine | urizen-onebit-r24-c027 | furniture / statue | author_metadata_visible_rgb_match / high | Render as shrine; preserve the source identity and keep mechanics in game data. |
| shrub | urizen-onebit-r10-c004 | flora / shrub | author_metadata_visible_rgb_match / high | Render as shrub; preserve the source identity and keep mechanics in game data. |
| sign | urizen-onebit-r37-c001 | book | author_metadata_visible_rgb_match / high | Render as sign; preserve the source identity and keep mechanics in game data. |
| skeleton | urizen-onebit-r02-c125 | Figure row 02, profession column 21: raised blade-like tool | visual_region_and_silhouette / low | Gray figure variant cast as an oathless guard; source species remains unconfirmed. |
| skull | urizen-onebit-r38-c001 | bones / skull | author_metadata_visible_rgb_match / high | Render as skull; preserve the source identity and keep mechanics in game data. |
| slash | urizen-onebit-r25-c002 | effect / cleave | author_metadata_visible_rgb_match / high | Render as slash; preserve the source identity and keep mechanics in game data. |
| slime | urizen-onebit-r42-c108 | enemy / slime | author_metadata_visible_rgb_match / high | Render as slime; preserve the source identity and keep mechanics in game data. |
| smith | urizen-onebit-r00-c174 | Figure row 00, profession column 70: large hammer-like tool | visual_region_and_silhouette / low | Render as smith; preserve the source identity and keep mechanics in game data. |
| spark | urizen-onebit-r13-c030 | magic / sparkles | author_metadata_visible_rgb_match / high | Render as spark; preserve the source identity and keep mechanics in game data. |
| spider | urizen-onebit-r15-c002 | animal / spider | author_metadata_visible_rgb_match / high | Render as spider; preserve the source identity and keep mechanics in game data. |
| staff | urizen-onebit-r25-c039 | skullcane | author_metadata_visible_rgb_match / high | Author's skullcane used for the wand/arcane ability icon. |
| stairs-down | urizen-onebit-r00-c023 | stairs / down | author_metadata_visible_rgb_match / high | Render as stairs down; preserve the source identity and keep mechanics in game data. |
| stairs-up | urizen-onebit-r00-c024 | stairs / up | author_metadata_visible_rgb_match / high | Render as stairs up; preserve the source identity and keep mechanics in game data. |
| stone | urizen-onebit-r05-c010 | floor / cobblestone; index=0 | author_metadata_visible_rgb_match / high | Render as stone; preserve the source identity and keep mechanics in game data. |
| stone-alt | urizen-onebit-r05-c005 | floor / flagged; index=1 | author_metadata_visible_rgb_match / high | Render as stone alt; preserve the source identity and keep mechanics in game data. |
| sword | urizen-onebit-r06-c030 | weapon / sword; index=0 | author_metadata_visible_rgb_match / high | Render as sword; preserve the source identity and keep mechanics in game data. |
| table | urizen-onebit-r36-c019 | furniture / table | author_metadata_visible_rgb_match / high | Render as table; preserve the source identity and keep mechanics in game data. |
| trap | urizen-onebit-r21-c026 | trap | author_metadata_visible_rgb_match / high | Render as trap; preserve the source identity and keep mechanics in game data. |
| tree | urizen-onebit-r08-c001 | flora / tree / oak | author_metadata_visible_rgb_match / high | Render as tree; preserve the source identity and keep mechanics in game data. |
| tuft | urizen-onebit-r09-c004 | flora / grass; index=1 | author_metadata_visible_rgb_match / high | Render as tuft; preserve the source identity and keep mechanics in game data. |
| urn | urizen-onebit-r47-c003 | urn | author_metadata_visible_rgb_match / high | Render as urn; preserve the source identity and keep mechanics in game data. |
| wall | urizen-onebit-r02-c002 | wall / stone / window | author_metadata_visible_rgb_match / high | Author's wall motif; repeated full cells rather than an inferred autotile bitmask. |
| warden | urizen-onebit-r00-c125 | Figure row 00, profession column 21: raised blade-like tool | visual_region_and_silhouette / low | Raised blade-like prop; Rowan's role is casting, not a confirmed publisher profession. |
| water | urizen-onebit-r12-c004 | river; orientation=RD | author_metadata_visible_rgb_match / high | A source water motif repeated over blue backing; collision is explicit map data. |
| web | urizen-onebit-r39-c004 | web | author_metadata_visible_rgb_match / high | Render as web; preserve the source identity and keep mechanics in game data. |
| well | urizen-onebit-r25-c031 | well | author_metadata_visible_rgb_match / high | Render as well; preserve the source identity and keep mechanics in game data. |
| willow | urizen-onebit-r08-c002 | flora / tree / willow | author_metadata_visible_rgb_match / high | Author's willow is a collidable/occluding Tree appearance; changing the crop does not change tree rules. |
| wisp | urizen-onebit-r42-c110 | enemy / ghost | author_metadata_visible_rgb_match / high | Render as wisp; preserve the source identity and keep mechanics in game data. |
| wolf | urizen-onebit-r14-c000 | animal / dog | author_metadata_visible_rgb_match / high | The author's dog becomes a Briar hound; enemy name/faction are game data. |
| wood | urizen-onebit-r05-c003 | floor / plank | author_metadata_visible_rgb_match / high | Render as wood; preserve the source identity and keep mechanics in game data. |

## Shared bitmap font

The RPG reuses the colony's exact default-font map. Space is an advance without a crop. Gothic/unknown glyphs remain available for inspection but are not substituted automatically.

| Character | Stable ID |
|---|---|
| '!' | urizen-onebit-r46-c101 |
| '"' | urizen-onebit-r47-c085 |
| '#' | urizen-onebit-r47-c079 |
| '$' | urizen-onebit-r47-c084 |
| '%' | urizen-onebit-r47-c082 |
| '&' | urizen-onebit-r47-c087 |
| "'" | urizen-onebit-r47-c086 |
| '(' | urizen-onebit-r46-c090 |
| ')' | urizen-onebit-r46-c091 |
| '*' | urizen-onebit-r47-c088 |
| '+' | urizen-onebit-r46-c098 |
| ',' | urizen-onebit-r47-c095 |
| '-' | urizen-onebit-r46-c099 |
| '.' | urizen-onebit-r47-c094 |
| '/' | urizen-onebit-r47-c092 |
| '0' | urizen-onebit-r45-c102 |
| '1' | urizen-onebit-r44-c098 |
| '2' | urizen-onebit-r44-c099 |
| '3' | urizen-onebit-r44-c100 |
| '4' | urizen-onebit-r44-c101 |
| '5' | urizen-onebit-r44-c102 |
| '6' | urizen-onebit-r45-c098 |
| '7' | urizen-onebit-r45-c099 |
| '8' | urizen-onebit-r45-c100 |
| '9' | urizen-onebit-r45-c101 |
| ':' | urizen-onebit-r47-c078 |
| ';' | urizen-onebit-r47-c096 |
| '<' | urizen-onebit-r46-c096 |
| '=' | urizen-onebit-r47-c089 |
| '>' | urizen-onebit-r46-c097 |
| '?' | urizen-onebit-r46-c100 |
| '@' | urizen-onebit-r47-c081 |
| 'A' | urizen-onebit-r44-c078 |
| 'B' | urizen-onebit-r44-c079 |
| 'C' | urizen-onebit-r44-c080 |
| 'D' | urizen-onebit-r44-c081 |
| 'E' | urizen-onebit-r44-c082 |
| 'F' | urizen-onebit-r44-c083 |
| 'G' | urizen-onebit-r44-c084 |
| 'H' | urizen-onebit-r44-c085 |
| 'I' | urizen-onebit-r44-c086 |
| 'J' | urizen-onebit-r44-c087 |
| 'K' | urizen-onebit-r44-c088 |
| 'L' | urizen-onebit-r44-c089 |
| 'M' | urizen-onebit-r44-c090 |
| 'N' | urizen-onebit-r44-c091 |
| 'O' | urizen-onebit-r44-c092 |
| 'P' | urizen-onebit-r44-c093 |
| 'Q' | urizen-onebit-r44-c094 |
| 'R' | urizen-onebit-r44-c095 |
| 'S' | urizen-onebit-r44-c096 |
| 'T' | urizen-onebit-r44-c097 |
| 'U' | urizen-onebit-r45-c078 |
| 'V' | urizen-onebit-r45-c079 |
| 'W' | urizen-onebit-r45-c080 |
| 'X' | urizen-onebit-r45-c081 |
| 'Y' | urizen-onebit-r45-c082 |
| 'Z' | urizen-onebit-r45-c083 |
| '[' | urizen-onebit-r46-c092 |
| '\\' | urizen-onebit-r47-c093 |
| ']' | urizen-onebit-r46-c093 |
| '^' | urizen-onebit-r46-c102 |
| '_' | urizen-onebit-r47-c080 |
| '`' | urizen-onebit-r47-c090 |
| 'a' | urizen-onebit-r45-c084 |
| 'b' | urizen-onebit-r45-c085 |
| 'c' | urizen-onebit-r45-c086 |
| 'd' | urizen-onebit-r45-c087 |
| 'e' | urizen-onebit-r45-c088 |
| 'f' | urizen-onebit-r45-c089 |
| 'g' | urizen-onebit-r45-c090 |
| 'h' | urizen-onebit-r45-c091 |
| 'i' | urizen-onebit-r45-c092 |
| 'j' | urizen-onebit-r45-c093 |
| 'k' | urizen-onebit-r45-c094 |
| 'l' | urizen-onebit-r45-c095 |
| 'm' | urizen-onebit-r45-c096 |
| 'n' | urizen-onebit-r45-c097 |
| 'o' | urizen-onebit-r46-c078 |
| 'p' | urizen-onebit-r46-c079 |
| 'q' | urizen-onebit-r46-c080 |
| 'r' | urizen-onebit-r46-c081 |
| 's' | urizen-onebit-r46-c082 |
| 't' | urizen-onebit-r46-c083 |
| 'u' | urizen-onebit-r46-c084 |
| 'v' | urizen-onebit-r46-c085 |
| 'w' | urizen-onebit-r46-c086 |
| 'x' | urizen-onebit-r46-c087 |
| 'y' | urizen-onebit-r46-c088 |
| 'z' | urizen-onebit-r46-c089 |
| '{' | urizen-onebit-r46-c094 |
| '\|' | urizen-onebit-r47-c091 |
| '}' | urizen-onebit-r46-c095 |
| '~' | urizen-onebit-r47-c083 |
