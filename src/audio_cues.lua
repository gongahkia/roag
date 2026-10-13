-- Stable presentation cue registry. IDs describe game context, never files.
local Biomes=require("src.biomes")
local Classes=require("src.classes")
local Catalog=require("src.boon_content")
local Terrain=require("src.terrain")
local C={}
local builtin_list,builtin_by_id
local function tail(id) return id:match("/([^/]+)$") end
function C.room_cue(room_id,kind)
    assert(kind=='ambient' or kind=='music','invalid room audio kind')
    local hash=0
    for index=1,#room_id do
        hash=(hash*33+room_id:byte(index))%4294967296
    end
    return 'studio:cue/room/'..string.format('%08x',hash)..'/'..
        tail(room_id):sub(1,40)..'/'..kind
end
function C.list(world_pack)
    if not world_pack and builtin_list then return builtin_list end
    local cues,ids={},{}
    local function add(id,name,category,context,event)
        assert(not ids[id],"duplicate audio cue")
        ids[id]=true
        cues[#cues+1]={id=id,name=name,category=category,
            context_id=context or "",event=event or ""}
    end
    for _,row in ipairs({
        {"player/move","Player move","player","Moved"},
        {"player/attack","Player attack","player","AttackPerformed"},
        {"player/dash","Player Dash","player","Moved"},
        {"player/damage","Player damage","player","DamageTaken"},
        {"player/death","Player death","player","Died"},
        {"enemy/move","Enemy movement","enemy","Moved"},
        {"enemy/telegraph","Enemy telegraph","enemy","WindupStarted"},
        {"enemy/attack","Enemy attack","enemy","AttackPerformed"},
        {"enemy/hit","Enemy hit","enemy","DamageTaken"},
        {"enemy/death","Enemy death","enemy","Died"},
        {"boss/arrival","Boss arrival","boss","FinalSanctumEntered"},
        {"boss/phase","Boss phase","boss","RegentPhaseChanged"},
        {"boss/defeat","Boss defeat","boss","RegentDefeated"},
        {"boon/activate","Any boon activation","boon","BoonActivated"},
        {"status/burning","Burning tick","boon","StatusTick"},
        {"terrain/step","Terrain footstep","terrain","TileEntered"},
        {"terrain/hazard","Terrain hazard","terrain","TerrainTriggered"},
        {"terrain/stairs","Stairs","terrain","FloorChanged"},
        {"world/door","Door opens","world","DoorOpened"},
        {"world/chest","Chest claimed","world","ChestClaimed"},
        {"world/secret","Secret discovered","world","SecretDiscovered"},
        {"world/currency","Currency collected","world","CurrencyCollected"},
        {"world/pickup","World pickup","world","CurrencyCollected"},
        {"ui/hover","UI hover","ui",""},
        {"ui/confirm","UI confirm","ui",""},
        {"ui/cancel","UI cancel","ui",""},
        {"ui/level_up","Level up","ui","StatIncreased"},
        {"ui/achievement","Achievement","ui","AchievementEarned"},
        {"ui/victory","Victory","ui","Victory"},
        {"ui/defeat","Defeat","ui","Defeat"},
        {"music/title","Title music","music",""},
        {"music/final_boss","Final boss music","music",""},
        {"music/victory","Victory music","music",""},
        {"music/defeat","Defeat music","music",""},
    }) do
        add("core:cue/"..row[1],row[2],row[3],"",row[4])
    end
    for _,id in ipairs(Classes.order) do
        local class=Classes.get(id)
        add("core:cue/class/"..tail(id).."/select",class.name.." selected",
            "class",id,"title")
        for _,ability_id in ipairs({class.basic_id,class.special_id}) do
            add("core:cue/ability/"..tail(ability_id),
                class.name.." "..(ability_id==class.basic_id and class.basic_name
                    or class.special_name),"ability",ability_id,"AttackPerformed")
        end
    end
    local enemies,elites={},{}
    for _,biome_id in ipairs(Biomes.order) do
        local biome=Biomes.get(biome_id)
        local suffix=tail(biome_id)
        add("core:cue/music/explore/"..suffix,biome.name.." exploration",
            "music",biome_id,"stage")
        add("core:cue/music/elite/"..suffix,biome.name.." elite",
            "music",biome_id,"elite")
        add("core:cue/ambient/"..suffix,biome.name.." ambience",
            "biome",biome_id,"stage")
        enemies[biome.elite_id]=true
        elites[biome.elite_id]=true
        for _,id in ipairs(biome.enemy_ids) do enemies[id]=true end
        for _,id in ipairs(biome.pressure_enemy_ids or {}) do enemies[id]=true end
    end
    enemies["core:enemy/hollow_regent"]=true
    enemies["core:enemy/extraction_pursuer"]=true
    local enemy_ids={};for id in pairs(enemies) do enemy_ids[#enemy_ids+1]=id end
    table.sort(enemy_ids)
    for _,id in ipairs(enemy_ids) do
        local slug=tail(id)
        for _,row in ipairs({{"move","moves","Moved"},
            {"telegraph","winds up","WindupStarted"},
            {"attack","attacks","AttackPerformed"},
            {"hit","is hit","DamageTaken"},
            {"death","dies","Died"}}) do
            add("core:cue/enemy/"..slug.."/"..row[1],
                slug:gsub("_"," ").." "..row[2],"enemy",id,row[3])
        end
        if id=="core:enemy/hollow_regent" or elites[id] then
            for _,row in ipairs({{"arrival","arrives"},
                {"phase","changes phase"},{"defeat","falls"}}) do
                add("core:cue/elite/"..slug.."/"..row[1],
                    slug:gsub("_"," ").." "..row[2],"elite",id,row[1])
            end
        end
    end
    for _,id in ipairs(Catalog.order) do
        add("core:cue/boon/"..tail(id),Catalog.get(id).name.." activates",
            "boon",id,"BoonActivated")
    end
    for _,symbol in ipairs(Terrain.order) do
        local def=Terrain.get(symbol)
        add("core:cue/terrain/"..tail(def.id),def.name,
            "terrain",def.id,"TerrainTriggered")
    end
    for _,room in ipairs(world_pack and world_pack.rooms or {}) do
        add(C.room_cue(room.id,'ambient'),
            room.name.." ambience","room",room.id,"stage")
        add(C.room_cue(room.id,'music'),
            room.name.." music","room",room.id,"stage")
    end
    table.sort(cues,function(a,b)return a.id<b.id end)
    if not world_pack then
        builtin_list,builtin_by_id=cues,{}
        for _,cue in ipairs(cues) do builtin_by_id[cue.id]=cue end
    end
    return cues
end
function C.get(id,world_pack)
    if not world_pack and builtin_by_id and builtin_by_id[id] then
        return builtin_by_id[id]
    end
    for _,cue in ipairs(C.list(world_pack)) do if cue.id==id then return cue end end
    if not world_pack and type(id)=='string' then
        if id:match('^studio:cue/room/%x%x%x%x%x%x%x%x/[a-z0-9_%-]+/ambient$')
            or id:match('^studio:cue/room/%x%x%x%x%x%x%x%x/[a-z0-9_%-]+/music$') then
            return {id=id,name=id,category='room',context_id='',event='stage'}
        end
    end
end
function C.by_category(category,world_pack)
    local out={}
    for _,cue in ipairs(C.list(world_pack)) do
        if not category or category=="all" or cue.category==category then
            out[#out+1]=cue
        end
    end
    return out
end
return C
