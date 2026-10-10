local BoonContent = require("src.boon_content")
local Boons = require("src.boons")
local Content = require("src.content")
local RNG = require("src.rng")

local Rewards = {}

local function copy(value)
    if type(value) ~= "table" then return value end
    local result = {}
    for key, child in pairs(value) do result[key] = copy(child) end
    return result
end

local function player(state)
    for _, actor in ipairs(state.entities) do
        if actor.id == state.player_id then return actor end
    end
    error("missing player")
end

-- Cumulative XP needed to leave the supplied level: 5, 15, 30, ... by default.
function Rewards.next_threshold(level)
    assert(type(level) == "number" and level % 1 == 0 and level >= 1,
        "invalid player level")
    local rules = Content.get_progression()
    return rules.first_threshold * level
        + rules.threshold_step * level * (level - 1) / 2
end

local function pick_stat(state)
    local rules = Content.get_progression()
    local total = 0
    for _, stat in ipairs(rules.stat_order) do total = total + rules.stat_weights[stat] end
    local roll = RNG.next_int(state.rng, total)
    for _, stat in ipairs(rules.stat_order) do
        roll = roll - rules.stat_weights[stat]
        if roll <= 0 then return stat end
    end
    error("invalid stat weights")
end

local function increase_stat(actor, stat, amount)
    if stat == "max_health" then
        local old = actor.max_hp
        actor.max_hp = old + amount
        actor.hp = math.min(actor.max_hp, actor.hp + amount)
        return old, actor.max_hp
    elseif stat == "damage" then
        local old, old_slash = actor.damage, actor.slash_damage
        actor.damage, actor.slash_damage = old + amount, old_slash + amount
        return old, actor.damage, {slash_old=old_slash, slash_new=actor.slash_damage}
    elseif stat == "defense" then
        local old = actor.defense
        actor.defense = old + amount
        return old, actor.defense
    elseif stat == "speed" then
        local old = actor.speed
        actor.speed = old + amount
        return old, actor.speed
    elseif stat == "crit_chance" then
        local old = actor.crit_chance_ppm
        actor.crit_chance_ppm = old + amount
        return old, actor.crit_chance_ppm
    elseif stat == "crit_damage" then
        local old = actor.crit_damage_percent
        actor.crit_damage_percent = old + amount
        return old, actor.crit_damage_percent
    end
    error("unknown growth stat")
end

function Rewards.gain_xp(state, amount, killed_event, emit)
    assert(type(amount) == "number" and amount % 1 == 0 and amount > 0,
        "invalid XP reward")
    local progress = state.progression
    local old_xp = progress.xp_total
    progress.xp_total = old_xp + amount
    emit("ExperienceGained", {
        amount=amount, old_xp=old_xp, new_xp=progress.xp_total,
        target_id=killed_event.target_id, target_definition_id=killed_event.target_definition_id,
        floor_id=killed_event.floor_id, x=killed_event.x, y=killed_event.y,
    }, state.player_id)
    local actor = player(state)
    while progress.xp_total >= Rewards.next_threshold(progress.level) do
        local old_level = progress.level
        progress.level = old_level + 1
        local stat = pick_stat(state)
        local amount_grown = Content.get_progression().growth[stat]
        local old_hp = actor.hp
        local old_value, new_value, extra = increase_stat(actor, stat, amount_grown)
        emit("LevelUp", {old_level=old_level, new_level=progress.level,
            xp_total=progress.xp_total, next_threshold=Rewards.next_threshold(progress.level),
            stat=stat, target_id=state.player_id}, state.player_id)
        local fields = {level=progress.level, stat=stat, amount=amount_grown,
            old_value=old_value, new_value=new_value,
            old_hp=stat == "max_health" and old_hp or nil,
            new_hp=stat == "max_health" and actor.hp or nil,
            target_id=state.player_id}
        if extra then for key, value in pairs(extra) do fields[key] = value end end
        emit("StatIncreased", fields, state.player_id)
    end
end

