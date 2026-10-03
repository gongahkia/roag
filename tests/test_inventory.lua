local Inventory = require("src.inventory.inventory")

local function item(id, width, height, rotatable, mass, shape)
  return {
    item_type = "fixture",
    physical_id = id,
    display_name = id,
    footprint = { width = width, height = height, rotatable = rotatable, shape = shape },
    mass = mass or 1,
    to_data = function(self)
      return {
        item_type = self.item_type,
        physical_id = self.physical_id,
        width = self.footprint.width,
        height = self.footprint.height,
        rotatable = self.footprint.rotatable,
        mass = self.mass,
      }
    end,
  }
end

local function decode(data)
  return item(data.physical_id, data.width, data.height, data.rotatable, data.mass)
end

return {
  {
    name = "inventory placement rejects overlap and bounds without mutation",
    run = function()
      local inventory = Inventory.new({ width = 3, height = 2 })
      local first, second = item("item:first", 2, 1, false), item("item:second", 1, 1, false)
      assert(inventory:place(first, 1, 1))
      assert(not inventory:place(second, 2, 1))
      assert(not inventory:place(second, 4, 1))
      assert(#inventory.entries == 1)
      assert(inventory:item_at(1, 1).item == first)
      assert(inventory:item_at(3, 2) == nil)
    end,
  },
  {
    name = "inventory rotation obeys item rules and preserves valid state",
    run = function()
      local inventory = Inventory.new({ width = 3, height = 3 })
      local arm = item("item:arm", 1, 3, true)
      local core = item("item:core", 2, 2, false)
      assert(inventory:place(arm, 1, 1))
      local rotated = assert(inventory:rotate(arm.physical_id))
      assert(rotated.rotated)
      assert(inventory:footprint(arm, rotated.rotated) == 3)
      assert(inventory:place(core, 2, 2))
      local failed = inventory:rotate(core.physical_id)
      assert(not failed)
      assert(not inventory:get(core.physical_id).rotated)
    end,
  },
  {
    name = "inventory move and remove clear old cells without changing identity",
    run = function()
      local inventory = Inventory.new({ width = 4, height = 2 })
      local module = item("item:module", 1, 1, false, 3)
      assert(inventory:place(module, 1, 1))
      assert(inventory:move(module.physical_id, 4, 2))
      assert(not inventory:item_at(1, 1))
      assert(inventory:item_at(4, 2).physical_id == "item:module")
      local removed = assert(inventory:remove(module.physical_id))
      assert(removed == module)
      assert(not inventory:item_at(4, 2))
      assert(inventory:total_mass() == 0)
    end,
  },
  {
    name = "irregular inventory footprints occupy only their authored cells and rotate as shapes",
    run = function()
      local inventory = Inventory.new({ width = 3, height = 3 })
      local hook = item("item:hook", 2, 2, true, 1, { "11", "10" })
      local token = item("item:token", 1, 1, false)
      assert(inventory:place(hook, 1, 1))
      assert(inventory:item_at(1, 1).physical_id == hook.physical_id)
      assert(inventory:item_at(2, 1).physical_id == hook.physical_id)
      assert(inventory:item_at(1, 2).physical_id == hook.physical_id)
      assert(not inventory:item_at(2, 2), "The hole in an L footprint must remain usable")
      assert(inventory:place(token, 2, 2))
      assert(inventory:remove(token.physical_id))
      assert(inventory:move(hook.physical_id, 1, 1, true))
      assert(inventory:item_at(1, 1).physical_id == hook.physical_id)
      assert(not inventory:item_at(1, 2), "Rotation must move the footprint hole")
      assert(inventory:item_at(2, 2).physical_id == hook.physical_id)
    end,
  },
  {
    name = "inventory automatic placement is deterministic with rotation fallback",
    run = function()
      local first, second = Inventory.new({ width = 2, height = 1 }), Inventory.new({ width = 2, height = 1 })
      local first_item, second_item = item("item:tall", 1, 2, true), item("item:tall", 1, 2, true)
      local first_entry, second_entry = assert(first:auto_place(first_item)), assert(second:auto_place(second_item))
      assert(first_entry.x == 1 and first_entry.y == 1 and first_entry.rotated)
      assert(second_entry.x == first_entry.x and second_entry.y == first_entry.y and second_entry.rotated == first_entry.rotated)
      assert(not first:auto_place(item("item:extra", 1, 1, false)))
    end,
  },
  {
    name = "inventory mass and encumbrance are derived from carried entries",
    run = function()
      local inventory = Inventory.new({
        width = 5,
        height = 1,
        thresholds = { burdened = 2, heavy = 4, overloaded = 6 },
      })
      assert(inventory:encumbrance() == "LIGHT")
      assert(inventory:place(item("item:one", 1, 1, false, 2), 1, 1))
      assert(inventory:total_mass() == 2 and inventory:encumbrance() == "BURDENED")
      assert(inventory:place(item("item:two", 1, 1, false, 2), 2, 1))
      assert(inventory:encumbrance() == "HEAVY")
      assert(inventory:place(item("item:three", 1, 1, false, 2), 3, 1))
      assert(inventory:encumbrance() == "OVERLOADED")
      assert(inventory:remove("item:three"))
      assert(inventory:total_mass() == 4 and inventory:encumbrance() == "HEAVY")
    end,
  },
  {
    name = "inventory data round trip preserves placements and physical identities",
    run = function()
      local inventory = Inventory.new({ width = 4, height = 3 })
      assert(inventory:place(item("item:beta", 1, 2, true, 2), 3, 1, true))
      assert(inventory:place(item("item:alpha", 2, 1, false, 1), 1, 2))
      local restored = Inventory.from_data(inventory:to_data(), decode)
      assert(restored.width == 4 and restored.height == 3)
      assert(restored:get("item:alpha").x == 1)
      assert(restored:get("item:beta").rotated)
      assert(restored:total_mass() == 3)
    end,
  },
}
