-- Presentation-only slots shared by Studio validation and the game renderer.
local Biomes=require('src.biomes')
local Boons=require('src.boon_content')
local Content=require('src.content')
local Classes=require('src.classes')
local Squads=require('src.enemy_squads')
local IdeaEnemies=require('src.idea_enemies')
local Interactions=require('src.idea_interaction_content')
local V={bindings={}}
local function add(id) V.bindings[id]=true end
for name in pairs(require('src.idea_terrain').art)do add('core:terrain/'..name)end
local Env=require('src.idea_environment_content')
for _,name in ipairs(Env.material_order)do add(Env.material_id(name))end
for name in pairs(Env.props)do add(Env.prop_id(name))end
for name in pairs(Env.symbols)do add(Env.symbol_id(name))end
for kind in pairs(Interactions.art)do add(Interactions.binding(kind))end
add('core:object/geomancer_bulwark')
add('core:object/bound_spirit')
add('core:object/bolt_sentry')
add('core:ui/storm_charge_effect')
for _,cue in ipairs(require('src.idea_boon_content').cues)do add(cue.id)end
add('core:object/return_beacon')
local function slug(id) return id:match('/([^/]+)$') end
for _,id in ipairs({'door_closed','door_open','stairs_down','stairs_up','exit','chest_open'})do
    add('core:object/'..id)
end
for _,id in ipairs({'review_secret_clue','review_key','review_vault_top','review_vault_bottom',
    'review_gate_top','review_gate_bottom','review_gate_tiny',
    'review_plate_up','review_plate_down','review_plate_tiny_up',
    'review_plate_tiny_down','review_ore','review_ore_depleted',
    'review_brazier_unlit','review_brazier_lit','review_ward',
    'review_mushroom_1','review_mushroom_2','review_overgrowth_1',
    'review_overgrowth_2','review_rail_top_left','review_rail_top_right',
    'review_rail_middle_left','review_rail_middle_right',
    'review_rail_bottom_left','review_rail_bottom_right',
    'review_pillar_top','review_pillar_middle','review_pillar_bottom'}) do
    add('core:object/'..id)
end
for _,id in ipairs({'bramble_bed','sticky_bog','glazed_ice','barricade',
    'obscuring_mist','resonant_crystal'})do add('core:terrain/'..id)end
add('core:ui/burning_effect');add('core:ui/block_effect')
for name in pairs(Squads.cues) do add('core:ui/'..name) end
for id in pairs(IdeaEnemies.symbols or {})do add(id)end
for id in pairs(IdeaEnemies.objects or {})do add(id)end
for _,id in ipairs({'attack_center_effect','attack_impact_effect',
    'weapon_sword_icon','weapon_bow_icon','weapon_spell_icon',
    'weapon_dagger_icon','weapon_hook_icon'}) do add('core:ui/'..id) end
-- One editable image per printable ASCII character. The numeric suffix keeps
-- upper and lower case distinct while satisfying Studio's lowercase ID rule.
local glyph_bindings={}
function V.glyph_binding(character)
    return glyph_bindings[character]
end
for byte=32,126 do
    local id=('core:glyph/ascii_%03d'):format(byte)
    glyph_bindings[string.char(byte)]=id
    add(id)
end
for _,slot in ipairs({'hud_panel','hp_icon','xp_icon','currency_icon',
    'title_background','pause_background','bitmap_font','hit_effect',
    'boon_effect'}) do add('core:ui/'..slot) end
local shapes={'base','center','edge','corner','inner_corner','edge_n','edge_e',
    'edge_s','edge_w','corner_nw','corner_ne','corner_se','corner_sw',
    'inner_corner_nw','inner_corner_ne','inner_corner_se','inner_corner_sw',
    'horizontal','vertical','isolated','end_n','end_e','end_s','end_w'}
for _,id in ipairs(Classes.order) do
    add(id);add(id..'/attack');add(id..'/hurt')
end
add('core:drop/currency')
add('core:drop/currency/detail')
add('core:drop/food')
add('core:drop/health')
add('core:chest/standard')
add('core:chest/free_elite')
local enemy_ids={['core:enemy/thorn_warden']=true,
    ['core:enemy/extraction_pursuer']=true,['core:enemy/hollow_regent']=true}
for _,biome_id in ipairs(Biomes.order) do
    local biome=Biomes.get(biome_id)
    add('core:background/'..slug(biome_id))
    enemy_ids[biome.elite_id]=true
    for _,enemy_id in ipairs(biome.enemy_ids) do enemy_ids[enemy_id]=true end
    for _,tile in ipairs({'floor','wall','water'}) do
        for _,shape in ipairs(shapes) do
            add('core:terrain/'..slug(biome_id)..'/'..tile..'/'..shape)
        end
    end