function Rewards.on_event(state, event, emit)
    if event.kind ~= "EntityKilled" then return end
    local definition = Content.get_enemy(event.target_definition_id)
    if not definition or state.rewarded_kills[event.target_id] then return end
    state.rewarded_kills[event.target_id] = true
    if event.source_id == state.player_id and event.source_faction == "player" then
        Rewards.gain_xp(state, definition.xp_reward, event, emit)
    end
    local drop = {
        id=state.next_drop_id, kind="currency", amount=definition.currency_drop,
        position={floor_id=event.floor_id, x=event.x, y=event.y}, collected=false,
        killed_entity_id=event.target_id,
    }
    state.next_drop_id = drop.id + 1
    state.drops[#state.drops + 1] = drop
    emit("CurrencyDropped", {drop_id=drop.id, amount=drop.amount,
        target_id=event.target_id, floor_id=event.floor_id, x=event.x, y=event.y},
        event.source_id)
end

function Rewards.pickup_at(state, actor, x, y, emit)
    for _, drop in ipairs(state.drops) do
        local position = drop.position
        if not drop.collected and position.floor_id == actor.position.floor_id
            and position.x == x and position.y == y then
            local old = state.currency
            state.currency = old + drop.amount
            drop.collected = true
            emit("CurrencyCollected", {drop_id=drop.id, amount=drop.amount,
                old_currency=old, new_currency=state.currency,
                floor_id=position.floor_id, x=x, y=y}, actor.id)
        end
    end
end

local function roll_rarity(state, weights)
    local total = 0
    for _, rarity in ipairs(BoonContent.rarities) do total = total + weights[rarity] end
    local roll = RNG.next_int(state.rng, total)
    for _, rarity in ipairs(BoonContent.rarities) do
        roll = roll - weights[rarity]
        if roll <= 0 then return rarity end
    end
    error("invalid rarity weights")
end

function Rewards.make_offers(state, chest_definition)
    assert(#BoonContent.order >= 3, "chest pool needs three boons")
    local pool = {}
    for index, id in ipairs(BoonContent.order) do pool[index] = id end
    local offers = {}
    for index = 1, 3 do
        local selected = RNG.next_int(state.rng, #pool)
        offers[index] = {boon_id=table.remove(pool, selected),
            rarity=roll_rarity(state, chest_definition.rarity_weights)}
    end
    return offers
end

function Rewards.validate_chest(chest)
    local definition = assert(Content.get_chest(chest.definition_id), "unknown chest definition")
    assert(chest.controller == "chest" and type(chest.claimed) == "boolean"
        and chest.price == definition.price and type(chest.offers) == "table"
        and #chest.offers == 3, "invalid chest state")
    assert(chest.blocks_movement == not chest.claimed, "invalid chest collision")
    local seen = {}
    for _, offer in ipairs(chest.offers) do
        assert(BoonContent.get(offer.boon_id) and not seen[offer.boon_id],
            "invalid chest offer")
        seen[offer.boon_id] = true
        local valid = false
        for _, rarity in ipairs(BoonContent.rarities) do
            if offer.rarity == rarity then valid = true end
        end
        assert(valid, "invalid chest offer rarity")
    end
    return true
end

function Rewards.chest_options(state, chest)
    Rewards.validate_chest(chest)
    if chest.claimed then return nil, "chest already claimed" end
    local actor = player(state)
    local a, b = actor.position, chest.position
    if a.floor_id ~= b.floor_id or math.abs(a.x - b.x) + math.abs(a.y - b.y) ~= 1 then
        return nil, "chest is not adjacent"
    end
    return {chest_id=chest.id, name=Content.get_chest(chest.definition_id).name,
        price=chest.price, currency=state.currency,
        shortfall=math.max(0, chest.price - state.currency),
        offers=copy(chest.offers)}
end

function Rewards.can_claim(state, chest, index)
    local options, reason = Rewards.chest_options(state, chest)
    if not options then return false, reason end
    if type(index) ~= "number" or index % 1 ~= 0 or index < 1 or index > 3 then
        return false, "choose offer 1, 2 or 3"
    end
    if options.shortfall > 0 then
        return false, "need " .. options.shortfall .. " more currency"
    end
    local offer = options.offers[index]
    local actor = player(state)
    local probe = {boons=copy(actor.boons)}
    local current = probe.boons[offer.boon_id]
        and probe.boons[offer.boon_id][offer.rarity] or 0
    local ok = pcall(Boons.set_stacks, probe, offer.boon_id, offer.rarity, current + 1)
    if not ok then return false, "incompatible boon" end
    return true
end

function Rewards.claim(state, chest, index, emit)
    local valid, reason = Rewards.can_claim(state, chest, index)
    assert(valid, reason)
    local offer = chest.offers[index]
    local actor = player(state)
    local old_currency = state.currency
    local old_count = actor.boons[offer.boon_id]
        and actor.boons[offer.boon_id][offer.rarity] or 0
    Boons.set_stacks(actor, offer.boon_id, offer.rarity, old_count + 1)
    state.currency = old_currency - chest.price
    chest.claimed = true
    chest.blocks_movement = false
    emit("CurrencySpent", {chest_id=chest.id, amount=chest.price,
        old_currency=old_currency, new_currency=state.currency}, actor.id)
    emit("BoonGranted", {chest_id=chest.id, boon_id=offer.boon_id,
        rarity=offer.rarity, old_count=old_count, new_count=old_count + 1}, actor.id)
    emit("ChestClaimed", {chest_id=chest.id, boon_id=offer.boon_id,
        rarity=offer.rarity, price=chest.price, offer_index=index,
        floor_id=chest.position.floor_id, x=chest.position.x, y=chest.position.y}, actor.id)
    return offer
end

return Rewards
