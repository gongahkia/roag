-- Authored boon data. Behavior names are implemented by the small shared resolver.
local Catalog = {}
Catalog.version = "roag-boons/3"
Catalog.rarities = {"common", "uncommon", "rare", "legendary"}
Catalog.order = {
    "core:boon/storm_conductor", "core:boon/detonation_bloom",
    "core:boon/thorn_mirror", "core:boon/static_footsteps",
    "core:boon/resonant_wounds", "core:boon/temporal_echo",
    "core:boon/crescent_reach", "core:boon/turncoat_spark",
    "core:boon/emberbrand", "core:boon/flashpoint",
    "core:boon/grave_dividend", "core:boon/opening_cut",
    "core:boon/crowdbreaker", "core:boon/harriers_response",
    "core:boon/perfect_guard", "core:boon/final_spark",
    "core:boon/pulse_step", "core:boon/packed_powder",
    "core:boon/longblade_geometry", "core:boon/phantom_lunge",
    "core:boon/guardians_vow", "core:boon/prospectors_heart",
    "core:boon/circuit_chorus", "core:boon/crownfire",
}
Catalog.starter_count = 20
function Catalog.starter_order()
    local result={}
    for i=1,Catalog.starter_count do result[i]=Catalog.order[i] end
    return result
end
function Catalog.normal_order()
    local result=Catalog.starter_order()
    for _,id in ipairs(require("src.boon_expansion_content").order) do
        result[#result+1]=id
    end
    result[#result+1]="core:boon/aftershock"
    result[#result+1]="core:boon/reprisal_nova"
    return result
end

local function strengths(chances, powers)
    local result = {}
    for i, rarity in ipairs(Catalog.rarities) do
        result[rarity] = {chance=chances and chances[i] or 0, power=powers[i]}
    end
    return result
end

Catalog.definitions = {
    ["core:boon/storm_conductor"] = {
        name="Storm Conductor", description="Hits may arc to another hostile creature.",
        tags={"offense", "lightning"}, triggers={"HitConfirmed"},
        scope={kind="owner"}, predicate={all={{op="source_owner"}, {op="target_hostile"}}},
        stack_policy="chance_overflow", parameters=strengths({20000,60000,180000,500000},{1,2,3,4}),
        effects={{kind="damage_nearest", radius=6, damage_type="lightning", proc_coefficient=1}},
    },
    ["core:boon/detonation_bloom"] = {
        name="Detonation Bloom", description="Credited kills burst around the victim.",
        tags={"offense", "explosion"}, triggers={"EntityKilled"}, scope={kind="floor"},
        predicate={op="source_owner"}, stack_policy="add_power",
        parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="area", radius=1, damage_type="explosion", proc_coefficient=1}},
    },
    ["core:boon/thorn_mirror"] = {
        name="Thorn Mirror", description="Positive damage retaliates against its source.",
        tags={"defense", "retaliation"}, triggers={"DamageTaken"}, scope={kind="owner"},
        predicate={all={{op="target_owner"},{op="positive"}}}, stack_policy="add_power",
        parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_source", damage_type="thorns", proc_coefficient=1}},
    },
    ["core:boon/static_footsteps"] = {
        name="Static Footsteps", description="Movement may shock a nearby hostile creature.",
        tags={"movement", "lightning"}, triggers={"TileEntered"}, scope={kind="owner"},
        predicate={op="source_owner"}, stack_policy="chance_overflow",
        parameters=strengths({50000,150000,350000,700000},{1,2,3,4}),
        effects={{kind="damage_nearest", radius=2, damage_type="lightning", proc_coefficient=1}},
    },
    ["core:boon/resonant_wounds"] = {
        name="Resonant Wounds", description="Adjacent hostiles hurt in one chain release a burst.",
        tags={"offense", "resonance"}, triggers={"DamageTaken"},
        scope={kind="radius", radius=8},
        predicate={all={{op="positive"},{op="target_hostile"},
            {op="pair_adjacent_damage", neighborhood="eight"}}},
        stack_policy="add_power", parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="area", radius=1, damage_type="resonance", proc_coefficient=1}},
    },
    ["core:boon/temporal_echo"] = {
        name="Temporal Echo", description="Repeat an attack's locked cells after 200 world time.",
        tags={"offense", "time"}, triggers={"AttackPerformed"}, scope={kind="owner"},
        predicate={op="source_owner"}, stack_policy="add_power",
        parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="echo", delay=200, damage_type="echo", proc_coefficient=1}},
    },
    ["core:boon/crescent_reach"] = {
        name="Crescent Reach", description="Slash gains one forward cell and extra damage.",
        tags={"transformation", "offense"}, triggers={}, scope={kind="owner"},
        stack_policy="add_power", parameters=strengths(nil,{1,2,3,4}),
        modifiers={{ability_id="core:ability/sweeping_slash", kind="forward_cell"},
            {ability_id="core:ability/sweeping_slash", kind="add_damage"}},
        effects={}, incompatible={},eligible_classes={"core:actor/adventurer"},
    },
    ["core:boon/turncoat_spark"] = {
        name="Turncoat Spark", description="Observed enemy friendly fire shocks the aggressor.",
        tags={"offense", "lightning"}, triggers={"DamageTaken"},
        scope={kind="radius", radius=8},
        predicate={all={{op="positive"},{op="source_target_same_faction"},
            {op="source_not_owner"},{op="target_hostile"}}},
        stack_policy="add_power", parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_source", damage_type="lightning", proc_coefficient=1}},
    },
    ["core:boon/emberbrand"] = {
        name="Emberbrand", description="Direct hits ignite foes for three 100-time ticks.",
        tags={"offense","burning"}, triggers={"HitConfirmed"}, scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="target_hostile"},{op="direct_attack"}}},
        stack_policy="add_power",parameters=strengths(nil,{2,3,4,6}),
        effects={{kind="apply_status", status="burning", interval=100, ticks=3}},
    },
    ["core:boon/flashpoint"] = {
        name="Flashpoint", description="Damage to a Burning foe beside another Burning foe bursts.",
        tags={"offense","burning"}, triggers={"DamageTaken"},scope={kind="radius",radius=8},
        predicate={all={{op="positive"},{op="target_hostile"},{op="burning_pair"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="area",radius=1,damage_type="fire"}},
    },
    ["core:boon/grave_dividend"] = {
        name="Grave Dividend",description="Credited hostile kills restore health.",
        tags={"sustain"},triggers={"EntityKilled"},scope={kind="floor"},
        predicate={all={{op="source_owner"},{op="target_hostile"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="heal_owner"}},
    },
    ["core:boon/opening_cut"] = {
        name="Opening Cut",description="Directly damaging an unhurt foe deals bonus damage.",
        tags={"offense"},triggers={"DamageTaken"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="target_hostile"},{op="direct_attack"},{op="target_was_full"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_target",damage_type="physical"}},
    },
    ["core:boon/crowdbreaker"] = {
        name="Crowdbreaker",description="Direct hits gain bonus damage per nearby living hostile.",
        tags={"offense","spatial"},triggers={"DamageTaken"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="target_hostile"},{op="direct_attack"},{op="adjacent_hostiles",min=1}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_target",damage_type="physical",scale="adjacent_hostiles"}},
    },
    ["core:boon/harriers_response"] = {
        name="Harrier's Response",description="Nearby hostile windups may draw a retaliatory spark.",
        tags={"defense","lightning"},triggers={"WindupStarted"},scope={kind="radius",radius=7},
        predicate={all={{op="source_hostile"},{op="unique_windup"}}},
        stack_policy="chance_overflow",
        parameters=strengths({80000,180000,350000,650000},{1,2,3,4}),
        effects={{kind="damage_source",damage_type="lightning"}},
    },
    ["core:boon/perfect_guard"] = {
        name="Perfect Guard",description="Fully blocked hostile damage retaliates nearby.",
        tags={"defense"},triggers={"DamageBlocked"},scope={kind="owner"},
        predicate={all={{op="target_owner"},{op="source_hostile"},{op="source_in_radius",radius=5}}},
        stack_policy="add_power",parameters=strengths(nil,{2,3,4,6}),
        effects={{kind="damage_source",damage_type="thorns"}},
    },
    ["core:boon/final_spark"] = {
        name="Final Spark",description="Direct hits while at 40% health or less spark again.",
        tags={"offense","lightning"},triggers={"DamageTaken"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="target_hostile"},{op="direct_attack"},{op="owner_hp_percent",max=40}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_target",damage_type="lightning"}},
    },
    ["core:boon/pulse_step"] = {
        name="Pulse Step",description="Enter a tile beside two foes to pulse into both.",
        tags={"movement","spatial"},triggers={"TileEntered"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="adjacent_hostiles",min=2}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="adjacent_hostiles",damage_type="pulse"}},
    },
    ["core:boon/packed_powder"] = {
        name="Packed Powder",description="Each disjoint three credited or observed hostile deaths blast.",
        tags={"offense","explosion"},triggers={"EntityKilled"},scope={kind="floor"},
        predicate={all={{op="target_hostile"},{op="distinct_chain_group",size=3}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="area",radius=1,damage_type="explosion"}},
    },
    ["core:boon/longblade_geometry"] = {
        name="Longblade Geometry",description="Explicit Sword Strike reaches two cells in its line.",
        tags={"transformation","offense"},triggers={},scope={kind="owner"},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),effects={},
        modifiers={{ability_id="core:ability/basic_attack",kind="line_second_cell"},
            {ability_id="core:ability/basic_attack",kind="add_damage"}},incompatible={},
        eligible_classes={"core:actor/adventurer"},
    },
    ["core:boon/phantom_lunge"] = {
        name="Phantom Lunge",description="Dash strikes each hostile beside its traversed path once.",
        tags={"transformation","movement"},triggers={"Moved"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="ability_id",value="core:ability/dash"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="path_adjacent_hostiles",damage_type="physical"}},
        modifiers={{ability_id="core:ability/dash",kind="path_strike"}},incompatible={},
    },
    ["core:boon/guardians_vow"] = {
        name="Guardian's Vow", description="A fully blocked hostile hit bursts around you.",
        tags={"defense","explosion"}, triggers={"DamageBlocked"},scope={kind="owner"},
        predicate={all={{op="target_owner"},{op="source_hostile"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="area",radius=1,damage_type="explosion"}},
    },
    ["core:boon/prospectors_heart"] = {
        name="Prospector's Heart", description="Collecting currency restores health.",
        tags={"sustain","currency"},triggers={"CurrencyCollected"},scope={kind="owner"},
        predicate={op="source_owner"},stack_policy="add_power",
        parameters=strengths(nil,{1,2,3,4}),effects={{kind="heal_owner"}},
    },
    ["core:boon/circuit_chorus"] = {
        name="Circuit Chorus",description="Other boon activations spark a nearby foe.",
        tags={"offense","lightning"},triggers={"BoonActivated"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="other_boon"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="damage_nearest",radius=4,damage_type="lightning"}},
    },
    ["core:boon/crownfire"] = {
        name="Crownfire",description="Direct first strikes ignite unhurt foes for three ticks.",
        tags={"offense","burning"},triggers={"DamageTaken"},scope={kind="owner"},
        predicate={all={{op="source_owner"},{op="target_hostile"},
            {op="direct_attack"},{op="target_was_full"},{op="positive"}}},
        stack_policy="add_power",parameters=strengths(nil,{1,2,3,4}),
        effects={{kind="apply_status",status="burning",interval=100,ticks=3}},
    },
}

