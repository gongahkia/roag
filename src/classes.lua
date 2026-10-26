-- Starting-class content and geometry. Simulation still submits the familiar
-- basic_attack, sweeping_slash and dash actions through one scheduler.
local Targeting=require("src.targeting")
local D=require('src.idea_duelist')
local Geo=require('src.idea_geomancer')
local Comp=require('src.idea_companions')
local Storm=require('src.idea_stormcaller')
local Way=require('src.idea_wayfinder')
local Classes={}
Classes.order={"core:actor/adventurer","core:actor/ranger",
    "core:actor/arcanist","core:actor/berserker",
    "core:actor/sentinel","core:actor/alchemist",
    "core:actor/shade","core:actor/hookrunner","core:actor/trapper"}
Classes.definitions={
    [Classes.order[1]]={id=Classes.order[1],name="Adventurer",
        description="Balanced sword, three-cell sweep, two-cell Dash",
        hp=24,damage=4,slash_damage=3,defense=0,speed=100,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=100,sweeping_slash=150,dash=130},
        dash_distance=2,basic_id="core:ability/basic_attack",
        special_id="core:ability/sweeping_slash",
        basic_name="Sword Strike",special_name="Sweeping Slash",dash_name="Dash",
        ui_controls="F Sword · R Slash · X Dash"},
    [Classes.order[2]]={id=Classes.order[2],name="Ranger",
        description="Four-cell bow, piercing volley, three-cell evasive Dash",
        hp=20,damage=3,slash_damage=3,defense=0,speed=115,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=100,sweeping_slash=155,dash=115},
        dash_distance=3,basic_id="core:ability/ranger_shot",
        special_id="core:ability/ranger_volley",
        basic_name="Bow Shot",special_name="Piercing Volley",dash_name="Evasive Dash",
        ui_controls="F Bow · R Volley · X Evade"},
    [Classes.order[3]]={id=Classes.order[3],name="Arcanist",
        description="Lightning bolt, fire sigil, measured phase step",
        hp=21,damage=3,slash_damage=3,defense=0,speed=100,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=110,sweeping_slash=170,dash=115},
        dash_distance=2,basic_id="core:ability/arcanist_bolt",
        special_id="core:ability/arcanist_sigil",
        basic_name="Arc Bolt",special_name="Fire Sigil",dash_name="Phase Step",
        ui_controls="F Bolt · R Sigil · X Step"},
    [Classes.order[4]]={id=Classes.order[4],name="Berserker",
        description="Heavy crush, broad cleave, slower armored traversal",
        hp=32,damage=6,slash_damage=5,defense=1,speed=75,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=115,basic_attack=130,sweeping_slash=180,dash=165},
        dash_distance=2,basic_id="core:ability/berserker_crush",
        special_id="core:ability/berserker_cleave",
        basic_name="Crushing Blow",special_name="Broad Cleave",dash_name="Heavy Lunge",
        ui_controls="F Crush · R Cleave · X Lunge"},
    [Classes.order[5]]={id=Classes.order[5],name="Sentinel",
        description="Shield strike and an eight-cell defensive pulse",
        hp=29,damage=4,slash_damage=3,defense=2,speed=90,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=105,basic_attack=105,sweeping_slash=165,dash=145},
        dash_distance=2,basic_id="core:ability/sentinel_strike",
        special_id="core:ability/sentinel_pulse",
        basic_name="Shield Strike",special_name="Shield Pulse",dash_name="Guard Step",
        ui_controls="F Strike · R Pulse · X Step"},
    [Classes.order[6]]={id=Classes.order[6],name="Alchemist",
        description="Three-cell vial shot and a targeted cross-shaped blast",
        hp=22,damage=3,slash_damage=4,defense=0,speed=105,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=105,sweeping_slash=165,dash=130},
        dash_distance=2,basic_id="core:ability/alchemist_vial",
        special_id="core:ability/alchemist_flask",
        basic_name="Vial Shot",special_name="Blast Flask",dash_name="Quick Step",
        ui_controls="F Vial · R Flask · X Step"},
    [Classes.order[7]]={id=Classes.order[7],name="Shade",
        description="Thrown blade, attack-drawing decoy, one swap",
        hp=20,damage=3,slash_damage=0,defense=0,speed=115,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=100,sweeping_slash=155,dash=115},
        dash_distance=2,basic_id="core:ability/shade_blade",
        special_id="core:ability/shade_decoy",
        basic_name="Thrown Blade",special_name="Place Decoy",dash_name="Shadow Swap",
        ui_controls="F Blade · R Decoy · X Swap"},
    [Classes.order[8]]={id=Classes.order[8],name="Hookrunner",
        description="Knife, three-cell hook pull, short sidestep",
        hp=22,damage=4,slash_damage=3,defense=0,speed=115,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=95,sweeping_slash=145,dash=110},
        dash_distance=2,basic_id="core:ability/hookrunner_knife",
        special_id="core:ability/hookrunner_hook",
        basic_name="Knife",special_name="Hook Pull",dash_name="Sidestep",
        ui_controls="F Knife · R Hook · X Step"},
    [Classes.order[9]]={id=Classes.order[9],name="Trapper",
        description="Knife, visible snare, short roll",
        hp=22,damage=4,slash_damage=3,defense=0,speed=105,
        crit_chance_ppm=0,crit_damage_percent=150,
        costs={move=100,basic_attack=100,sweeping_slash=150,dash=120},
        dash_distance=2,basic_id="core:ability/trapper_knife",
        special_id="core:ability/trapper_snare",
        basic_name="Knife",special_name="Place Snare",dash_name="Roll",
        ui_controls="F Knife · R Snare · X Roll"},
}
local directions={{0,-1},{1,-1},{1,0},{1,1},
    {0,1},{-1,1},{-1,0},{-1,-1}}

