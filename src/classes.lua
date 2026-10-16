-- Starting-class content and geometry. Simulation still submits the familiar
-- basic_attack, sweeping_slash and dash actions through one scheduler.
local Targeting=require("src.targeting")
local Classes={}
Classes.order={"core:actor/adventurer","core:actor/ranger",
    "core:actor/arcanist","core:actor/berserker",
    "core:actor/sentinel","core:actor/alchemist"}
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
}
local directions={{0,-1},{1,-1},{1,0},{1,1},
    {0,1},{-1,1},{-1,0},{-1,-1}}

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

function Classes.attack(state,player,kind,dx,dy,content)
    local def=Classes.for_player(player)
    local p=player.position
    local cells,ability_id,ranged,damage_type
    if kind=="basic_attack" then
        ability_id=def.basic_id
        if def.id==Classes.order[2] then
            cells=line(state,content,p,dx,dy,4);ranged=true
        elseif def.id==Classes.order[3] then
            cells=line(state,content,p,dx,dy,3)
            ranged=true;damage_type="lightning"
        elseif def.id==Classes.order[6] then
            cells=line(state,content,p,dx,dy,3)
            ranged=true;damage_type="acid"
        else
            cells=Targeting.cells("basic_attack",p.x,p.y,dx,dy)
        end
    else
        ability_id=def.special_id
        if def.id==Classes.order[2] then
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
        else
            cells=Targeting.cells("sweeping_slash",p.x,p.y,dx,dy)
        end
    end
    return cells,ability_id,ranged,damage_type
end

function Classes.validate()
    local ids={}
    assert(#Classes.order==6,"expected six starting classes")
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