end
for id in pairs(Squads.roles) do enemy_ids[id]=true end
for id in pairs(IdeaEnemies.roles) do enemy_ids[id]=true end
for id in pairs(enemy_ids) do
    assert(Content.get_enemy(id),'unknown visual enemy slot: '..id)
    add(id);add(id..'/attack');add(id..'/hurt')
end
for _,id in ipairs(Boons.order) do
    add(id..'/icon');add(id..'/detail')
end
function V.pose(id,current,group)
    local hurt,attack=false,false
    for _,event in ipairs(group or (current and {current}) or {}) do
        if event.kind=='DamageTaken' and event.target_id==id and
            (event.actual_loss or 0)>0 then hurt=true end
        if (event.kind=='AttackPerformed' or event.kind=='WindupStarted') and
            event.source_id==id then attack=true end
    end
    return hurt and 'hurt' or attack and 'attack' or nil
end
-- The authored base image faces right. Mirroring only changes presentation;
-- vertical movement retains the last horizontal facing.
function V.facing(current,dx)
    if dx==-1 or dx==1 then return dx end
    return current or 1
end
-- New fragments share the approved skeleton, at a smaller whole-pixel scale.
function V.enemy_size(id,tile)
    if id=='core:enemy/bone_fragment'then
        return math.min(tile,math.max(16,math.floor(tile*2/3/16)*16))
    end
    return tile
end

function V.horizontal_draw(x,width,image_width,facing)
    local scale=width/image_width
    if facing==-1 then return x+width,-scale end
    return x,scale
end
local symbol={floor='.',wall='#',water='~'}
function V.terrain_shape(map,x,y,kind)
    local mark=assert(symbol[kind],'invalid terrain kind')
    local function same(dx,dy)
        local yy=y+dy;local row=map.rows[yy]
        return row and row:sub(x+dx,x+dx)==mark or false
    end
    local n,e,s,w=same(0,-1),same(1,0),same(0,1),same(-1,0)
    local count=(n and 1 or 0)+(e and 1 or 0)+(s and 1 or 0)+(w and 1 or 0)
    if count==4 then
        for _,corner in ipairs({{'nw',-1,-1},{'ne',1,-1},
            {'se',1,1},{'sw',-1,1}}) do
            if not same(corner[2],corner[3]) then
                return 'inner_corner_'..corner[1]
            end
        end
        return 'center'
    elseif count==3 then
        if not n then return 'edge_n' end
        if not e then return 'edge_e' end
        if not s then return 'edge_s' end
        return 'edge_w'
    elseif count==2 then
        if n and s then return 'vertical' end
        if e and w then return 'horizontal' end
        if n and e then return 'corner_sw' end
        if e and s then return 'corner_nw' end
        if s and w then return 'corner_ne' end
        return 'corner_se'
    elseif count==1 then
        if n then return 'end_s' end
        if e then return 'end_w' end
        if s then return 'end_n' end
        return 'end_e'
    end
    return 'isolated'
end
function V.terrain_bindings(biome_id,kind,shape)
    local base='core:terrain/'..slug(biome_id or Biomes.order[1])..'/'..kind..'/'
    local result={base..shape}
    local generic=shape:match('^inner_corner_') and 'inner_corner' or
        shape:match('^corner_') and 'corner' or
        shape:match('^edge_') and 'edge' or nil
    if generic then result[#result+1]=base..generic end
    if shape~='center' then result[#result+1]=base..'center' end
    result[#result+1]=base..'base'
    return result
end
-- Legacy motif packs retain their subdued opacity. Version-2 tiled packs and
-- published Paint tiles cover the surface; fog dimming is presentation only.
function V.terrain_alpha(kind,source,remembered,mode)
    local alpha=1
    if source=='generated' and mode~='tiled' then
        if kind=='floor' then alpha=0.18
        elseif kind=='wall' then alpha=0.72
        elseif kind=='water' then alpha=0.58 end
    end
    return remembered and alpha*0.38 or alpha
end
-- Legacy motif packs used sparse stamps. Tiled packs and published Paint
-- terrain are literal full-coverage surfaces.
function V.terrain_draws_at(kind,source,x,y,mode)
    if source~='generated' or mode=='tiled' then return true end
    local period=kind=='wall' and 4 or kind=='water' and 3 or 5
    return (x+2*y)%period==0
end
function V.background_alpha(source,mode)
    if source=='generated' and mode=='tiled' then return 0 end
    return source=='generated' and 0.16 or 1
end
-- Connected wall faces are presentation geometry inside the owning tile.
-- Unknown neighbors never disclose a hidden passage through a bevel.
function V.wall_edges(map,x,y,known)
    local edges={}
    for _,d in ipairs({{'n',0,-1},{'e',1,0},{'s',0,1},{'w',-1,0}})do
        local xx,yy=x+d[2],y+d[3]
        local row=map.rows[yy]
        local cell=row and row:sub(xx,xx)or ''
        edges[d[1]]=cell~='' and cell~='#' and cell~='B' and
            (not known or known(xx,yy))or false
    end
    return edges
end
return V