local expansion=require("src.boon_expansion_content")
for _,id in ipairs(expansion.order) do
    assert(not Catalog.definitions[id],"duplicate expansion boon")
    Catalog.order[#Catalog.order+1]=id
    Catalog.definitions[id]=expansion.definitions[id]
end

-- Two ordinary-pool area boons. Each uses the existing area resolver and
-- branch-local ancestry guard, so secondary hits cannot recurse forever.
Catalog.order[#Catalog.order+1]="core:boon/aftershock"
Catalog.definitions["core:boon/aftershock"]={
    name="Aftershock",description="A direct hit bursts across the target's adjacent cells.",
    tags={"offense","area"},triggers={"DamageTaken"},scope={kind="owner"},
    predicate={all={{op="source_owner"},{op="target_hostile"},
        {op="positive"},{op="direct_attack"}}},
    stack_policy="add_power",parameters=strengths(nil,{2,3,4,6}),
    effects={{kind="area",radius=1,damage_type="shock"}},
    unit="adjacent burst damage before defense",
    eligible_classes={"core:actor/sentinel","core:actor/alchemist"},
}
Catalog.order[#Catalog.order+1]="core:boon/reprisal_nova"
Catalog.definitions["core:boon/reprisal_nova"]={
    name="Reprisal Nova",description="Taking real hostile damage bursts around you.",
    tags={"defense","area"},triggers={"DamageTaken"},scope={kind="owner"},
    predicate={all={{op="target_owner"},{op="source_hostile"},{op="positive"}}},
    stack_policy="add_power",parameters=strengths(nil,{2,3,4,6}),
    effects={{kind="area",radius=1,damage_type="pulse"}},
    unit="nearby retaliation damage before defense",
    eligible_classes={"core:actor/sentinel","core:actor/alchemist"},
}

for _, id in ipairs(Catalog.order) do Catalog.definitions[id].id = id end

local valid_events = {HitConfirmed=true, EntityKilled=true, DamageTaken=true,
    DamageBlocked=true,TileEntered=true, AttackPerformed=true,WindupStarted=true,Moved=true,
    CurrencyCollected=true,BoonActivated=true,TerrainTriggered=true,TerrainBroken=true}
local valid_effects = {damage_nearest=true, damage_source=true, area=true, echo=true,
    apply_status=true,heal_owner=true,damage_target=true,adjacent_hostiles=true,
    path_adjacent_hostiles=true,prime=true,speed_boost=true,ward=true,
    mark_damage=true,status_neighbors=true}
local valid_predicates = {source_owner=true, target_owner=true, target_hostile=true,
    source_not_owner=true, positive=true, source_target_same_faction=true,
    pair_adjacent_damage=true, source_id=true, target_id=true,
    source_tag=true, target_tag=true, source_faction=true, target_faction=true,
    direct_attack=true,target_was_full=true,adjacent_hostiles=true,burning_pair=true,
    source_hostile=true,source_in_radius=true,owner_hp_percent=true,
    distinct_chain_group=true,ability_id=true,unique_windup=true,other_boon=true,
    event_kind=true,from_terrain=true,path_terrain=true,source_on_terrain=true,
    target_near_terrain=true,terrain_id=true,damage_type=true,primed=true,
    special_attack=true,target_hp_percent_before=true,owner_in_locked_cells=true,
    overlapping_windups=true,approaching_owner=true,aligned_hostiles=true,
    event_distance=true,visible_source=true,visible_event=true}

local function validate_predicate(node)
    assert(type(node) == "table", "invalid boon predicate")
    if node.all or node.any then
        local children = node.all or node.any
        assert(type(children) == "table" and #children > 0, "empty boon predicate")
        for _, child in ipairs(children) do validate_predicate(child) end
    elseif node.not_ then validate_predicate(node.not_)
    else
        assert(valid_predicates[node.op], "unknown boon predicate")
        if node.op == "pair_adjacent_damage" then
            assert(node.neighborhood == "cardinal" or node.neighborhood == "eight",
                "invalid pair neighborhood")
        elseif node.op == "adjacent_hostiles" then
            assert(type(node.min)=="number" and node.min%1==0 and node.min>=1,"invalid adjacent count")
        elseif node.op == "distinct_chain_group" then
            assert(type(node.size)=="number" and node.size%1==0 and node.size>=2,"invalid group size")
        elseif node.op == "source_in_radius" then
            assert(type(node.radius)=="number" and node.radius%1==0 and node.radius>=0,"invalid source radius")
        elseif node.op == "owner_hp_percent" then
            assert(type(node.max)=="number" and node.max>=0 and node.max<=100,"invalid health threshold")
        elseif node.op == "target_hp_percent_before" then
            assert(type(node.max)=="number" and node.max>=0 and node.max<=100,
                "invalid target health threshold")
        elseif node.op == "aligned_hostiles" then
            assert(type(node.min)=="number" and node.min%1==0 and node.min>=2,
                "invalid formation count")
        elseif node.op == "event_distance" then
            assert(type(node.min)=="number" and type(node.max)=="number"
                and node.min>=0 and node.max>=node.min,"invalid event distance")
        elseif node.op == "source_id" or node.op == "target_id" or node.op == "ability_id"
            or node.op == "source_tag" or node.op == "target_tag"
            or node.op == "source_faction" or node.op == "target_faction"
            or node.op == "event_kind" or node.op == "from_terrain"
            or node.op == "path_terrain" or node.op == "source_on_terrain"
            or node.op == "target_near_terrain" or node.op == "terrain_id"
            or node.op == "damage_type" then
            assert(node.value ~= nil, "missing predicate value")
        end
    end
end

function Catalog.validate(definitions, order)
    definitions, order = definitions or Catalog.definitions, order or Catalog.order
    local seen = {}
    for _, id in ipairs(order) do
        assert(not seen[id], "duplicate boon ID")
        seen[id] = true
        local def = assert(definitions[id], "missing boon definition")
        assert(type(id) == "string" and id:match("^core:boon/"), "invalid boon ID")
        assert(def.id == id, "boon definition ID mismatch")
        assert(type(def.name) == "string" and type(def.description) == "string", "invalid boon text")
        assert(def.unit==nil or (type(def.unit)=="string" and #def.unit>0),
            "invalid boon magnitude text")
        assert(type(def.tags) == "table" and type(def.triggers) == "table"
            and type(def.effects) == "table", "invalid boon declaration")
        assert(def.scope and ({owner=true, nearby=true, radius=true, floor=true})[def.scope.kind],
            "invalid observation scope")
        if def.scope.kind == "radius" or def.scope.kind == "nearby" then
            assert(type(def.scope.radius) == "number" and def.scope.radius >= 0,
                "invalid observation radius")
        end
        if def.predicate then validate_predicate(def.predicate) end
        assert(def.stack_policy == "chance_overflow" or def.stack_policy == "add_power",
            "invalid stack policy")
        local seen_triggers = {}
        for _, trigger in ipairs(def.triggers) do
            assert(valid_events[trigger] and not seen_triggers[trigger], "invalid boon trigger")
            seen_triggers[trigger] = true
        end
        for _, effect in ipairs(def.effects) do
            assert(valid_effects[effect.kind], "invalid boon effect")
            if effect.when then validate_predicate(effect.when) end
            assert(effect.proc_coefficient == nil or
                (type(effect.proc_coefficient) == "number" and effect.proc_coefficient >= 0
                    and effect.proc_coefficient <= 10), "invalid proc coefficient")
            if effect.radius then
                assert(type(effect.radius) == "number" and effect.radius % 1 == 0
                    and effect.radius >= 0, "invalid effect radius")
            end
            if effect.kind == "echo" then
                assert(type(effect.delay) == "number" and effect.delay % 1 == 0
                    and effect.delay > 0, "invalid echo delay")
            end
            if effect.kind == "apply_status" then
                assert(effect.status=="burning" and type(effect.interval)=="number"
                    and effect.interval%1==0 and effect.interval>0
                    and type(effect.ticks)=="number" and effect.ticks%1==0
                    and effect.ticks>0,"invalid status effect")
            end
            if effect.kind=="prime" or effect.kind=="speed_boost"
                or effect.kind=="ward" or effect.kind=="mark_damage" then
                assert(type(effect.duration)=="number" and effect.duration%1==0
                    and effect.duration>0 and effect.duration<=10000,
                    "invalid boon duration")
            end
            if effect.kind=="mark_damage" then
                assert(type(effect.threshold)=="number" and effect.threshold%1==0
                    and effect.threshold>=2 and effect.threshold<=effect.cap
                    and effect.cap<=20,"invalid mark threshold")
            end
            if effect.kind=="status_neighbors" then
                assert(effect.status=="burning","invalid shared status")
            end
            if effect.consume_prime~=nil then
                assert(effect.consume_prime==true and effect.kind=="damage_target",
                    "invalid primed effect")
            end
            assert(effect.scale==nil or effect.scale=="adjacent_hostiles","invalid effect scale")
        end
        local last_power, last_chance = -1, -1
        for _, rarity in ipairs(Catalog.rarities) do
            local p = assert(def.parameters[rarity], "missing rarity parameters")
            assert(type(p.power) == "number" and p.power >= 0, "invalid boon power")
            assert(type(p.chance) == "number" and p.chance >= 0
                and p.chance % 1 == 0, "invalid boon chance")
            assert(p.power >= last_power and p.chance >= last_chance,
                "rarity strength must be monotone")
            last_power, last_chance = p.power, p.chance
        end
        for _, modifier in ipairs(def.modifiers or {}) do
            assert((modifier.ability_id == "core:ability/sweeping_slash"
                    and (modifier.kind == "forward_cell" or modifier.kind == "add_damage"))
                or (modifier.ability_id == "core:ability/basic_attack"
                    and (modifier.kind == "line_second_cell" or modifier.kind == "add_damage"))
                or (modifier.ability_id == "core:ability/dash"
                    and (modifier.kind == "path_strike" or modifier.kind=="dash_prime"))
                or (modifier.ability_id=="core:ability/ranger_shot"
                    and (modifier.kind=="branch_near" or modifier.kind=="add_damage"))
                or (modifier.ability_id=="core:ability/ranger_volley"
                    and (modifier.kind=="line_extend" or modifier.kind=="add_damage"))
                or (modifier.ability_id=="core:ability/arcanist_sigil"
                    and (modifier.kind=="area_expand" or modifier.kind=="add_damage"))
                or (modifier.ability_id=="core:ability/arcanist_bolt"
                    and (modifier.kind=="status_chain" or modifier.kind=="add_damage"))
                or (modifier.ability_id=="core:ability/berserker_crush"
                    and (modifier.kind=="shockwave" or modifier.kind=="add_damage")),
                "invalid ability modifier")
        end
        if def.eligible_classes then
            local Classes=require("src.classes")
            assert(type(def.eligible_classes)=="table" and #def.eligible_classes>0,
                "invalid class eligibility")
            local classes={}
            for _,class_id in ipairs(def.eligible_classes) do
                assert(Classes.get(class_id) and not classes[class_id],
                    "invalid eligible class")
                classes[class_id]=true
            end
        end
        for _, other in ipairs(def.incompatible or {}) do
            assert(other ~= id and definitions[other], "invalid boon incompatibility")
        end
    end
    for id in pairs(definitions) do assert(seen[id], "unlisted boon definition") end
    return true
end

function Catalog.get(id) return Catalog.definitions[id] end

function Catalog.offer_text(id,rarity)
    local def=assert(Catalog.get(id),"unknown boon")
    local p=assert(def.parameters[rarity],"unknown rarity")
    local units={
        ["core:boon/emberbrand"]="damage/tick before defense",
        ["core:boon/flashpoint"]="burst damage before defense",
        ["core:boon/grave_dividend"]="health per credited kill",
        ["core:boon/opening_cut"]="bonus damage before defense",
        ["core:boon/crowdbreaker"]="damage per adjacent foe",
        ["core:boon/harriers_response"]="spark damage before defense",
        ["core:boon/perfect_guard"]="retaliation damage before defense",
        ["core:boon/final_spark"]="bonus damage before defense",
        ["core:boon/pulse_step"]="pulse damage before defense",
        ["core:boon/packed_powder"]="blast damage before defense",
        ["core:boon/longblade_geometry"]="Sword damage bonus; reach stays 2",
        ["core:boon/phantom_lunge"]="path strike damage before defense",
        ["core:boon/guardians_vow"]="adjacent burst damage before defense",
        ["core:boon/prospectors_heart"]="health per pickup",
        ["core:boon/circuit_chorus"]="spark damage before defense",
        ["core:boon/crownfire"]="Burning damage/tick before defense",
    }
    local suffix=def.stack_policy=="chance_overflow"
        and ("%.1f%% proc, %d %s"):format(p.chance/10000,p.power,
            def.unit or units[id] or "power")
        or ("%d %s per copy"):format(p.power,def.unit or units[id] or "power")
    return def.description.." ["..suffix.."]"
end

return Catalog
