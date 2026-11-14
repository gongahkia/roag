-- Bounded Tranche 04 constants, not a tile scripting language.
local C={size=6,route_size=4,ordinary_weight=31,guard=2,heal=4,boon_slots=2,shuffle_uses=1,
    directions={{0,-1,'N'},{1,0,'E'},{0,1,'S'},{-1,0,'W'}},types={},ordinary_order={},finite={
        'mend','mend','sweep','rhythm','bloom'}}
for _,kind in ipairs({'move','strike','dash'})do
    for i,d in ipairs(C.directions)do
        local key=kind..i
        C.ordinary_order[#C.ordinary_order+1]=key
        C.types[key]={key=key,name=({move='Move',strike='Strike',dash='Dash'})[kind]..' '..d[3],
            short=({move='GO',strike='HIT',dash='DASH'})[kind],kind=kind,dir=i,dx=d[1],dy=d[2],
            description=kind=='move' and 'Step one tile. Blocked movement is invalid.' or
                kind=='strike' and 'Sword Strike in the printed direction; an empty attack is a legal miss.' or
                'Dash in the printed direction. Stops at blockers; needs positive travel.'}
    end
end
local function tile(key,name,short,description,finite,harmful)
    C.types[key]={key=key,name=name,short=short,kind=key,description=description,finite=finite,harmful=harmful}
    if not finite then C.ordinary_order[#C.ordinary_order+1]=key end
end
tile('guard','Guard','GUARD','Refresh separate protection to 2. Armour and existing wards apply first.')
tile('wound','Wound','WOUND','Lose 2 HP immediately, ignoring armour and shields. Can kill.',false,true)
tile('dull','Dull Edge','DULL','Next primary attack loses 1 damage. Stacks; spent even on a miss.',false,true)
tile('fury','Enemy Fury','FURY','Each living enemy gains +1 on its next resolved primary attack. Stacks.',false,true)
tile('focus','Focus','FOCUS','Next primary attack gains 1 damage. Stacks; spent even on a miss.')
tile('mend','Mend','MEND','Heal up to 4 HP, then retire. At full HP heals 0 and still costs time.',true)
tile('sweep','Sweep','SWEEP','One-use directional special sweep. Retires even on a miss.',true)
tile('rhythm','Rhythm of Steel','RHYTH','Equip common Rhythm of Steel in a free slot. Moving primes a bonus hit.',true)
tile('bloom','Detonation Bloom','BLOOM','Equip common Detonation Bloom in a free slot. Credited kills burst.',true)
C.types.rhythm.boon_id='core:boon/rhythm_of_steel'
C.types.bloom.boon_id='core:boon/detonation_bloom'
-- Same arena/enemy definitions. One guard begins nearer, so a harmful bridge
-- can buy a second hit promptly; no global stats or timings change.
C.fights={
    {enemies={{'core:enemy/mossbound_guard',4,5},{'core:enemy/mossbound_guard',5,7},{'core:enemy/thornspitter',8,3}}},
    require('src.card_trial_content').fights[2],
}
return C
