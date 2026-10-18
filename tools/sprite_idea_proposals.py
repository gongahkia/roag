"""Curated design proposals only; never imported by the game or a publisher.

Indices are zero-based source cells. The builder removes used/empty cells,
records overlaps between proposals, and accounts for every remaining cell.
Motif names are visual interpretations, not official Kenney asset names.
"""


def span(first, last):
    return list(range(first, last + 1))


def block(columns, rows, cols):
    return [r * columns + c for r in rows for c in cols]


def art(sheet, indices, label, role='prop'):
    return dict(sheet=sheet, indices=indices, label=label, role=role)


def proposals():
    cards = []

    def add(id, name, kind, effort, idea, overlap, caution, groups,
            *, remainder=False, dependencies=(), excluded=False):
        cards.append(dict(id=id, title=name, kind=kind, effort=effort,
            idea=idea, existing=overlap, decisions=caution, groups=groups,
            remainder=remainder, dependencies=list(dependencies), excluded=excluded))

    # Exclusions claim first. These cells must not become an accidental new
    # suggestion merely because a later broad material-family range includes them.
    add('held-decisions', 'Previously excluded art — keep on hold', 'held', 'none',
        'Preserve the rejected flask/brazier variants and the hanging-slime reading. No spore-pod or bomb mechanic is proposed.',
        'Mushroom scenery, selected flasks/braziers and abandoned rails already exist. Hittable spore pods and bombs were rejected; moving carts were not selected.',
        'This is an exclusion record, not a request to reverse earlier decisions. Comment only if you deliberately want to revisit an exclusion.',
        [art('micro',[123],'Excluded flask variant'),art('onebit',[315,316,317,740],'Hanging slime, not approved mushroom art; excluded brazier')], excluded=True)
    add('held-modern', 'Modern/vehicle motifs — setting mismatch', 'held', 'none',
        'Keep overtly modern vehicle/electronic-looking motifs out of the current fantasy proposals.',
        'Their presence in a general-purpose source sheet does not mean ROAG needs spaceships, trains or handheld consoles.',
        'These remain visible for completeness. Comment if a particular cell has been misread; no unrelated vehicle/electronics feature is proposed.',
        [art('onebit',span(43,47)+span(81,86)+span(92,96)+[747,749,750],'Vehicle/electronic-looking motifs','icon')], excluded=True)

    # Enemies: each proposal has a distinct tactical decision. Reused cells
    # are alternatives and are flagged explicitly by the builder and client.
    add('reanimator', 'Grave Cantor + finite resurrection', 'enemy', 'high',
        'A fragile caster commits one revival of a fallen ally. Occupy the marked corpse tile or kill the caster to stop it; the group has one revival budget.',
        'Mend Acolytes heal living allies and Forge Conjurers summon; neither revives the dead.',
        'Needs corpse records, locked occupancy, cancellation and save tests. Revived units must not farm XP/gold. Hooded figures are candidates, not a named official necromancer.',
        [art('tiny',[85,88],'Hooded caster candidates','actor'),art('onebit',[318,173],'Hooded/staff-bearing alternatives','actor'),art('rpg',[564,565,566],'Grave markers'),art('onebit',[72,539,628],'Graves and hourglass','cue')])
    add('haunted-cache', 'Haunted cache / chest mimic', 'enemy', 'medium',
        'A visibly suspicious chest wakes into a locked, telegraphed bite rather than granting an immediate free ambush hit.',
        'Vault Lurkers ambush and vaults can spawn enemies. A container-to-enemy transition would be new.',
        'Choose its tell, wake action, reward and minimap treatment. Tiny #92 reads as an open mimic-like chest; 1-Bit can use a chest-to-ghost reveal.',
        [art('tiny',[90,91,92],'Chest states / mimic-like open form','actor'),art('tiny',[121],'Ghost reveal alternative','actor'),art('onebit',[303,417],'Chest and ghost reveal','actor')])
    add('heavy-rammer', 'Heavy Rammer + positional shove', 'enemy', 'medium-high',
        'A slow enemy locks a one-tile shove toward brambles, bog or a partner’s snare. Moving sideways or baiting the strike changes its landing.',
        'Glass Charger moves itself; Hookrunner pulls AI enemies. Player displacement is not currently supported by the shared displacement operation.',
        'Define wall/occupied-cell collisions and stairs. Reject illegal landings and do not guarantee reaction time. A blocked shove must have a clear result.',
        [art('tiny',[89,109],'Blocky armoured figure / heavy humanoid','actor'),art('onebit',[129,276],'Armoured / insect alternatives','actor')])
    add('web-weaver', 'Web Weaver + anchored webs', 'enemy', 'medium',
        'A spider commits a line between two visible anchors. Crossing consumes the web and adds recovery; destroying an anchor clears the line.',
        'Snare Scribe already places individual floor traps. Anchored, breakable lines supply a different route decision.',
        'Cap active webs, preserve an exit, show endpoints through fog correctly and save their remaining state. No real-time spreading or simulation on render.',
        [art('tiny',[122],'Spider-like body','actor'),art('onebit',[414,463],'Spider/creature alternatives','actor'),art('micro',[60],'Mesh-like cue','cue')])
    add('hollow-wraith', 'Hollow Wraith + announced phase step', 'enemy', 'medium',
        'A hollow figure marks its destination before phasing to a visible, legal tile. Standing on the destination cancels the move.',
        'Gloom Strider skirmishes and Shade has a decoy swap; this is an enemy relocation commitment, not hidden teleport damage.',
        'No unseen spawn, occupied landing or floor transition. Attack and relocation must remain separate commitments.',
        [art('onebit',[416,464,517],'Hollow silhouettes','actor'),art('tiny',[121],'Ghost alternative','actor')])
    add('gulp-toad', 'Gulp Toad + recoverable stolen drop', 'enemy', 'medium',
        'A bulky creature visibly swallows one nearby loose drop, then carries it until defeated. The player can intercept it or fight for the same drop.',
        'Food/health/currency drops already exist. No enemy currently carries a specific swallowed pickup.',
        'No removal from player inventory and no duplicate rewards. Do not consume unique keys or make route completion depend on the toad.',
        [art('onebit',[421,422,423],'Bulky creature / frog-like alternatives','actor'),art('tiny',[123],'Beast-like mixed alternative','actor')])
    add('mirror-hexer', 'Mirror Hexer + reflected lane', 'enemy', 'high',
        'A caster marks one reflective lane. A later direct ranged attack through it reflects along a previewed cardinal path; side attacks and melee bypass it.',
        'Shield Bearer blocks its front wedge. This proposal redirects a limited attack class rather than adding another all-purpose shield.',
        'Must preview both paths, bound reflection to one bounce and preserve friendly fire. DOT/boon chains must not recursively reflect.',
        [art('onebit',[320,468],'Robed / spectral caster choices','actor'),art('rpg',[479,480,481],'Mirror fixtures'),art('onebit',[493,494],'Reflector cue','cue')])
    add('trail-worm', 'Silt Worm + temporary bog trail', 'enemy', 'medium',
        'A worm announces a short charge and leaves two temporary bog cells behind it. The trail changes approach costs, not the whole floor.',
        'Bog already increases recovery; Glass Charger already commits movement. The new decision is the visible trailing lane.',
        'Cap trail cells and lifetime in simulation time. Keep stairs/doors free and restore underlying terrain exactly after expiry.',
        [art('onebit',[420,371],'Worm / lizard-like alternatives','actor'),art('caves',[64,65],'Silt cue alternatives','cue')])
    add('pain-linked-twins', 'Oath Twins + breakable pain link', 'enemy', 'high',
        'Two enemies visibly share part of incoming damage while their short LOS link is intact. Separate them with geometry or displacement before committing an area attack.',
        'Ward Weaver prevents damage and healers restore it. A shared finite HP-loss link would be a different group puzzle.',
        'Needs simultaneous batch rules, no recursive secondary procs and fog-clipped links. Agree whether separation or one death breaks the oath permanently.',
        [art('onebit',[124,127],'Paired warrior alternatives','actor'),art('tiny',[99,100],'Paired humanoid alternatives','actor'),art('onebit',[574],'Link motif','cue')])
    add('alarm-keeper', 'Alarm Keeper + one interruptible call', 'enemy', 'medium',
        'A bell-bearing keeper commits a long visible alarm. Killing or stunning it cancels a single bounded reinforcement group.',
        'Captains buff existing followers; Conjurer summons repeatedly up to its cap. This keeper has one call and never creates an infinite stream.',
        'Reinforcements must appear at marked legal cells, with fixed eligibility for rewards. A failed placement must not block the floor.',
        [art('onebit',[173,470],'Keeper candidates','actor'),art('rpg',[681,682,683],'Bell-like fixtures'),art('onebit',[867,915],'Bell symbols','cue')])
    add('ember-moth', 'Ember Moth + readable burning landing', 'enemy', 'medium',
        'A flying creature locks a landing cell and leaves a short-lived ember there after impact. The landing and follow-up hazard remain visibly distinct.',
        'Rushwings already flank; Burning already exists. The moth pressures space after its strike instead of healing or coordinating a pack.',
        'Do not reinterpret rejected spore pods as this enemy. No hidden landing, guaranteed dodge or long-lived terrain flood.',
        [art('onebit',[410,411,412,413],'Winged creature alternatives','actor'),art('micro',[136,137],'Ember motifs','cue')])
    add('bone-splitter', 'Bone Splitter + reward-bounded fragments', 'enemy', 'medium-high',
        'A skeleton collapses into two weak fragments after its first defeat. Destroy the fragments to finish the encounter; they cannot split again.',
        'Existing enemies summon or revive no such death fragments. The primary death HP batch must still finish before secondary spawns.',
        'One total reward budget, capped fragments, legal occupied-cell handling and saved split state. Do not prolong every ordinary fight.',
        [art('onebit',[323,324,374],'Skeletal alternatives','actor'),art('caves',[58,60,61],'Bone remnants','cue')])
    add('rotating-sentinel', 'Rotating Sentinel encounter', 'enemy', 'high',
        'An optional armoured champion commits successive quarter-turn strikes. Its next facing is announced, creating a circular positioning puzzle.',
        'Shield Bearer and existing bosses already have facing/phase rules. This is a potential encounter, not another permanent invulnerable front.',
        'Keep every footprint locked; stop the sequence on stun/death/displacement. Decide whether it is a room champion or a future miniboss.',
        [art('onebit',[224,129],'Armoured body alternatives','actor')])
    add('brood-keeper', 'Brood Keeper + finite hatching anchors', 'enemy', 'high',
        'A creature marks two hatching anchors. Destroying an anchor cancels its one hatch; the keeper cannot replenish them.',
        'Forge Conjurer already summons, so anchor destruction and a finite budget must be the distinguishing decisions.',
        'Not an exploding spore pod. Eggs are separate marked spawn targets, not damaging mystery scenery; cap enemies and rewards.',
        [art('onebit',[276,360,361,362,363,364,365,366],'Insect/beast body pool','actor'),art('tiny',[122],'Spider body alternative','actor')])
    add('echo-duelist', 'Echo Duelist + copied attack shape', 'enemy', 'high',
        'A duelist shows whether its next committed strike copies a short line or a sweep from the player’s last eligible attack.',
        'Temporal Echo is a boon and several enemies use sweeps. This enemy reads a small recorded action category rather than copying arbitrary boon code.',
        'Only two vetted shapes, fixed stats and normal locked timing. Display its chosen shape before resolution; never steal or disable a player boon.',
        [art('onebit',[125,128,471],'Duelist alternatives','actor'),art('micro',[70,71],'Blade symbols','icon')])

    # Playable classes are alternatives, not promises to grow the roster all at once.
    add('duelist-class', 'Duelist — precision and paid riposte', 'class', 'medium-high',
        'A light blade class uses a narrow thrust, a committed parry window and a sidestep. A successful parry funds one riposte instead of granting passive invulnerability.',
        'Adventurer is balanced, Berserker cleaves, Sentinel is defensive and Hookrunner pulls. The new decision is timing one paid defensive commitment.',
        'No free guaranteed reaction action. Define which direct attacks can be parried and make the window use scheduler time.',
        [art('tiny',[99],'Light humanoid candidate','actor'),art('onebit',[124,127],'Light fighter alternatives','actor'),art('tiny',[103,104,105,106,107],'Blade options','icon')])
    add('geomancer-class', 'Geomancer — one temporary bulwark', 'class', 'high',
        'A caster raises one short-lived stone block on an adjacent legal cell, shaping a firing lane before a heavy stone strike.',
        'Arcanist attacks zones, Trapper lays snares. A deliberately placed temporary LOS/movement obstacle is a different role.',
        'Never seal required access, stairs or an occupied tile. Store original terrain and a simulation-time expiry; published art must not own collision.',
        [art('tiny',[100],'Grey-haired figure alternative','actor'),art('onebit',[467],'Pointed-hat caster','actor'),art('micro',[116],'Stone face / rock symbol','icon')])
    add('spiritbinder-class', 'Spiritbinder — one commanded familiar', 'class', 'high',
        'Command one bound spirit to occupy a visible nearby cell or attack a locked target. Repositioning it costs the player an ordinary action.',
        'Shade’s decoy draws attacks and swaps once. This spirit performs bounded damage under explicit orders rather than autonomous free attacks.',
        'Needs friendly ownership, cap, save state and reward rules. Never use render dt or allow a familiar to scout unseen rooms for free.',
        [art('tiny',[85],'Hooded player candidate','actor'),art('onebit',[122,417],'Binder and familiar alternatives','actor')],dependencies=['hollow-wraith'])
    add('pilgrim-class', 'Pilgrim — spend health for a measured vow', 'class', 'high',
        'A close-range pilgrim trades a clearly shown amount of current HP for one empowered action, then returns to ordinary attacks.',
        'Berserker is durable/high damage and Alchemist throws flasks. This is an explicit finite HP expenditure, not free regeneration.',
        'No permanent meta advantage, healing exploit or accidental lethal toggle. Preview the exact cost and the one-action benefit.',
        [art('tiny',[88],'Hooded figure candidate','actor'),art('onebit',[318,468],'Robed alternatives','actor'),art('onebit',[582,583,584,585],'Cross/seal motifs','icon')])
    add('tinkerer-class', 'Tinkerer — one limited bolt sentry', 'class', 'high',
        'Place one visible bolt sentry with three charges. Each shot follows an explicitly scheduled commitment; replacing it discards remaining charges.',
        'Ranger shoots directly and Trapper controls movement. A finite deployed attacker adds setup and line-of-fire decisions.',
        'Needs friendly-fire, target visibility, ownership, occupancy and save rules. No wall penetration, automatic scouting or unlimited wait farming.',
        [art('tiny',[109],'Heavy humanoid alternative','actor'),art('onebit',[222],'Wide-hat figure','actor'),art('micro',[119,120],'Mechanical/stand-like prop alternatives','prop')])
    add('stormcaller-class', 'Stormcaller — charge, ground, discharge', 'class', 'high',
        'Build a small visible charge through direct hits, spend it on a delayed locked cross, or ground it into a one-hit ward.',
        'Arcanist already has lightning and fire zones; the distinction must be a capped choice between expenditure modes, not a recolour.',
        'No extra boon proc coefficient loophole. Decide whether this is distinct enough to justify a class before implementing it.',
        [art('onebit',[175,177],'Crowned/caster-like alternatives','actor'),art('tiny',[100],'Mixed candidate','actor'),art('onebit',[615,616,617,618,619],'Discharge motifs','cue')])
    add('wayfinder-class', 'Wayfinder — a visible return beacon', 'class', 'high',
        'Plant one beacon on explored ground, then spend the movement ability to return within a bounded same-room distance.',
        'Shade already swaps with a decoy. The beacon does not draw attacks; it offers deliberate retreat and cannot scout or cross floors.',
        'Reject walls, occupied landings, doors and stairs as necessary. Approve only if the difference from Shade is useful enough.',
        [art('onebit',[473,465],'Scout/hooded alternatives','actor'),art('micro',[95],'Cloth/marker-like option','prop'),art('onebit',[459,408],'Flag-like markers','cue')])
    add('cantor-class', 'Cantor — a three-note attack phrase', 'class', 'high',
        'Alternate thrust and pulse to build a displayed three-note phrase, then spend it on a local push or direct hit.',
        'There is no rhythm minigame or animation timeline. World time still advances only through ordinary legal actions.',
        'No real-time beat test and no party-wide free buff. Requires a simple saved capped phrase and clearly previewed payoff.',
        [art('onebit',[32,33,470],'Hatted/robed player alternatives','actor'),art('onebit',[823,824],'Music/cup symbols','icon')])

    add('offering-font','One-use HP offering font','item','medium',
        'Choose a boon offer in exchange for an explicit HP payment at an optional font.',
        'Shrine room templates and gold chests exist, but no HP-payment altar interaction does.',
        'Agree the cost, lethal-use policy, confirmation and one-use save state. Do not require payment to complete the route.',
        [art('tiny',[32,74],'Glowing fixture / bowl'),art('rpg',[563],'Cyan stone alternative'),art('onebit',[690,691],'Vessel/urn alternatives')])
    add('ration-cache','Sealed ration cache','item','small-medium',
        'A finite cache offers immediate food or a short ward, letting the player choose recovery versus the next fight.',
        'Food/health drops and a three-action ward already exist. This combines existing effects in one explicit choice, not a new hunger system.',
        'One use, no explosive barrel, no extra break-for-gold duplicate. Reward quantities need pacing tests.',
        [art('rpg',span(23,27),'Barrel variants'),art('tiny',[63,75],'Crate forms'),art('onebit',[650,651],'Barrel forms')])
    add('cleanse-flask','Single-use cleansing flask','item','medium',
        'A clearly labelled dose removes Burning now instead of healing HP. It can be selected from an optional supply choice.',
        'Burning and healing exist; no poison system is assumed.',
        'Decide immediate-use versus one carried charge. Do not introduce a full inventory UI for one potion.',
        [art('tiny',span(113,117),'Potion colours','icon'),art('micro',[107],'Approved-shape spare flask','icon'),art('onebit',[579,580,581,669,670,671,718,719,720],'Bottle/vial alternatives','icon')])
    add('escape-scroll','One-charge escape scroll','item','medium-high',
        'Spend a finite charge to step to a previewed visible legal cell at short range; the move consumes a committed action.',
        'Dash already exists. A carried consumable would be new and must not replace class identity.',
        'Choose charge storage, pickup behavior and LOS/wall rules. No unseen teleport or stair activation through the scroll.',
        [art('rpg',[896,897,898,899],'Paper/scroll forms','icon'),art('onebit',[139,140,525],'Scroll/flag-like alternatives','icon')])
    add('return-token','Room-bound return token','item','high',
        'A one-use token returns the player to a deliberately placed, same-room marker when a normal action is spent.',
        'Would share landing validation with the proposed Wayfinder, without giving every class a permanent teleport.',
        'Only if a carried-charge workflow is approved. No return across floors, occupied cells or closed gates.',
        [art('onebit',[574,575],'Linked-circle / spark motifs','icon'),art('micro',[100],'Linked ornament-like motif','icon')],dependencies=['wayfinder-class'])
    add('charm-well','Charm well — choose one short condition','item','medium',
        'A well offers one clearly stated temporary condition, such as a ward for the next three committed actions, instead of another permanent boon.',
        'Ward flask already exists. The new choice must be a small mutually exclusive menu rather than automatic buffs.',
        'Use explicit action counts and finite use. Agree on the alternatives and reward pacing before adding numbers.',
        [art('onebit',[435,436,437,438],'Pendant/charm motifs','icon'),art('rpg',[853,854,855,911],'Vessel/chalice forms')])
    add('weapon-practice','Practice rack — preview before choosing','room','medium',
        'A safe optional rack previews existing class attacks on inert targets without changing the current run’s class or consuming gameplay rewards.',
        'Nine classes already exist. This is an onboarding/training room, not unrequested collectible weapons.',
        'Decide title-only practice versus an explicitly separate practice fixture. Training must be achievement-ineligible and never alter a saved run.',
        [art('tiny',[103,104,105,106,107,118,119,125,126,127,128,129,130,131],'Weapons/shields','icon'),art('onebit',span(329,336),'Blade variations','icon')])
    add('attunement-forge','Attunement forge — trade one stack','room','high',
        'At a one-use forge, explicitly trade one selected boon stack and gold for one previewed alternative offer.',
        'Current chests give three offers and mixed-rarity stacks are uncapped. This adds a controlled build correction, not a free rarity upgrade.',
        'Must preserve frozen run pool, rarity accounting, deterministic offers, transformations and cancellation. Agree exact trade rules first.',
        [art('rpg',[13,14,510,511,624,625],'Forge/hearth assemblies'),art('onebit',[570,400,401,402],'Hearth/workshop motifs')])
    add('travelling-trader','One-visit supply trader','room','medium-high',
        'An optional stall offers two finite services, such as food versus an already-generated boon offer, at visible gold prices.',
        'Gold chests already supply boons. The trader needs a useful service choice rather than cheaper duplicate rewards.',
        'No resale loop, inventory overhaul or refresh-farming. Fix stock and prices in the floor snapshot.',
        [art('rpg',[239,240,241,296,297,298,353,354,355,668,669,670],'Stall goods and gold'),art('onebit',[768,769],'Stall/cart-like motifs; stationary only')])
    add('memorial-choice','Memorial — retire a boon for recovery','room','high',
        'An optional memorial trades one chosen stack for immediate recovery, with the exact effect previewed before confirmation.',
        'This is different from a free heal and from the proposed forge’s alternate offer.',
        'Approve only if losing a boon feels worthwhile. Needs transformation rollback, mixed-rarity stack selection and deterministic save continuation.',
        [art('rpg',[564,565,566,677,678,679,680],'Memorial gravestones'),art('onebit',[24,72,539],'Headstone motifs')])

    add('mirror-puzzle','Mirror relay room','room','high',
        'Reorient a small fixed set of reflectors to route a visible beam into a receiver, opening an optional cache.',
        'Keys and timed plates exist. This is a spatial relay rather than another plate timer.',
        'At most one bounce per reflector and bounded traversal. No arbitrary ray engine, moving bullet requirement or mandatory puzzle gate.',
        [art('rpg',[200,201,422,423,479,480,481],'Mirror variants'),art('onebit',[493,494,648],'Slanted reflector forms')],dependencies=['mirror-hexer'])
    add('bell-sequence','Bell-order side room','room','medium',
        'Read three pictorial clues and activate stationary bells in that order for an optional reward.',
        'Sidequests already record clues/plate state. This adds a short visible sequence, not real-time rhythm input.',
        'Reset behavior and readable symbols must be clear. Audio may reinforce the clue but must not be required.',
        [art('rpg',[681,682,683,963,964],'Bell/gong-like props'),art('onebit',[867,915,964],'Bell-like symbols')])
    add('prism-relay','Prism socket relay','room','high',
        'Activate fixed coloured sockets to connect a short displayed relay. A wrong choice resets the relay without unavoidable damage.',
        'Crystal terrain reacts to lightning already. This is a room objective and should reuse that rule where possible.',
        'Colours need distinct shapes in 1-Bit. Keep sockets/frames separate from traversable ground and cap state combinations.',
        [art('caves',span(358,363),'Crystal/ore colours'),art('tiny',[56],'Crystal-like prop'),art('onebit',[112,113,114,115,168,169,170],'Socket/frame cues')])
    add('heat-vents','Clocked heat vents','terrain','high',
        'A small vent alternates warning, impact and cooldown at explicit simulation timestamps. It pressures a route without relying on animation speed.',
        'Burning exists; static decorative hearths are not hazards by default.',
        'Never activate from render dt, place unavoidable spawn damage or hide the warning under fog. Needs saved phase and next due time.',
        [art('tiny',[9,29],'Vented/flame wall fixtures'),art('caves',[506,507],'Flame-topped fixtures'),art('onebit',[564,565,566,567],'Warning/heat motifs','cue')])
    add('sluice-room','Two-state sluice room','terrain','high',
        'One visible lever changes a bounded set of water cells between two authored states, revealing an optional crossing.',
        'Water is static movement-blocking terrain. This is a saved discrete switch, not liquid simulation.',
        'Both states need connectivity checks. Never flood an occupied cell, required stairs or the only route; no full fluid system.',
        [art('caves',span(290,299)+span(319,328)+span(348,357)+span(377,386),'Water edges/corners','water'),art('rpg',[953,954,955,1010,1011,1012],'Lever-like controls'),art('onebit',[557,558,559],'Mechanism alternatives')])
    add('push-floor','Push-floor strip','terrain','high',
        'Arrow-marked cells add one legal-tile push after entry. The strip can be used tactically by both sides.',
        'This is a discrete terrain effect, not a moving train/cart or physical conveyor simulation.',
        'Depends on safe player displacement. Bound chains, resolve blocked landings and keep mandatory exits safe.',
        [art('onebit',span(1000,1006)+[1008,1009,1010,1011],'Direction markings','cue'),art('micro',[113,114,115],'Strip/arrow-like cues','cue')],dependencies=['heavy-rammer'])
    add('fragile-crossing','Fragile optional crossing','terrain','high',
        'A cracked bridge segment clearly warns that it will close after one crossing, giving a shortcut with an alternate return route.',
        'Barricades break and timed gates close already. This would be a one-way optional shortcut, not a required collapsing floor.',
        'Prove alternate routes before placement, restore/save its exact state and forbid occupied-cell collapse damage.',
        [art('caves',[93,94,122,123,237,266],'Wood supports/beam parts','assembly'),art('rpg',[493,494,495],'Ladder/wood crossing parts','assembly'),art('onebit',[70,294,600],'Ladder-like symbols','assembly')])
    add('falling-fixture','Announced falling fixture','room','medium-high',
        'A suspended fixture can be triggered from a marked switch; it first locks a small impact area, then falls through the normal scheduler.',
        'Not a volatile bomb or spore pod. It is an explicit one-use room trap with friendly fire.',
        'Visible trigger and delay, one saved use, and no hidden enemy tracking after commitment. Must not obstruct mandatory routes.',
        [art('rpg',[475,476,477,478],'Suspended fixture candidates','assembly'),art('onebit',[547,549],'Hanging/fixture-like alternatives')])
    add('signposted-routes','Pictorial route signposts','room','small-medium',
        'A sign near a branch hints at an already-generated room’s reward or danger category, helping navigation without revealing its exact geometry.',
        'Route choices, fog and secrets already exist. This adds readable local hints, not full-map revelation.',
        'Only show information the sign is intended to disclose. Avoid misleading signs unless that is separately approved.',
        [art('rpg',span(16,22)+[425,737,794,851,908],'Signs/boards'),art('onebit',[674,675,502,503],'Sign/information motifs')])
    add('camp-choice','One-use camp choice','room','medium',
        'A tent offers one finite rest choice: recover now or prepare a short ward. Leaving commits the choice.',
        'Food and wards exist; this is a visible pacing node rather than another permanent stat bonus.',
        'No unlimited wait healing or normal-run reset. Decide whether a camp replaces a chest slot or adds a rarer side room.',
        [art('rpg',[616,617,618,619,674,675],'Tent parts','assembly'),art('micro',[117],'Tent/hooded shelter-like prop'),art('onebit',[508,509],'Tent-like motifs')])
    add('archive-contract','Archivist’s optional contract','room','high',
        'Accept a clearly scoped room challenge, such as clearing its marked enemies without using Dash, for a fixed optional reward.',
        'Sidequests already exist. The new piece is a voluntarily accepted, visible action constraint.',
        'Never disable controls silently. Explicit failure/cancellation and no profile rewards from practice or developer fixtures.',
        [art('rpg',[896,897,899,901,902,903,904,905,906,907],'Books/papers','icon'),art('onebit',[392,441,442,448],'Book/scroll-like motifs','icon')])
    add('sparring-trial','Optional sparring trial','room','high',
        'Enter a clearly labelled short arena with one fixed enemy pattern to learn its counterplay, then take one fixed reward.',
        'Elites and optional secrets exist. This is an opt-in teaching room, not a second boss progression.',
        'Define exit/failure behavior before closing any gate. No weakened fixture should be claimed as an ordinary legal victory.',
        [art('rpg',[203,204,205,206,207,211,212,262],'Bars/gate parts','assembly'),art('onebit',[150,152,153,262],'Gate-like alternatives','assembly')])
    add('secret-mural','Readable secret murals','presentation','small-medium',
        'Use spare face/sigil art as local clue variants for existing secret mechanisms.',
        'Crack and spiral clues already exist. This is a readability and art expansion, not another secret reward system.',
        'Clues must remain visible without a specific colour pack. No new collision inferred from the image.',
        [art('micro',[98,116,118],'Frame/face/sigil motifs','icon'),art('onebit',[135,136,137,138,233,234],'Face/shield motifs','icon')])

    # Environment kits include ground separately from walls/assemblies. Many
    # source cells are variants/corners; approval is not proof of seam quality.
    add('crypt-kit','Ossuary / crypt environment kit','environment','high',
        'A bone-and-grave environment could become an optional room family or a future eighth biome.',
        'Seven biomes and unique minibosses already exist. Art alone does not define a new biome.',
        'Choose dressing-only versus full biome. Full biome needs signature mechanic, route/boss/generation/save acceptance. Never use grave props as floor.',
        [art('caves',[58,59,60,61],'Bone remains'),art('rpg',[564,565,566,677,678,679,680],'Grave variants'),art('onebit',[24,72,539,620,621],'Grave/bone motifs')])
    add('sandstone-kit','Pale sanctuary — ground and masonry','environment','medium-high',
        'Assemble pale stone floors, walls and columns for a readable sanctuary or crypt room family.',
        'Could enrich current rooms before adding a biome. The kit is not a ready-made connected terrain implementation.',
        'Review ground and wall parts separately; test every connected edge and corner in actual generated rooms.',
        [art('rpg',block(57,range(0,6),[8])+block(57,range(18,25),range(6,15)),'Pale ground variants','ground'),art('rpg',block(57,range(12,21),range(13,20)),'Pale architectural pieces','assembly')])
    add('timber-mine-kit','Timber mine — floors, frames and braces','environment','medium-high',
        'Use wood surfaces and support assemblies to make mining rooms distinguishable without moving carts.',
        'Abandoned rails and ore are already approved. This is a stronger environment kit for those rooms.',
        'Supports stay on walls/props, never repeated as ground. A new timber palette needs a game-scale seam and readability review.',
        [art('caves',span(150,231)+span(237,289),'Brown surfaces and wood/stone assemblies','assembly'),art('rpg',block(57,range(2,6),range(5,10)),'Floor material alternatives','ground')])
    add('observatory-mosaic','Observatory mosaic and cold stone','environment','medium-high',
        'Give Glacial Observatory a more precise pale/cool material set with matching carved accents.',
        'The biome already exists. This is a palette/material improvement, not new icy movement rules.',
        'Keep player/enemy/telegraph contrast and use full connected-wall composition tests.',
        [art('rpg',block(57,range(12,21),range(26,32)),'Cool masonry assemblies','wall'),art('caves',span(480,484)+span(509,513),'Pale floor variants','ground')])
    add('root-garden-kit','Root garden and overgrown courts','environment','medium',
        'Use grass, flowers, shrubs and tree variants to distinguish side gardens and current Ruins rooms.',
        'Trees and mushroom scenery already exist. No hittable spore-pod mechanic is being reopened.',
        'Decoration must not imply blocking unless assigned as a real prop. Preserve approved mushroom art.',
        [art('rpg',block(57,range(6,15),range(0,5)),'Overgrown ground transitions','ground'),art('rpg',block(57,range(9,12),range(16,32)),'Trees/shrubs/flowers'),art('onebit',[49,50,51,52,53,55,56,97,98,99,100,101,102,104,105],'Vegetation alternatives')])
    add('drowned-sanctum','Drowned sanctum water kit','environment','medium-high',
        'Assemble water boundaries, arches and submerged accents for Aqueduct rooms.',
        'Water already blocks movement while permitting sight/projectiles. Art must not silently change that rule.',
        'Keep water/collision separate, check all corners, and use transparent water pieces over compatible bases only.',
        [art('rpg',block(57,range(0,6),range(0,5)),'Water family','water'),art('caves',span(406,443)+span(464,473)+span(493,501),'Waterfalls, edges and channels','water')])
    add('forge-masonry','Forge masonry and ironworks','environment','medium-high',
        'Strengthen Forge/Bastion identity with dark framing, iron gates and mechanical accents.',
        'These biomes already have terrain and enemies; iron-looking art is not automatically an impassable block.',
        'No unrelated industrial/space mechanics. Audit every multi-cell gate’s role and retain current navigation.',
        [art('caves',span(79,86)+span(108,115)+span(137,144)+span(232,236)+span(261,265),'Dark frames and iron parts','assembly'),art('rpg',block(57,range(2,8),range(32,48)),'Door/window variants','assembly')])
    add('library-carpet','Archive carpets and patterned ground','environment','medium',
        'Use coherent carpet borders to distinguish Archive chambers and safe reading alcoves.',
        'Floor visuals currently convey biome identity through a small material set. This adds room-level variation.',
        'Carpet border pieces are ground decoration, not walls. Keep tactical overlays legible over coloured patterns.',
        [art('rpg',block(57,range(16,25),range(12,15)),'Carpet centres and borders','ground'),art('onebit',[196,197,198,849,852],'Patterned material alternatives','ground')])
    add('broken-cavern-kit','Broken cavern — rubble, damaged edges, cave mouths','environment','medium',
        'Add damaged wall silhouettes and nonblocking rubble dressing to make cave rooms less repetitive.',
        'Generation already creates connected rooms; this changes appearance before considering any collision changes.',
        'Do not paint doorway/gate images as floors. Review occlusion and fog boundaries at wall corners.',
        [art('caves',span(0,78)+span(95,107)+span(124,149),'Rock/stone surfaces and broken assemblies','assembly')])
    add('ruin-masonry-kit','Ruins masonry variants','environment','medium-high',
        'Offer several coherent masonry families for the existing Ruins, with corners selected as a set.',
        'Current wall/water mask coverage is complete; spare cells add stylistic choices, not missing collision geometry.',
        'Material families need connected 47-mask visual tests. Approval of a pool does not mean every fragment is a floor tile.',
        [art('rpg',block(57,range(12,21),range(18,26)),'Neutral masonry parts','wall')])
    add('open-court-ground','Open-court grass and dirt ground','environment','medium',
        'Ground-only variants and rounded borders for optional open courts and room transitions.',
        'This can remain inside the existing room generator; no outdoor world or new algorithm is required.',
        'Keep passable ground distinct from walls/props and verify patch seams at native scale.',
        [art('rpg',block(57,range(7,13),range(6,15))+block(57,range(16,21),range(0,5)),'Dirt/grass region surfaces','ground')])
    add('ember-ground','Warm embers / fired-clay ground','environment','medium',
        'A warm ground set could improve Sundered Forge or become a cosmetic room variant.',
        'Existing Burning is gameplay; orange pixels alone must not create damage.',
        'Choose safe ground versus separately marked heat vents. Review colour contrast for hostile attacks.',
        [art('rpg',block(57,range(12,21),range(10,13))+block(57,range(18,21),range(0,5)),'Warm floor families','ground')])
    add('rose-geode','Rose geode cavern direction','environment','high',
        'A pink mineral room palette with existing crystal interactions; possible future biome direction if its traversal rule is approved.',
        'Glacial Observatory already uses crystal themes. A recolour is not enough to justify an eighth biome.',
        'Start as a rare room palette. A full biome needs distinctive rules, enemies, boss and route tests.',
        [art('rpg',block(57,range(21,24),range(0,5)),'Pink ground transitions','ground'),art('caves',[358,360,362],'Purple crystal accents')])
    add('ice-crossings','Cold channels and pale crossing accents','environment','medium-high',
        'Use alternate pale/blue boundaries to distinguish cold channels and their crossing points.',
        'Ice already modifies Dash; do not introduce movement changes merely from palette selection.',
        'Art-only first; any slippery/water rules require explicit terrain IDs and saved state.',
        [art('caves',span(306,318)+span(335,347)+span(364,376)+span(393,405),'Stone/water arches and boundary parts','assembly')])
    add('camp-furniture','Camp and inn furniture','environment','small-medium',
        'Beds, seats and tables make optional rest rooms readable before opening a menu.',
        'A camp interaction is a separate proposal. Decorative beds do not heal automatically.',
        'Keep large furniture assembled and off mandatory routes; choose collision intentionally.',
        [art('rpg',block(57,range(0,7),range(10,32)),'Beds, seating, tables and cabinets')])
    add('workshop-fixtures','Workshop fixtures and supplies','environment','small-medium',
        'Add coherent benches, cupboards and tool dressing to Forge or trader rooms.',
        'This is visual dressing independent of the proposed trader/attunement mechanics.',
        'Avoid introducing durability/crafting systems just because cabinets and tools exist.',
        [art('rpg',block(57,range(7,12),range(16,26))+block(57,range(9,12),range(32,40)),'Workshop fixtures')])
    add('archive-shelves','Archive bookshelves and empty shelf states','environment','small-medium',
        'Mix full and empty shelves to identify Archive rooms, with small readable story variation.',
        'Archive already exists and shelves need no new combat rules.',
        'Decide wall-attached versus floor prop; keep bookshelves out of ground tiling and preserve LOS semantics.',
        [art('rpg',span(725,734)+span(782,791)+span(839,848),'Shelf variants'),art('onebit',[299,346,347],'Shelf/cabinet alternatives')])
    add('chapel-windows','Chapel windows and alcoves','environment','small-medium',
        'Use narrow window and alcove assemblies for sanctuaries, shrines or crypt dressing.',
        'These do not make windows shoot-through by appearance. Current LOS/collision remains explicit.',
        'Multi-tile placement and wall attachment need approval. No exterior geometry revealed through fog.',
        [art('rpg',block(57,range(0,9),range(40,48)),'Windows and door/window assemblies','assembly')])
    add('prison-dressing','Prison cells, bars and grates','environment','medium',
        'A readable optional prison room kit with cells, grates and a distinct barred-gate silhouette.',
        'Keys and timed gates are already implemented. This is scenery or a skin unless a separate interaction is approved.',
        'Do not create false exits or imply projectile blocking from iron bars alone.',
        [art('caves',span(232,236)+span(261,265)+span(343,347)+span(372,376),'Bar/grid assemblies','assembly'),art('onebit',[147,148,149,151,152,153,201,202,250,251,252,261,262,263],'Bars and grates','assembly')])
    add('faction-banners','Faction banners and heraldry','environment','small-medium',
        'Use banner sets to identify enemy-room themes or an announced squad type before entering.',
        'The five existing squads are already implemented. Heraldry can improve recognition without a new faction system.',
        'No hidden-enemy reveal through walls. Treat each banner as a prop/assembly, not ground.',
        [art('rpg',block(57,range(0,8),range(48,57)),'Coloured banner sets','assembly'),art('micro',[75,76,77,78,79],'Banner/equipment alternatives')])
    add('stone-column-kit','Pillars, plinths and architectural supports','environment','small-medium',
        'More pillar/plinth silhouettes distinguish existing blockers and support structures.',
        'Solid pillars already exist. This adds art variants; new destructibility is not implied.',
        'Agree footprint, wall/prop role and whether a variant is decorative. Never infer collision from its sprite.',
        [art('caves',[145,146,147,148,149,174,175,176,177,178,203,204,205,206],'Column assemblies','assembly'),art('rpg',block(57,range(18,21),range(16,32)),'Carved support pieces','assembly')])

    add('boon-ring-art','Distinct ring artwork for existing boons','presentation','small',
        'Use spare ring silhouettes to clarify existing circular/protective boon identities.',
        'Ring of Reversal already uses Micro #89 / 1-Bit #337. These are art alternatives, not bombs.',
        'Choose exact existing boon targets after visual approval; leave the accepted Reversal ring intact.',
        [art('onebit',[338,339,340],'Ring/jewellery motifs','icon')])
    add('weapon-icon-families','Weapon and attack icon families','presentation','small-medium',
        'Use remaining blades, bows, axes, staves and shields as clearer action/boon icons and inspected ability art.',
        'Nine classes already have attacks. No collectible weapon inventory is being added by this artwork vote.',
        'Group by damage/ability meaning; icons do not change damage, range or class rules.',
        [art('micro',span(70,79),'Weapons/equipment','icon'),art('onebit',block(49,range(2,10),range(32,47)),'Weapon/equipment motif pool','icon')])
    add('status-icon-family','Status, supplies and reward pictograms','presentation','small-medium',
        'Give Burning, wards, healing, currency, charge and boon details clearer consistent symbols.',
        'Current status semantics and 53 boons stay authoritative. Extra bottle colours do not imply new statuses.',
        'Every symbol needs a real existing target or a separately approved mechanic. Preserve text fallback.',
        [art('micro',[88,99,101,102,106,107,112,128,129,130,131,132,133,134,135,136,137],'Small reward/effect motifs','icon'),art('onebit',[529,530,531,532,533,534,535,536,537,538,576,577,578,631,632,633,634,635,636,672,673,676,677,678,679,680,681,682,683,684],'Status/reward symbols','icon')])
    add('sprite-hud-skin','Scalable sprite HUD/menu frame skin','presentation','medium',
        'Replace selected drawn frame borders with an optional scalable sprite skin, retaining the uncluttered menu layout.',
        'Current menu frames and meters are drawn in code. Text/readability and authoritative HUD values stay unchanged.',
        'Needs correct corners/insets at two viewport sizes and tiny-size fallback. Card/tablet pictures are not generic frame pieces.',
        [art('onebit',[748,800,801,802,849,850,851,852,898,899,900,901,947,948,949,996,997,998,1045,1046,1047],'Border/corner sets','assembly')])
    add('bitmap-labels','Optional digits and uppercase bitmap labels','presentation','medium',
        'Use native digits/uppercase glyphs for compact counters and headings, with the existing TTF for unsupported text.',
        'No complete lowercase alphabet is established in this sheet. This is not a full font editor.',
        'Check glyph metrics, coverage, fallback and text alignment. Keep descriptions and menu actions readable.',
        [art('onebit',span(868,880)+span(917,929)+span(966,978)+span(1015,1020),'Digits, uppercase and punctuation','glyph')])
    add('map-signals','Map legend and route pictograms','presentation','small-medium',
        'Use spare stairs, arrows, flags and door motifs to label known room types in the map legend.',
        'Fog/minimap cells are intentionally drawn. Icons must represent known information only.',
        'Do not leak undiscovered enemies, secrets or exact room contents. Keep 1-Bit cues distinguishable by shape.',
        [art('onebit',[294,295,296,297,298,408,409,459,612,613,614,660,661,662,663,664,665,666,667,668,1007,1008,1009,1010,1011,1057,1058,1059,1060],'Navigation motifs','icon')])
    add('combat-bursts','Static combat impact cue family','presentation','small-medium',
        'Assign spare bursts, slashes and small rings to existing hit/block/boon playback events.',
        'This is static event art, not sprite animation or a particle timeline. Tactical footprints remain dotted outlines.',
        'Cap concurrent cues, respect fog and preserve event timing/RNG. No large opaque effect over a committed attack.',
        [art('micro',[69,128,129,130,131,132,133,134,135,136,137],'Small static effect candidates','cue'),art('onebit',[563,564,565,566,567,568,569,575,615,616,617,618,619,705,706,707,708,709,710,711,712,713,714,715,716,717,830,831,1048,1049],'Slash/burst/reticle cues','cue')])
    add('trap-signals','Switch, hazard and trap state markers','presentation','small-medium',
        'Give existing plates, snares, ward fields and timed mechanisms a consistent before/after icon vocabulary.',
        'The mechanisms already exist. A marker does not create a new terrain effect.',
        'Preserve exact authoritative states and drawn tactical outlines; colour must not be the only distinction.',
        [art('onebit',[623,624,625,626,627,628,629,630,674,675,695,696],'State/clock/reticle motifs','cue'),art('tiny',[54,55,60,61,62],'Plate/target-like cue alternatives','cue')])
    add('control-help-icons','Input and action help pictograms','presentation','small-medium',
        'Use spare arrows, pointer, keyboard/controller-like marks for concise help while retaining visible key labels.',
        'Keyboard and gamepad already work. This does not add a new input architecture.',
        'Do not show a mouse/tablet glyph as proof of physical testing. Preserve remapped key text and non-icon fallback.',
        [art('onebit',[526,527,528,622,819,820,821,822,825,826,827,828,829,1016,1017,1018,1019,1020],'Pointers / input symbols','icon')])
    add('oracle-choice','Oracle — visible finite omen choice','room','high',
        'An optional oracle shows three pictorial omens describing fixed local challenge/reward alternatives. Pick one; no deck or card combat is introduced.',
        'Three-offer boon chests already exist. An omen must describe a distinct room condition, not conceal another random chest.',
        'Specify all outcomes, costs and save behavior. Card/dice motifs are visual labels; do not infer a card-game system from them.',
        [art('onebit',span(726,733)+span(775,782)+span(803,815)+span(853,864)+span(902,913)+span(951,962),'Card/dice/omen symbol pool','icon')])
    add('existing-body-variants','Body variants for existing identities','presentation','small-medium',
        'Use spare complete figures to review repeated enemy/class appearances and selected cosmetic variants before adding more combat roles.',
        'Mixed has eight shared body pairs; standalone already has distinct current actors. Micro’s character-body rows are fully used.',
        'No unreviewed role remapping. A larger pool is not a promise that every figure reads correctly as every enemy.',
        [art('tiny',[85,88,89,99,100,109,121,122,123],'Nine spare complete figures','actor'),art('onebit',block(49,range(0,11),range(24,32)),'Spare figure variants','actor')], remainder=True)

    # Exhaustive families: these take only cells not already grouped above.
    # They are still useful proposals, with explicit assembly/source constraints.
    add('tiny-dungeon-kit','Compact brick/sand dungeon kit','environment','medium-high',
        'Review Tiny Dungeon’s remaining ground, brick, stairs and doorway assemblies as one coherent alternative room palette.',
        'The mixed choice currently uses Tiny actors and some fixtures, not this complete environment kit.',
        'Doors/archways remain wall/prop pieces; the ochre ground cells are separate. Requires seam/contrast tests and no collision-from-colour.',
        [art('tiny',span(0,59),'Ground/brick/stairs/door assemblies','assembly')], remainder=True)
    add('tiny-fixture-kit','Tiny furniture, shield and fixture variants','environment','small-medium',
        'Use remaining Tiny props as controlled variants for camps, supply rooms and existing breakable/scenery objects.',
        'Furniture does not imply new inventory or weapon rules.',
        'Keep readable footprints and explicit collision. Object/multi-tile rail parts must not be ground.',
        [art('tiny',span(60,131),'Unallocated Tiny prop/equipment pieces')], remainder=True)
    add('micro-wall-kit','Micro-scale walls, passages and surface alternatives','environment','medium-high',
        'A complete 8px environment option could match Micro actors more closely than the current mixed 16px surfaces.',
        'This would be an optional native palette using existing terrain IDs, not another gameplay pack loader.',
        'Do not repeat doors/gates as floors. Identify ground versus wall parts before connected-surface integration.',
        [art('micro',span(0,3)+span(16,19)+span(32,68)+span(80,83)+span(96,98)+span(103,105)+span(144,159),'Micro architecture/water parts','assembly')], remainder=True)
    add('micro-prop-kit','Micro props, furnishings and remaining accents','environment','small-medium',
        'Review the remaining tiny equipment, foliage and furnishing motifs for existing rooms and UI identities.',
        'No new full actor body is hiding in the remaining Micro cells.',
        'Respect the excluded flask and ring interpretation. Comment on exact motif readings before binding.',
        [art('micro',span(0,159),'Unallocated Micro motifs')], remainder=True)
    add('cave-portal-kit','Cave portals, gates and rail assemblies','environment','medium-high',
        'Group remaining cave entrances, doors, closed/open gates and abandoned rail parts into matched sets.',
        'Gates and decorative rails already exist. Moving trains/carts remain outside the proposal.',
        'These are assemblies/props; never floor textures. Match open and closed art to the same actual collision state.',
        [art('caves',span(300,521),'Remaining cave assemblies','assembly')], remainder=True)
    add('cave-material-variants','Remaining cave material variants','environment','medium',
        'Keep extra cave wall/ground variants as a material pool for approved biome visual improvements.',
        'Current connectivity already works; this supplies alternatives rather than missing mechanics.',
        'Approve coherent material families, not random cell mixing. Each candidate still needs its actual ground/wall role checked.',
        [art('caves',span(0,521),'Unallocated cave material/prop parts','assembly')], remainder=True)
    add('rpg-open-ground-kit','RPG open-ground transitions and borders','environment','medium-high',
        'Assemble the remaining dirt, stone and grass patches as coherent ground families for existing room palettes.',
        'Region transitions have many corner variants; they are not new mechanics.',
        'Only tested ground-family pieces may become floor. Reject discontinuities and preserve tile-level collision independently.',
        [art('rpg',block(57,range(0,25),range(0,15)),'Remaining ground/transition families','assembly')], remainder=True)
    add('rpg-architecture-kit','RPG architecture, stairs and support variants','environment','medium-high',
        'A broad architectural library for improved room entrances, platforms, rails and wall depth.',
        'This is a kit proposal for existing rooms; no new vertical navigation or multi-level physics is implied.',
        'Parts must be assembled coherently, aligned at the game footprint and kept out of floor sampling.',
        [art('rpg',block(57,range(0,25),range(15,57)),'Remaining furniture/architecture assemblies','assembly')], remainder=True)
    add('theme-colour-frames','Alternative colour frame and panel skins','presentation','medium',
        'Choose a coherent neutral, green, warm or teal skin from the large remaining panel/border families.',
        'Hundreds of cells are frame pieces and colours, not hundreds of new systems.',
        'Select complete corners/edges/centres. Preserve uncluttered UI, legibility, scalable borders and tiny-size fallback.',
        [art('rpg',span(1425,1766),'Panel/border colour families','assembly')], remainder=True)
    add('onebit-architecture','1-Bit architectural counterpart pool','environment','medium-high',
        'Use unallocated wall, doorway, window, ladder and furniture parts as counterparts to approved coloured kits.',
        'Standalone loads only this sheet; it must not silently depend on Tiny or RPG images.',
        'Approve exact counterparts in comments/preferred cells. Do not equate a doorway, wall or furniture cell with ground.',
        [art('onebit',block(49,range(0,16),range(0,24))+block(49,range(16,22),range(0,16)),'Remaining architecture/fixtures','assembly')], remainder=True)
    add('onebit-symbol-library','Remaining 1-Bit item, cue and label library','presentation','small-medium',
        'Retain spare icons and lettering for future approved boon, UI, item and encounter identities.',
        'An unused symbol is not a demand for a new mechanic. Every later binding must have a specific semantic target.',
        'This pool includes ambiguous motifs and variants; choose exact cells with comments rather than blanket-assigning them.',
        [art('onebit',span(0,1077),'Remaining symbols/figure variants','icon')], remainder=True)
    # Explicit counterparts where the shapes/materials plausibly support the
    # same kit. Monochrome families intentionally share materials; these are
    # assembly candidates, never a claim of finished terrain or unique palettes.
    counterparts = {
        'geomancer-class': art('onebit',[466],'Unused robed figure candidate','actor'),
        'sandstone-kit': art('onebit',[18,19,20,67,68,69,116,117,118],'Shared masonry/arch assembly candidates','assembly'),
        'timber-mine-kit': art('onebit',[120,147,148,149,209,210,211],'Posts, fences and frame parts; no wood colour','assembly'),
        'observatory-mosaic': art('onebit',[4,17,66,341],'Geometric material samples; assemble, do not treat every sample as a full floor','assembly'),
        'forge-masonry': art('onebit',[17,18,19,20,147,148,149],'Shared masonry and bars; monochrome counterpart','assembly'),
        'ruin-masonry-kit': art('onebit',[18,19,20,67,68,69],'Shared masonry family, not a distinct new colour palette','wall'),
        'chapel-windows': art('onebit',[110,201,246,248],'Window/barred-opening alternatives','assembly'),
        'faction-banners': art('onebit',[408,459],'Flag-like counterparts; heraldry needs symbol choices'),
        'camp-furniture': art('onebit',[396,404,405,452],'Table/cabinet/seat-like alternatives; review silhouettes'),
        'workshop-fixtures': art('onebit',[400,401,402,404,405,451],'Hearth/cabinet/workshop forms'),
        'micro-wall-kit': art('onebit',[9,10,11,12,13,14,15,154,155,156,157,158,159,160,161],'Unallocated frame/door material counterparts','assembly'),
        'tiny-dungeon-kit': art('onebit',[17,18,19,20,67,68,69,294,295],'Masonry/stairs counterpart pool','assembly'),
        'theme-colour-frames': art('onebit',[748,800,801,802,898,899,900],'Shared border geometry; colour variants have no 1-Bit equivalent','assembly'),
    }
    for card in cards:
        if card['id'] in counterparts:
            counterparts[card['id']]['counterpart'] = True
            card['groups'].append(counterparts[card['id']])
    return cards
