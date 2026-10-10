-- Authored boon data. Behavior names are implemented by the small shared resolver.
local Catalog = {}
Catalog.version = "roeg-boons/1"
Catalog.rarities = {"common", "uncommon", "rare", "legendary"}
Catalog.order = {
    "core:boon/storm_conductor", "core:boon/detonation_bloom",
    "core:boon/thorn_mirror", "core:boon/static_footsteps",
    "core:boon/resonant_wounds", "core:boon/temporal_echo",
    "core:boon/crescent_reach", "core:boon/turncoat_spark",
}

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
        effects={}, incompatible={},
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
}

for _, id in ipairs(Catalog.order) do Catalog.definitions[id].id = id end

local valid_events = {HitConfirmed=true, EntityKilled=true, DamageTaken=true,
    TileEntered=true, AttackPerformed=true}
local valid_effects = {damage_nearest=true, damage_source=true, area=true, echo=true}
local valid_predicates = {source_owner=true, target_owner=true, target_hostile=true,
    source_not_owner=true, positive=true, source_target_same_faction=true,
    pair_adjacent_damage=true, source_id=true, target_id=true,
    source_tag=true, target_tag=true, source_faction=true, target_faction=true}

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
        elseif node.op == "source_id" or node.op == "target_id"
            or node.op == "source_tag" or node.op == "target_tag"
            or node.op == "source_faction" or node.op == "target_faction" then
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
            assert(modifier.ability_id == "core:ability/sweeping_slash"
                and (modifier.kind == "forward_cell" or modifier.kind == "add_damage"),
                "invalid ability modifier")
        end
        for _, other in ipairs(def.incompatible or {}) do
            assert(other ~= id and definitions[other], "invalid boon incompatibility")
        end
    end
    for id in pairs(definitions) do assert(seen[id], "unlisted boon definition") end
    return true
end

function Catalog.get(id) return Catalog.definitions[id] end

return Catalog
