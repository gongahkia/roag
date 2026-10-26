# ROAG expansion encounters (M4)

The seven biome elites and sixteen ordinary enemy identities use the existing
integer-time scheduler, locked `WindupStarted` cells, batched damage and death
rewards. These are provisional balance values; no player attack cost, strict
turn ordering, boon rule or pressure budget changed.

## Biome elites

The Thorn Warden remains exclusive to Overgrown Ruins. The other biomes now
spawn a unique elite in the lower arena of a connected-floor stage (or the
single arena of a legacy fixture). Every elite alternates two locked attack
footprints. At half HP the existing phase event raises speed by 25 and reverses
the next pattern; normal damage does not cancel a committed windup.

| Biome | Elite | Alternating footprints |
|---|---|---|
| Fungal Hollows | Mycelial Matriarch | Target plus diagonal spores; outer ring around source |
| Sunken Aqueduct | Canal Lockmaster | Four-cell cardinal lane; parallel sidebars around target |
| Ashen Bastion | Ash Marshal | Front fan; target cross |
| Glacial Observatory | Prism Curator | Two-radius diagonal star; two-radius target ring |
| Fractured Archive | Index Keeper | Five-cell target row; two three-cell target columns |
| Sundered Forge | Ember Crucible | Three-spoke source pressure; compact target box |

The arena, one free elite chest, grace/deadline, pursuer and exit behavior stay
in `src/stage.lua`. Old in-progress snapshots with a Warden in a non-Ruins
biome remain readable because they did not store an elite definition ID. New
snapshots store and validate the biome-specific ID.

## Ten new ordinary tactical roles

| Enemy | Encounter role | Locked action or interaction |
|---|---|---|
| Mossbreaker | cover breaker | Attacks adjacent breakable barricades before pursuing; short melee cleave |
| Gloom Strider | ranged skirmisher | Steps away at close range; fires a three-cell fork |
| Mire Binder | space denial | Locks an eight-neighbor ring while leaving its center safe |
| Pack Chorister | pack coordinator | Adjacent hit gains up to two damage from living nearby allies |
| Vault Lurker | delayed ambusher | Waits beyond five tiles until alerted, then uses a delayed diagonal mark |
| Crystal Bombardier | anti-cluster artillery | Target and cardinal two-step blasts, including friendly fire |
| Cinder Mender | support | Locked heal of the most wounded allied entity in range |
| Glass Charger | mobile charger | Locked cardinal line, then follows its actual open path up to three cells |
| Forge Conjurer | summoner | Locked adjacent summon of a normal Skitter, maximum three lifetime summons |
| Bastion Bulwark | defender | Positive hostile HP loss arms a cross-shaped retaliatory windup; blocked hits do not |

Support, summon and charge follow-up data are plain tables on the existing
pending attack. Heals and summons resolve only at its scheduled due time.
Summoned Skitters use ordinary faction, AI, rewards and death logic; the source
has a lifetime cap to avoid an unbounded farm. Charged movement emits the
ordinary `Moved`/`TileEntered` events with the actual traversed path for the
existing board-piece slide renderer. Barricade breaks, artillery friendly fire,
retaliation and all hits use canonical events. The new roles are in the normal
biome generation and pressure pools; the first three inherited biome mixes
were kept intact to protect the established legal-action run.

## Verification and limits

`TMPDIR=/var/tmp luajit tests/run.lua` covers each identity/pool, distinct
elite footprints and phase choices, pending windup restore/continuation,
Mossbreaker terrain edits, support/summon/charge, friendly-fire batches and
retaliation positive versus blocked damage. The seven-biome generation corpus
continues to validate 100 seeds per biome. `TMPDIR=/var/tmp sh
tests/expansion_encounter_gui_smoke.sh` captures actual elite and summoner
telegraphs in separate LÖVE processes; its prepared positions are **assisted
render fixtures**, not ordinary player travel. The legacy single-floor legal
seed-3 route still reaches Victory, now in 945 ordinary actions because the
Fungal and Ashen elite encounters have changed. A complete default two-floor
legal Victory and human readability/balance acceptance remain M8 work.

The numerical stats and shape recipes live in `src/content.lua`,
`src/elite_ai.lua` and `src/regular_ai.lua`. No independent combat resolver or
new save schema was introduced.
