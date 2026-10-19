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

return I