Classes.order[#Classes.order+1]=D.id
Classes.definitions[D.id]=D.definition
Classes.order[#Classes.order+1]=Geo.id
Classes.definitions[Geo.id]=Geo.definition
Classes.order[#Classes.order+1]=Comp.spirit_id
Classes.definitions[Comp.spirit_id]=Comp.spirit
Classes.order[#Classes.order+1]=Comp.tinker_id
Classes.definitions[Comp.tinker_id]=Comp.tinker
Classes.order[#Classes.order+1]=Storm.id
Classes.definitions[Storm.id]=Storm.definition
Classes.order[#Classes.order+1]=Way.id
Classes.definitions[Way.id]=Way.definition

function Classes.get(id) return Classes.definitions[id] end
function Classes.for_player(player)
    return assert(Classes.get(player.definition_id),"unknown player class")
end
function Classes.cost(player,kind)
    return Classes.for_player(player).costs[kind]
end
function Classes.is_attack(ability_id)
    for _,id in ipairs(Classes.order) do
        local class=Classes.get(id)
        if ability_id==class.basic_id or ability_id==class.special_id then
            return true
        end
    end
    return false
end

-- An action that actually enters the immediate primary attack pipeline.
-- Placement, guard and delayed-discharge skills keep their own semantics.
function Classes.immediate_attack(player,kind,preview)
    if kind=='basic_attack' then return true end
    if kind~='sweeping_slash' then return false end
    local id=player.definition_id
    if id==Comp.spirit_id then return preview.companion_mode=='strike' end
    return id~=D.id and id~=Geo.id and id~=Comp.tinker_id and id~=Storm.id
        and id~=Way.id and id~='core:actor/shade' and id~='core:actor/trapper'
end

local function line(state,content,position,dx,dy,range)
    local map=content.map_for(state)
    local cells={}
    for step=1,range do
        local x,y=position.x+dx*step,position.y+dy*step
        local blocked,corner_x,corner_y=false,nil,nil
        if dx~=0 and dy~=0 then
            local old_x,old_y=position.x+dx*(step-1),position.y+dy*(step-1)
            if not content.projectile_passable(map,x,old_y,state) then
                blocked,corner_x,corner_y=true,x,old_y
            elseif not content.projectile_passable(map,old_x,y,state) then
                blocked,corner_x,corner_y=true,old_x,y
            end
        end
        if blocked then
            cells[#cells+1]={x=corner_x,y=corner_y}
            break
        end
        cells[#cells+1]={x=x,y=y}
        if not content.projectile_passable(map,x,y,state) then break end
    end
    return cells
end

function Classes.attack(state,player,kind,dx,dy,content,explicit)
    local def=Classes.for_player(player)
    local p=player.position
    local cells,ability_id,ranged,damage_type
    if kind=="basic_attack" then
        ability_id=def.basic_id
        if def.id==Way.id then
            cells=line(state,content,p,dx,dy,2);ranged=true
        elseif def.id==Storm.id then
            cells=line(state,content,p,dx,dy,3);ranged=true;damage_type='lightning'
        elseif def.id==Comp.spirit_id or def.id==Comp.tinker_id then
            cells=line(state,content,p,dx,dy,3);ranged=true;damage_type='spirit'
        elseif def.id==Geo.id then
            cells=line(state,content,p,dx,dy,3);ranged=true
        elseif def.id==D.id and explicit then
            cells=line(state,content,p,dx,dy,2);ranged='piercing_line'
        elseif def.id==Classes.order[2] then
            cells=line(state,content,p,dx,dy,4);ranged=true
        elseif def.id==Classes.order[3] then
            cells=line(state,content,p,dx,dy,3)
            ranged=true;damage_type="lightning"
        elseif def.id==Classes.order[6] then
            cells=line(state,content,p,dx,dy,3)
            ranged=true;damage_type="acid"
        elseif def.id==Classes.order[7] then
            cells=line(state,content,p,dx,dy,2);ranged=true
        else
            cells=Targeting.cells("basic_attack",p.x,p.y,dx,dy)
        end
    else
        ability_id=def.special_id
        if def.id==Way.id then
            cells={{x=p.x+dx,y=p.y+dy}}
        elseif def.id==Storm.id then
            cells={};local map=content.map_for(state)
            local travel=line(state,content,p,dx,dy,2);local center=travel[2]
            if center and center.x==p.x+2*dx and center.y==p.y+2*dy and content.projectile_passable(map,center.x,center.y,state)then
                for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}})do
                    local x,y=center.x+d[1],center.y+d[2]
                    if content.projectile_passable(map,x,y,state)then cells[#cells+1]={x=x,y=y}end
                end
            end
        elseif def.id==Comp.spirit_id or def.id==Comp.tinker_id then
            cells=Comp.cells(state,player,dx,dy,content)
        elseif def.id==Geo.id then
            cells={{x=p.x+dx,y=p.y+dy}}
        elseif def.id==D.id then
            cells=Targeting.cells('sweeping_slash',p.x,p.y,dx,dy)
        elseif def.id==Classes.order[2] then
            cells=line(state,content,p,dx,dy,5)
            ranged="piercing_line"
        elseif def.id==Classes.order[3] then
            local travel=line(state,content,p,dx,dy,2)
            local center=travel[#travel]
            cells={}
            if center and center.x==p.x+2*dx and center.y==p.y+2*dy
                and content.projectile_passable(content.map_for(state),center.x,center.y,state) then
                local map=content.map_for(state)
                for oy=-1,1 do for ox=-1,1 do
                    local x,y=center.x+ox,center.y+oy
                    if content.projectile_passable(map,x,y,state) then
                        cells[#cells+1]={x=x,y=y}
                    end
                end end
            else cells=travel end
            ranged="area";damage_type="fire"
        elseif def.id==Classes.order[4] then
            cells={}
            local index=assert(Targeting.direction_index(dx,dy))
            for offset=-2,2 do
                local d=directions[(index+offset-1)%8+1]
                cells[#cells+1]={x=p.x+d[1],y=p.y+d[2]}
            end
        elseif def.id==Classes.order[5] then
            cells={}
            for _,d in ipairs(directions) do
                cells[#cells+1]={x=p.x+d[1],y=p.y+d[2]}
            end
            damage_type="pulse"
        elseif def.id==Classes.order[6] then
            local travel=line(state,content,p,dx,dy,3)
            local center=travel[#travel]
            cells={}
            local map=content.map_for(state)
            if center and center.x==p.x+3*dx and center.y==p.y+3*dy
                and content.projectile_passable(map,center.x,center.y,state) then
                for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}}) do
                    local x,y=center.x+d[1],center.y+d[2]
                    if content.projectile_passable(map,x,y,state) then
                        cells[#cells+1]={x=x,y=y}
                    end
                end
            else
                cells=travel
            end
            ranged="area";damage_type="explosion"
        elseif def.id==Classes.order[7] or def.id==Classes.order[9] then
            cells={{x=p.x+dx,y=p.y+dy}}
        elseif def.id==Classes.order[8] then
            cells=line(state,content,p,dx,dy,3);ranged=true
        else
            cells=Targeting.cells("sweeping_slash",p.x,p.y,dx,dy)
        end
    end
    return cells,ability_id,ranged,damage_type
end

local function action_hint(player,kind,preview)
    if preview.vow then
        return ('VOW: %d HP -> +%d damage%s · P off · Enter'):format(
            preview.vow.hp_cost,preview.vow.bonus,preview.fate_power and ' · die on commit' or '')
    end
    if preview.fate_power then return 'Die rolls on commit · Enter attack · Esc cancel' end
    if kind=='dash' then return 'Enter to move · Esc cancel' end
    if kind~='sweeping_slash' then return 'Enter to strike · Esc cancel' end
    local id=player.definition_id
    local hints={[D.id]='Enter to parry · no attack damage',
        [Geo.id]='Enter to raise one finite block',
        [Comp.tinker_id]='Enter to deploy three-charge sentry',
        [Storm.id]='Enter to spend charge on locked cross',
        [Way.id]='Enter to plant return beacon'}
    if id==Comp.spirit_id then return preview.companion_mode=='strike'
        and 'Enter to order one paid spirit strike' or 'Enter to position spirit' end
    return hints[id] or 'Enter to strike · Esc cancel'
end
function Classes.preview_hint(player,kind,preview)
    local hint=action_hint(player,kind,preview)
    if preview.weapon_hint then hint=hint..' · '..preview.weapon_hint end
    return preview.boon_hint and hint..' · '..preview.boon_hint or hint
end
function Classes.status(state,player)
    local id=player.definition_id
    if id==Geo.id then
        for _,e in ipairs(state.entities)do if e.controller==Geo.controller then
            return e.position.floor_id==player.position.floor_id and ('BULWARK %d TIME'):format(math.max(0,e.expires-state.clock))
                or 'BULWARK ON OTHER FLOOR','R replace · expires in world time'
        end end
        return 'NO BULWARK','R place block · routes stay open'
    elseif id==Comp.spirit_id then
        local e=Comp.owned(state,'spirit')
        return e and 'SPIRIT PLACED' or 'NO BOUND SPIRIT','R position or order adjacent strike'
    elseif id==Comp.tinker_id then
        local e=Comp.owned(state,'sentry')
        return e and ('SENTRY %d/3'):format(e.sentry_shots) or 'NO SENTRY',
            e and e.sentry_shots==0 and 'Spent · R replaces sentry' or 'Bolts have friendly fire · R replace'
    elseif id==Storm.id then
        return ('CHARGE %d/3'):format(player.storm_charge or 0),player.storm_ward and 'Grounded ward ready' or 'R discharge · X ground ward'
    elseif id==Way.id then
        return state.class_special.beacon and 'BEACON PLANTED' or 'NO BEACON','R plant · X room-bound return / Dash'
    end
end
function Classes.validate()
    local ids={}
    assert(#Classes.order==15,"expected fifteen starting classes")
    for _,id in ipairs(Classes.order) do
        local def=assert(Classes.get(id),"missing class")
        assert(def.id==id and not ids[id] and type(def.name)=="string"
            and type(def.description)=="string" and def.dash_distance>=1
            and def.dash_distance<=3 and def.speed>0 and def.hp>0,
            "invalid class identity")
        for _,key in ipairs({"move","basic_attack","sweeping_slash","dash"}) do
            local cost=def.costs[key]
            assert(type(cost)=="number" and cost%1==0 and cost>0,
                "invalid class action cost")
        end
        ids[id]=true
    end
    return true
end
return Classes
