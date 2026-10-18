-- Standalone Kenney 1-Bit presentation. Only symbols.png is read; no Micro or
-- Caves artwork is required. Static poses and representative motifs are explicit.
local Symbols=require('src.kenney_symbols')
local Neighbors=require('src.kenney_terrain') -- pure canonical mask geometry
local Biomes=require('src.biomes')
local Squads=require('src.enemy_squads')
local O={path=Symbols.path,checksum=Symbols.checksum,width=784,height=352,tile=16,
    ink={.9,.9,.9},background={.045,.045,.045},floor_index=5,wall_index=16,
    water_index=268,frame_index=169}
O.actors={
    ['core:actor/adventurer']=468,['core:actor/ranger']=472,
    ['core:actor/arcanist']=467,['core:actor/berserker']=226,
    ['core:actor/sentinel']=324,['core:actor/alchemist']=469,
    ['core:actor/shade']=79,['core:actor/hookrunner']=80,
    ['core:actor/trapper']=77,
    ['core:enemy/mossbound_guard']=275,['core:enemy/thornspitter']=171,
    ['core:enemy/ruin_skitter']=459,['core:enemy/spore_herald']=418,
    ['core:enemy/canal_lancer']=26,['core:enemy/cinder_mender']=75,
    ['core:enemy/cinder_bellkeeper']=32,['core:enemy/glass_charger']=410,
    ['core:enemy/crystal_bombardier']=271,['core:enemy/vault_lurker']=31,
    ['core:enemy/mire_binder']=420,['core:enemy/pack_chorister']=461,
    ['core:enemy/gloom_strider']=367,['core:enemy/forge_conjurer']=471,
    ['core:enemy/mossbreaker']=325,['core:enemy/bastion_bulwark']=274,
    ['core:enemy/thorn_warden']=272,['core:enemy/mycelial_matriarch']=419,
    ['core:enemy/canal_lockmaster']=128,['core:enemy/ash_marshal']=43,
    ['core:enemy/prism_curator']=227,['core:enemy/index_keeper']=76,
    ['core:enemy/ember_crucible']=273,['core:enemy/extraction_pursuer']=421,
    ['core:enemy/hollow_regent']=269,
}
for role,index in pairs(Squads.onebit) do O.actors['core:enemy/'..role]=index end
O.objects={
    ['core:drop/currency']=818,['core:drop/food']=238,['core:drop/health']=670,
    ['core:chest/standard']=302,['core:chest/free_elite']=301,
    ['core:object/chest_open']=344,['core:object/door_closed']=548,
    ['core:object/door_open']=443,['core:object/stairs_down']=295,
    ['core:object/stairs_up']=296,['core:object/exit']=451,
    ['core:ui/hp_icon']=530,['core:ui/xp_icon']=706,['core:ui/currency_icon']=818,
    ['core:ui/hit_effect']=618,['core:ui/boon_effect']=616,
    ['core:ui/burning_effect']=567,['core:ui/block_effect']=233,
    ['core:ui/attack_center_effect']=625,
    ['core:ui/attack_impact_effect']=626,
    ['core:ui/weapon_sword_icon']=327,
    ['core:ui/weapon_bow_icon']=328,
    ['core:ui/weapon_spell_icon']=329,
    ['core:ui/weapon_dagger_icon']=331,
    ['core:ui/weapon_hook_icon']=332,
    ['core:object/review_secret_clue']=3,
    ['core:object/review_key']=571,
    ['core:object/review_vault_bottom']=552,
    ['core:object/review_vault_top']=552,
    ['core:object/review_gate_bottom']=607,
    ['core:object/review_gate_top']=607,
    ['core:object/review_plate_up']=111,
    ['core:object/review_plate_down']=112,
    ['core:object/review_gate_tiny']=607,
    ['core:object/review_plate_tiny_up']=111,
    ['core:object/review_plate_tiny_down']=112,
    ['core:object/review_ore']=6,
    ['core:object/review_ore_depleted']=524,
    ['core:object/review_brazier_unlit']=738,
    ['core:object/review_brazier_lit']=739,
    ['core:object/review_ward']=578,
    ['core:object/review_mushroom_1']=103,
    ['core:object/review_mushroom_2']=54,
    ['core:object/review_overgrowth_1']=49,
    ['core:object/review_overgrowth_2']=50,
    ['core:object/review_rail_top_left']=245,
    ['core:object/review_rail_top_right']=245,
    ['core:object/review_rail_middle_left']=294,
    ['core:object/review_rail_middle_right']=294,
    ['core:object/review_rail_bottom_left']=294,
    ['core:object/review_rail_bottom_right']=294,
    ['core:object/review_pillar_top']=542,
    ['core:object/review_pillar_middle']=591,
    ['core:object/review_pillar_bottom']=640,
}
O.masks=Neighbors.masks
O.canonical=Neighbors.canonical
function O.surface_sheet(source)
    local out=source:clone()
    local function sample(index,x,y)
        local ox,oy=Symbols.region(index)
        return source:getPixel(ox+x,oy+y)
    end
    local function composite(index,x,y,light)
        local r,g,b,a=sample(index,x,y)
        return O.background[1]*(1-a)+r*light*a,
            O.background[2]*(1-a)+g*light*a,
            O.background[3]*(1-a)+b*light*a,1
    end
    for group,kind in ipairs({'wall','water','floor'})do
        local masks=kind=='floor' and {255} or O.masks
        for i,mask in ipairs(masks)do
            local slot=kind=='floor' and 94 or (group-1)*47+i-1
            local ox,oy=slot%49*16,math.floor(slot/49)*16
            for y=0,15 do for x=0,15 do
                local index=kind=='wall' and O.wall_index or kind=='water' and
                    O.water_index or O.floor_index
                local light=kind=='wall' and .35 or kind=='water' and .55 or .19
                local r,g,b,a=composite(index,x,y,light)
                local function has(bit)return math.floor(mask/bit)%2==1 end
                local edge=(not has(1) and y<2) or (not has(4) and y>=14) or
                    (not has(8) and x<2) or (not has(2) and x>=14)
                if kind~='floor' and edge then
                    local rr,gg,bb,aa=sample(O.frame_index,x,y)
                    if aa>0 then r,g,b=rr*.86,gg*.86,bb*.86 end
                end
                -- Native frame corners also mark an exposed diagonal pocket.
                for _,c in ipairs({{1,8,128,0,0},{1,2,16,12,0},
                    {4,2,32,12,12},{4,8,64,0,12}})do
                    if kind~='floor' and has(c[1]) and has(c[2]) and not has(c[3])
                        and x>=c[4] and x<c[4]+4 and y>=c[5] and y<c[5]+4 then
                        local sx=c[4]==0 and 3-x or 27-x
                        local sy=c[5]==0 and 3-y or 27-y
                        local rr,gg,bb,aa=sample(O.frame_index,sx,sy)
                        if aa>0 then r,g,b=rr*.86,gg*.86,bb*.86 end
                    end
                end
                out:setPixel(ox+x,oy+y,r,g,b,a)
            end end
        end
    end
    return out
