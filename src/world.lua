-- Small serializable exploration interactions. Definitions are editor-facing data;
-- the resolver receives canonical combat/movement events after primary HP batches.
local Content = require("src.content")
local Rewards = require("src.rewards")

local World = {}
World.version = "roeg-world/1"
World.order = {"core:secret/cracked_cache", "core:secret/moss_sigil"}
World.definitions = {
    ["core:secret/cracked_cache"] = {
        id="core:secret/cracked_cache", trigger="AttackPerformed",
        ability_id="core:ability/basic_attack", clue="cracked_moss_wall",
        reward="free_chest", reward_definition_id="core:chest/free_elite",
        description="A thin moss-lined crack yields to a sword strike.",
    },
    ["core:secret/moss_sigil"] = {
        id="core:secret/moss_sigil", trigger="TileEntered",
        clue="moss_sigil", reward="currency", amount=4,
        description="A repeated moss spiral releases a small cache underfoot.",
    },
}

function World.validate()
    local seen={}
    for _,id in ipairs(World.order) do
        local definition=assert(World.definitions[id],"missing secret definition")
        assert(not seen[id] and definition.id==id,"duplicate secret ID")
        seen[id]=true
        assert(definition.trigger=="AttackPerformed" or definition.trigger=="TileEntered"
            or definition.trigger=="ItemUsed",
            "invalid secret event trigger")
        if definition.trigger=="ItemUsed" then
            assert(type(definition.item_id)=="string" and #definition.item_id>0,
                "item-triggered secret needs item ID")
        end
        assert(type(definition.clue)=="string" and #definition.clue>0,
            "secret needs learnable clue")
        if definition.reward=="currency" then
            assert(type(definition.amount)=="number" and definition.amount%1==0
                and definition.amount>0,"invalid secret currency")
        else
            assert(definition.reward=="free_chest"
                and Content.get_chest(definition.reward_definition_id).price==0,
                "invalid secret free chest")
        end
    end
    return true
end

local function replace_tile(map,x,y,tile,visual)
    local row=map.rows[y]
    map.rows[y]=row:sub(1,x-1)..tile..row:sub(x+1)
    local art=map.visual_rows[y]
    map.visual_rows[y]=art:sub(1,x-1)..visual..art:sub(x+1)
end

function World.populate(state)
    state.secrets={}
    for _,placement in ipairs(state.generated_map and state.generated_map.placements.secrets or {}) do
        local secret={id=placement.id,definition_id=placement.definition_id,
            floor_id=state.active_floor_id,x=placement.x,y=placement.y,
            wall_x=placement.wall_x,wall_y=placement.wall_y,
            cache_x=placement.cache_x,cache_y=placement.cache_y,
            room_id=placement.room_id,revealed=false,reward_id=nil}
        state.secrets[#state.secrets+1]=secret
    end
end

-- Future item actions may emit ItemUsed {item_id,target_position,source_id,
-- action_id}; this event subscription seam requires no inventory or search key.
function World.on_event(state,event,emit,spawn_chest)
    if event.kind~="AttackPerformed" and event.kind~="TileEntered"
        and event.kind~="ItemUsed" then return false end
    if event.source_id~=state.player_id then return false end
    for _,secret in ipairs(state.secrets or {}) do
        local definition=World.definitions[secret.definition_id]
        if not secret.revealed and definition and definition.trigger==event.kind
            and (event.floor_id==secret.floor_id or event.target_position
                and event.target_position.floor_id==secret.floor_id) then
            local match=false
            if event.kind=="AttackPerformed" and not event.bump
                and event.ability_id==definition.ability_id then
                for _,cell in ipairs(event.target_cells or {}) do
                    if cell.x==secret.wall_x and cell.y==secret.wall_y then
                        match=true; break
                    end
                end
            elseif event.kind=="TileEntered" then
                match=event.x==secret.x and event.y==secret.y
            elseif event.kind=="ItemUsed" then
                local target=event.target_position
                match=event.item_id==definition.item_id and target
                    and target.x==secret.x and target.y==secret.y
            end
            if match then
                secret.revealed=true
                if definition.reward=="free_chest" then
                    local map=assert(state.generated_map,"wall secret needs concrete floor")
                    replace_tile(map,secret.wall_x,secret.wall_y,".","m")
                    replace_tile(map,secret.cache_x,secret.cache_y,".","m")
                else
                    replace_tile(state.generated_map,secret.x,secret.y,".","m")
                end
                emit("SecretDiscovered",{secret_id=secret.id,
                    definition_id=secret.definition_id,trigger=event.kind,
                    floor_id=secret.floor_id,x=secret.x,y=secret.y,
                    wall_x=secret.wall_x,wall_y=secret.wall_y,
                    cache_x=secret.cache_x,cache_y=secret.cache_y,
                    parent_event_id=event.id},event.source_id)
                if definition.reward=="free_chest" then
                    local chest=spawn_chest(definition.reward_definition_id,
                        secret.cache_x,secret.cache_y,"secret:"..secret.id)
                    secret.reward_id=chest.id
                    emit("SecretRewardSpawned",{secret_id=secret.id,
                        reward="free_chest",chest_id=chest.id,
                        floor_id=secret.floor_id,x=secret.cache_x,y=secret.cache_y},
                        event.source_id)
                else
                    local drop=Rewards.spawn_currency(state,definition.amount,
                        secret.floor_id,secret.x,secret.y,emit,event.source_id,
                        secret.id)
                    secret.reward_id=drop.id
                    local player
                    for _,entity in ipairs(state.entities) do
                        if entity.id==state.player_id then player=entity; break end
                    end
                    Rewards.pickup_at(state,player,secret.x,secret.y,emit)
                end
                return true
            end
        end
    end
    return false
end

return World
