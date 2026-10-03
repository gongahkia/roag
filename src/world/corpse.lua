-- Corpses preserve the defeated actor's actual Body table. They are inert
-- world entities, not regenerated loot definitions.
local Body = require("src.body.body")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")

local Corpse = {}
Corpse.__index = Corpse

function Corpse.from_actor(corpse_id, actor, carried_inventory, provenance)
  assert(actor.body, "Only body-bearing actors can create physical corpses")
  provenance = provenance or {}
  local corpse = setmetatable({
    id = corpse_id,
    kind = "corpse",
    source_kind = actor.kind,
    source_actor_id = actor.content_id,
    x = actor.x,
    y = actor.y,
    body = actor.body,
    -- A player body is a complete physical recovery site. Ordinary enemy
    -- corpses omit this optional collection entirely, preserving their small
    -- historical shard shape rather than serializing empty cargo grids.
    carried_inventory = carried_inventory,
    fallen_archive_id = actor.fallen_archive_id,
    fallen_source_run_id = actor.fallen_source_run_id,
    source_body_id = provenance.source_body_id or actor.actor_id,
    campaign_id = provenance.campaign_id,
    death_zone_key = provenance.death_zone_key,
    death_cause = provenance.death_cause,
    lost_charm_ids = provenance.lost_charm_ids or {},
  }, Corpse)
  actor.body = nil
  return corpse
end

function Corpse:list_components()
  return self.body:list_installed_slots()
end

function Corpse:list_carried_components()
  local values = {}
  for _, entry in ipairs(self.carried_inventory and self.carried_inventory.entries or {}) do
    if entry.item.item_type == "component" then values[#values + 1] = entry end
  end
  return values
end

function Corpse:list_carried_items()
  return self.carried_inventory and self.carried_inventory.entries or {}
end

function Corpse:to_data()
  return {
    id = self.id,
    kind = self.kind,
    source_kind = self.source_kind,
    source_actor_id = self.source_actor_id,
    x = self.x,
    y = self.y,
    body = self.body:to_data(),
    carried_inventory = self.carried_inventory and self.carried_inventory:to_data() or nil,
    fallen_archive_id = self.fallen_archive_id,
    fallen_source_run_id = self.fallen_source_run_id,
    source_body_id = self.source_body_id,
    campaign_id = self.campaign_id,
    death_zone_key = self.death_zone_key,
    death_cause = self.death_cause,
    lost_charm_ids = self.lost_charm_ids,
  }
end

function Corpse.from_data(registry, data)
  assert(type(data) == "table" and type(data.id) == "string", "Corpse data must include a stable ID")
  return setmetatable({
    id = data.id,
    kind = "corpse",
    source_kind = data.source_kind,
    source_actor_id = data.source_actor_id,
    x = data.x,
    y = data.y,
    body = Body.from_data(registry, data.body),
    carried_inventory = data.carried_inventory and Inventory.from_data(data.carried_inventory, function(item)
      return PhysicalItem.from_data(item, registry)
    end) or nil,
    fallen_archive_id = data.fallen_archive_id,
    fallen_source_run_id = data.fallen_source_run_id,
    source_body_id = data.source_body_id,
    campaign_id = data.campaign_id,
    death_zone_key = data.death_zone_key,
    death_cause = data.death_cause,
    lost_charm_ids = data.lost_charm_ids or {},
  }, Corpse)
end

return Corpse