end
function O.new(read,make_texture,make_quad)
    local bytes=read(O.path)
    if not bytes then return nil,'absent'end
    make_quad=make_quad or love.graphics.newQuad
    local symbols=Symbols.new(function(path)assert(path==O.path);return bytes end,
        make_texture,make_quad,{monochrome=true})
    local surface=assert(make_texture(bytes,O.path,'onebit-surfaces'),'unreadable 1-Bit surfaces')
    assert(surface:getWidth()==784 and surface:getHeight()==352,'1-Bit surface dimensions differ')
    local handles=symbols.handles
    local native={}
    local function handle(index)
        if not native[index]then
            local x,y,w,h=Symbols.region(index)
            native[index]={texture=symbols.texture,quad=make_quad(x,y,w,h,784,352),
                width=w,height=h,offset_x=x,offset_y=y,source='onebit',symbol_index=index,tint=O.ink}
        end
        return native[index]
    end
    for id,index in pairs(O.actors)do
        handles[id]=handle(index);handles[id..'/attack']=handles[id];handles[id..'/hurt']=handles[id]
    end
    for id,index in pairs(O.objects)do handles[id]=handle(index)end
    local walls,waters={},{}
    local function surface_handle(slot,mask,kind)
        local x,y=slot%49*16,math.floor(slot/49)*16
        return {texture=surface,quad=make_quad(x,y,16,16,784,352),width=16,height=16,
            offset_x=x,offset_y=y,source='onebit',terrain_pack='onebit',
            wall_mask=kind=='wall' and mask or nil,water_mask=kind=='water' and mask or nil}
    end
    for i,mask in ipairs(O.masks)do
        walls[mask]=surface_handle(i-1,mask,'wall')
        waters[mask]=surface_handle(47+i-1,mask,'water')
    end
    local floor=surface_handle(94,255,'floor')
    for _,id in ipairs(Biomes.order)do
        local root='core:terrain/'..id:match('/([^/]+)$')..'/'
        handles[root..'floor/base']=floor;handles[root..'wall/base']=walls[255]
        handles[root..'water/base']=waters[255]
    end
    return setmetatable({handles=handles,walls=walls,waters=waters,floor=floor,
        terrain_renderer=true,terrain_mode='tiled',style='onebit',label='Kenney 1-Bit',
        signature=O.checksum..':onebit-1',pages={symbols={texture=symbols.texture},
            surfaces={texture=surface}}},{__index=O}),'loaded'
end
function O:resolve(id)return self.handles[id]end
function O:terrain(map,x,y,kind,known)
    if kind=='floor'then return self.floor end
    assert(kind=='wall' or kind=='water','invalid 1-Bit terrain kind')
    local mask=0
    for _,n in ipairs({{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},
        {1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}})do
        local xx,yy=x+n[1],y+n[2];local row=map.rows[yy]
        if xx>=1 and row and xx<=#row and ((known and not known(xx,yy)) or
            row:sub(xx,xx)==(kind=='wall' and '#' or '~'))then mask=mask+n[3]end
    end
    return (kind=='wall' and self.walls or self.waters)[O.canonical(mask)]
end
return O
