-- Selective ink accents over the fixed 1-Bit artwork. Presentation only.
local P={
    chest={.77,.62,.42},spent={.56,.44,.31},food={.78,.61,.43},
    gold={.88,.75,.46},enemy={.61,.70,.78},
    hostile={.94,.32,.30},friendly={.47,.82,.58},blocked={.48,.48,.48},
}
local items={ration_cache=true,cleanse_flask=true,escape_scroll=true,
    return_token=true,return_marker=true,offering_font=true,charm_well=true,
    attunement_forge=true,travelling_trader=true,archive_contract=true}
local hues={{1,0,0},{1,1,0},{0,1,0},{0,1,1},{0,0,1},{1,0,1}}
function P.binding(id)
    if type(id)~='string' then return end
    if id:match('^core:boon/') or id:match('^core:drop/') or
        id=='core:ui/currency_icon' or id=='core:ui/xp_icon' or
        id=='core:ui/boon_effect' or id=='core:ui/cue_boon_effect' or
        id=='core:ui/status_heal_icon' then
        return id=='core:drop/food' and P.food or P.gold
    end
    if id=='core:object/chest_open' then return P.spent end
    if id:match('^core:chest/') or id=='core:object/idea_cache_dormant' then return P.chest end
    if items[id:match('^core:object/interaction_(.+)$')] then return P.gold end
    if id:match('^core:enemy/') then return P.enemy end
end
function P.resolve(assets,id)
    if not assets then return end
    local handle=assets:resolve(id)
    local ink=P.binding(id)
    if not handle or not ink then return handle end
    -- The cache belongs to this resource generation. Shared source handles are
    -- never tinted in place: the same sheet cell may be an actor and an icon.
    assets.ink_handles=assets.ink_handles or {}
    local cached=assets.ink_handles[id]
    if not cached or cached.base~=handle then
        cached=setmetatable({ink=ink,base=handle},{__index=handle})
        assets.ink_handles[id]=cached
    end
    return cached
end
function P.attack(player_id,event,source)
    if event.support_kind or event.support then return P.enemy end
    if event.terrain_heat or event.ember_pulse or event.fixture then return P.hostile end
    if event.source_id==player_id or event.owner_id==player_id or event.companion or
        source and source.owner_id==player_id then return P.friendly end
    return P.hostile
end
function P.preview(kind,status)
    if (kind=='dash' or kind=='use_utility') and status~='open' then return P.blocked end
    return P.friendly
end
function P.damage(hit)
    if hit.amount==0 then return P.enemy end -- A blocked zero stays calm.
    -- One smooth rainbow turn over a hit's lifetime; a gentle brightness pulse,
    -- no blinking, wall clock or gameplay randomness. Critical ! stays intact.
    local phase=((hit.x or 0)*7+(hit.y or 0)*11+hit.amount*3)%31/31
    local hue=((hit.age or 0)*1.2+phase)%1*6
    local value=.9+.1*math.sin((hit.age or 0)*math.pi*4+phase*math.pi*2)
    local chroma=value*.8
    local index=math.floor(hue)
    local a,b=hues[index+1],hues[(index+1)%6+1]
    local fraction=hue-index
    local base=value-chroma
    return {base+chroma*(a[1]+(b[1]-a[1])*fraction),
        base+chroma*(a[2]+(b[2]-a[2])*fraction),
        base+chroma*(a[3]+(b[3]-a[3])*fraction)}
end
return P
