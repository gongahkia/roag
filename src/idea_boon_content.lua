-- Approved surplus-art boon pass. Data only; ordered merges preserve every
-- earlier ID and the pool frozen into an existing run.
local I={order={},definitions={},cues={}}
local tiers={'common','uncommon','rare','legendary'}
local function add(slug,name,description,icon,motif,triggers,predicate,effects,powers,unit)
    local id='core:boon/'..slug
    local parameters={}
    for n,tier in ipairs(tiers)do parameters[tier]={power=powers[n],chance=0}end
    I.order[#I.order+1]=id
    I.definitions[id]={name=name,description=description,tags={'ideas'},
        triggers=triggers,scope={kind='owner'},predicate=predicate,
        effects=effects,parameters=parameters,stack_policy='add_power',unit=unit,
        art={index=icon,motif=motif}}
end
local owner={op='source_owner'}
local food={all={owner,{op='supply_kind',value='food'}}}

add('beggars_cap',"Beggar's Cap",
    'Collecting gold at half health or lower grants a one-hit ward for 300 world time.',
    33,'broad travelling hat',{'CurrencyCollected'},
    {all={owner,{op='owner_hp_percent',max=50}}},
    {{kind='ward',duration=300}},{2,3,4,6},'damage absorbed on one hit')
add('overflow_chalice','Overflow Chalice',
    'Wasted food or health healing becomes a one-hit ward for 400 world time, capped by your power. Replaces a smaller ward.',
    824,'prize cup',{'SupplyCollected'},{all={owner,{op='supply_excess'}}},
    {{kind='ward',duration=400,scale='supply_excess'}},{2,3,4,6},'maximum captured healing')
add('road_flask','Road Flask',
    'Eating food grants action speed for 300 world time. More food refreshes the duration.',
    965,'travelling chalice',{'SupplyCollected'},food,
    {{kind='speed_boost',duration=300}},{10,15,20,25},'temporary speed')
add('field_dressing','Field Dressing',
    'A health pickup removes Burning and restores additional health. Food does not trigger it.',
    1052,'crossed bandage',{'SupplyCollected'},
    {all={owner,{op='supply_kind',value='health'}}},
    {{kind='cleanse_owner',status='burning'},{kind='heal_owner'}},
    {1,2,3,4},'additional health')
add('ration_reserve','Ration Reserve',
    'Eating food stores one recovery charge for 600 world time. Your next Wait spends it to heal; charges do not accumulate.',
    817,'wrapped ration',{'SupplyCollected','Waited'},
    {any={{all={food,{op='event_kind',value='SupplyCollected'}}},
        {all={owner,{op='event_kind',value='Waited'},{op='primed'}}}}},
    {{kind='prime',duration=600,when={op='event_kind',value='SupplyCollected'}},
        {kind='heal_owner',consume_prime=true,when={op='event_kind',value='Waited'}}},
    {1,2,3,4},'health from the next charged Wait')
add('brokers_seal',"Broker's Seal",
    'A paid chest refunds some gold after purchase, capped by its price. You must afford the full price first.',
    489,'star seal',{'ChestClaimed'},
    {all={owner,{op='event_amount',field='price',min=1}}},
    {{kind='refund_purchase'}},{1,2,3,4},'maximum gold refunded')
add('funeral_coin','Funeral Coin',
    'Your credited kills add gold to their normal currency drop. Summoned or revived reward-exempt foes give no bonus.',
    773,'stamped coin',{'EntityKilled'},
    {all={owner,{op='target_hostile'},{op='rewarded_drop'}}},
    {{kind='enrich_drop'}},{1,2,3,4},'extra dropped gold')

add('pilgrims_vow',"Pilgrim's Vow",
    'While aiming an immediate attack, P / LB toggles a vow: pay 2 HP on commit for bonus primary damage before criticals. Must leave at least 1 HP. Cancel is free; movement and utility cannot pay.',
    318,'hooded pilgrim',{},nil,{},
    {3,4,6,8},'bonus primary damage for the 2 HP vow')
I.definitions['core:boon/pilgrims_vow'].attack_payment={hp=2}

local hit={all={{op='source_owner'},{op='target_hostile'},
    {op='direct_attack'},{op='positive'}}}
local function face(n)return {op='roll_face',value=n}end
local once_per_attack={all={{any={face(1),face(4),face(5),face(6)}},
    {op='unique_action'}}}
local dice_hit={all={hit,{any={face(3),once_per_attack}}}}
add('fateful_dice','Fateful Dice',
    'Each attack rolls one of 8 equal faces: 1 heal, 2 heavy, 3 Burning, 4 ward, 5 arc, 6 burst, skull executes regular foes, cross misses. Bonuses need a hit. Utility never rolls; bosses/specials resist skull.',
    728,'one-pip die',{'DamageTaken'},
    dice_hit,
    {{kind='heal_owner',when=face(1)},
        {kind='apply_status',status='burning',interval=100,ticks=3,when=face(3)},
        {kind='ward',duration=200,when=face(4)},
        {kind='damage_nearest',radius=3,damage_type='lightning',when=face(5)},
        {kind='area',radius=1,damage_type='explosion',when=face(6)}},
    {1,2,3,4},'rolled bonus/heal/ward/tick/burst power')
I.faces={'1: heal','2: heavy','3: Burning','4: ward','5: arc','6: burst','SKULL','MISS'}
for n=1,8 do
    I.cues[n]={id='core:ui/fate_'..n..'_icon',index=n<=6 and 727+n or n==7 and 727 or 726,
        label=I.faces[n]}
end

local function pattern(slug,name,text,icon,motif,shape,trigger,extra,delay)
    local clauses={{op='source_owner'}}
    if trigger=='hit' then
        clauses[#clauses+1]={op='target_hostile'}
        clauses[#clauses+1]={op='direct_attack'}
        clauses[#clauses+1]={op='positive'}
    elseif trigger=='kill' then clauses[#clauses+1]={op='target_hostile'}
    elseif trigger=='dash' then clauses[#clauses+1]={op='actual_dash'}end
    if extra then clauses[#clauses+1]=extra end
    local effect={kind=delay and 'echo' or 'pattern',pattern=shape,
        damage_type='physical',delay=delay}
    add(slug,name,text,icon,motif,
        {({hit='DamageTaken',kill='EntityKilled',dash='Moved',wait='Waited'})[trigger]},
        {all=clauses},{effect},{1,2,3,4},'pattern damage per cell before defense')
    local def=I.definitions['core:boon/'..slug]
    def.tags={'ideas','spatial'}
    def.pattern_preview={trigger=trigger,axis=extra and extra.op=='pattern_axis' and extra.value or nil,
        special=extra and extra.op=='special_attack' or nil}
end
pattern('linebreaker_pawn',"Linebreaker's Pawn",
    'Positive primary hits send a two-cell lance beyond the victim, away from the strike origin. Walls and closed doors stop it.',
    1021,'chess pawn','lance','hit')
pattern('rooks_wake',"Rook's Wake",
    'Cardinal primary hits send two cells in both perpendicular directions from the victim. It rewards lining up a row of foes.',
    1023,'chess rook','rook','hit',{op='pattern_axis',value='cardinal'})
pattern('bishops_wake',"Bishop's Wake",
    'Diagonal primary hits release four diagonal two-cell rays from the victim. Stone corners block each ray.',
    1024,'chess bishop','bishop','hit',{op='pattern_axis',value='diagonal'})
pattern('knights_gambit',"Knight's Gambit",
    'Positive special-attack hits strike the eight L-shaped endpoints around the victim. At least one clear two-leg route is required; adjacent cells are spared.',
    1027,'chess knight','knight','hit',{op='special_attack'})
pattern('hollow_crown','Hollow Crown',
    'Credited hostile kills strike a hollow ring two tiles from the victim. The inner ring is spared, and walls block the outer ring.',
    816,'crowned card','crown','kill')
pattern('crosswind','Crosswind',
    'Wait sends four cardinal rays two cells from your current tile. It spends ordinary Wait time, hits no diagonals and stops at walls.',
    584,'four-point cross','crosswind','wait')
pattern('fan_of_blades','Fan of Blades',
    'Positive special-attack hits strike a three-cell forward fan beyond the victim. Walls and stone corners block it.',
    39,'crossed blades','fan','hit',{op='special_attack'})
pattern('backtrail_mantle','Backtrail Mantle',
    'A committed Dash locks a five-cell cross at its starting tile, striking after 150 world time. Utility movement and beacon returns do not trigger it.',
    87,'travelling mantle','backtrail','dash',nil,150)

local regular_hit={all={hit,{op='regular_target'}}}
add('grasping_hand','Grasping Hand',
    'Positive special hits on regular foes in your row or column deal bonus damage, then pull surviving victims one cardinal cell toward you. Occupied cells, exits and walls stop the pull.',
    1054,'open grasping hand',{'DamageTaken'},
    {all={regular_hit,{op='special_attack'},{op='owner_axis',value='cardinal'}}},
    {{kind='damage_target',damage_type='physical'},{kind='displace_target',mode='pull'}},
    {1,2,3,4},'bonus damage before one-cell pull')
add('anchor_maul','Anchor Maul',
    'Basic hits against cardinal-adjacent regular foes push them one cell. If the push is blocked, deal collision damage instead. A successful push interrupts a windup; bosses are exempt.',
    1055,'curled impact hand',{'DamageTaken'},
    {all={regular_hit,{op='basic_attack'},{op='owner_axis',value='cardinal'},
        {op='event_distance',min=1,max=1}}},
    {{kind='displace_target',mode='push',collision_damage=true,damage_type='physical'}},
    {1,2,3,4},'collision damage when the one-cell push is blocked')
add('hourglass_clasp','Hourglass Clasp',
    'A positive primary hit briefly stuns the first surviving regular foe. One trigger per attack, then a 400-time cooldown. Stun caps at 100 time; it never gives a guaranteed player reaction action.',
    1072,'small hourglass',{'DamageTaken'},
    {all={regular_hit,{op='cooldown_ready'},{op='unique_action'}}},
    {{kind='stun_target',cap=100,cooldown=400}},
    {10,20,30,40},'stun time, capped at 100; cooldown 400')
add('ward_ledger','Ward Ledger',
    'Wait while hurt spends active one-hit wards to restore health, up to your power. Consumed wards lose unused capacity. Empty wards and full health cannot trigger it.',
    854,'ace medical cross',{'Waited'},
    {all={owner,{op='owner_missing_hp'},{op='active_ward'}}},
    {{kind='ward_to_heal'}},{1,2,3,4},'maximum health recovered by spending wards')
add('cinder_stopper','Cinder Stopper',
    'A primary hit on an already Burning foe consumes its remaining Burning for one immediate fire hit, capped by your power. The queued ticks are removed; normal defenses still apply.',
    916,'sealed fire flask',{'DamageTaken'},
    {all={hit,{op='burning_target'}}},
    {{kind='consume_burning',damage_type='fire'}},
    {3,5,7,10},'maximum captured remaining Burning damage')
add('scavengers_hook',"Scavenger's Hook",
    'Wait gathers pickups from up to four adjacent cardinal tiles, limited by power, in north/east/south/west order. Requires clear visible ground; full-health food and carried drops stay put. Normal pickup effects apply.',
    1053,'curved gathering hook',{'Waited'},owner,
    {{kind='gather_pickups'}},{1,2,3,4},'adjacent pickup tiles gathered per Wait, capped at four')

local function weapon(slug,name,text,icon,motif,class,ability,powers)
    add(slug,name,text,icon,motif,{},nil,{},powers or {1,2,3,4},
        'bonus primary damage before criticals and defense')
    local d=I.definitions['core:boon/'..slug]
    d.tags={'ideas','transformation','offense'}
    d.eligible_classes={'core:actor/'..class}
    d.modifiers={{ability_id='core:ability/'..ability,kind='weapon_form',form=slug},
        {ability_id='core:ability/'..ability,kind='add_damage'}}
end
weapon('pommel_return','Pommel Return',
    'Adventurer: aimed F also strikes the clear cell directly behind you. Both sides share one primary batch. Bump remains forward-only; F gains bonus damage.',
    1026,'short chess pommel','adventurer','basic_attack')
weapon('needle_flight','Needle Flight',
    'Ranger: aimed F pierces creatures along its four-cell clear line. Existing Splitflight branches still work. Bump keeps first-contact behavior; F gains bonus damage.',
    1063,'paired flight feathers','ranger','ranger_shot')
weapon('hollow_sigil','Hollow Sigil',
    'Arcanist: a fully placed R fire sigil spares its center and strengthens the surrounding cells. Expanding Sigil keeps its outer ring. Short blocked casts retain their normal travel cells.',
    930,'nested hollow square sigil','arcanist','arcanist_sigil',{2,3,4,6})
weapon('backswing','Backswing',
    'Berserker: R cleave also strikes its three clear rear neighbors, completing the surrounding arc on open ground. Normal deliberate recovery remains; R gains bonus damage.',
    40,'curved crossed blades','berserker','berserker_cleave')
weapon('distant_bastion','Distant Bastion',
    'Sentinel: R instead strikes the four two-step cardinal tiles and four diagonal neighbors. Cardinal-adjacent tiles are spared. Walls and corners block the stronger pulse.',
    585,'projecting round bastion','sentinel','sentinel_pulse')
weapon('volatile_vial','Volatile Vial',
    'Alchemist: F splashes a one-cell cardinal cross at its first creature contact. Empty shots keep the normal line. Clear travel cells and normal recovery remain; F gains bonus damage.',
    194,'stoppered square vial','alchemist','alchemist_vial')
weapon('split_thrust','Split Thrust',
    'Duelist: aimed F keeps its first step, then forks into two clear side endpoints instead of the straight second cell. Walls and corners stop each branch. Bump stays narrow; F gains bonus damage.',
    1062,'paired pointed projectiles','duelist','duelist_thrust')

add('threefold_remedy','Threefold Remedy',
    'Each disjoint group of three different foes damaged by one primary attack restores health. Repeated damage to one victim cannot count again in that attack chain.',
    953,'three-pip remedy card',{'DamageTaken'},
    {all={hit,{op='distinct_chain_group',size=3}}},
    {{kind='heal_owner'}},{1,2,3,4},'health per three distinct primary victims')
add('twin_seals','Twin Seals',
    'Two different credited, reward-eligible kills in one causal chain grant a one-hit ward for 300 world time. Summoned and revived reward-exempt foes cannot count.',
    903,'two diamond seals',{'EntityKilled'},
    {all={owner,{op='target_hostile'},{op='rewarded_drop'},
        {op='distinct_chain_group',size=2}}},
    {{kind='ward',duration=300}},{2,3,4,6},'damage absorbed by the one-hit ward')
add('fourfold_ledger','Fourfold Ledger',
    'Each group of four different other triggered boons in one causal chain restores health. Repeated activations of one boon do not count again; passive weapon forms are not triggered activations.',
    856,'four-club ledger card',{'BoonActivated'},
    {all={owner,{op='other_boon'},{op='distinct_chain_group',size=4,field='boon_id'}}},
    {{kind='heal_owner'}},{2,3,4,6},'health per four different triggered boons')
add('stone_backstop','Stone Backstop',
    'Positive primary hits gain bonus damage when the victim is in your row or column and a wall, closed door or blocking object is immediately behind it, away from you.',
    1077,'rigid backstop frame',{'DamageTaken'},
    {all={hit,{op='back_to_wall'}}},
    {{kind='damage_target',damage_type='stone'}},{2,3,4,6},'bonus pinned-victim damage before defense')
add('borrowed_guard','Borrowed Guard',
    'A fully blocked primary attack against a surviving regular foe grants a one-hit ward. One trigger per attack, with a 300-world-time cooldown; bosses and exempt foes do not qualify.',
    1066,'blocked strike seal',{'DamageBlocked'},
    {all={owner,{op='target_hostile'},{op='direct_attack'},{op='regular_target'},
        {op='cooldown_ready'},{op='unique_action'}}},
    {{kind='ward',duration=300}},{1,2,3,4},'damage absorbed by the one-hit ward')
add('long_wick','Long Wick',
    'Your Burning applications add remaining ticks, capped at eight total. Tick power, interval and the already queued next tick do not change. More copies add ticks up to that cap.',
    488,'long burning candle',{'StatusApplied'},
    {all={owner,{op='event_status',value='burning'},{op='target_entity_hostile'}}},
    {{kind='extend_burning',cap=8}},{1,2,3,4},'additional Burning ticks; eight total maximum')
add('passing_ember','Passing Ember',
    'Credited reward-eligible kills of Burning foes ignite the nearest other foe within three clear tiles for two ticks. Ordinary ancestry guards prevent an endless automatic fire chain.',
    195,'lit passing fuse',{'EntityKilled'},
    {all={owner,{op='target_hostile'},{op='burning_target'},{op='rewarded_drop'}}},
    {{kind='apply_status',target='nearest',radius=3,clear_path=true,
        status='burning',interval=100,ticks=2}},
    {1,2,3,4},'passed Burning damage per tick before defense')
add('purging_seal','Purging Seal',
    'Actually cleansing Burning from you releases a one-cell fire burst around you. Empty cleansing attempts and consuming a foe\'s Burning cannot trigger it.',
    1065,'cleansing cross seal',{'StatusCleansed'},
    {all={{op='target_owner'},{op='event_status',value='burning'}}},
    {{kind='area',radius=1,damage_type='fire'}},{1,2,3,4},'cleansing burst damage per cell before defense')

return I
