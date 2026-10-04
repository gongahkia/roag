-- Initial conventional tools.  These are portable cargo, not body parts: a
-- player may carry, store, drop, lose, and recover the exact same tool.
return {
  {
    id = "tool.axe", display_name = "Axe", family = "axe",
    mass = 1.2, max_durability = 10,
    inventory = { width = 1, height = 3, rotatable = true },
    -- A field tool is deliberately not the equal of the installed Impact
    -- Blade: it can hurt an enemy, but lacks the blade's knockback.
    combat = { damage = 1, force = 0 },
    modification = { damage = 2 },
  },
  {
    id = "tool.pickaxe", display_name = "Pickaxe", family = "pickaxe",
    mass = 1.4, max_durability = 12,
    inventory = { width = 2, height = 2, rotatable = true, shape = { "11", "10" } },
    combat = { damage = 1, force = 0 },
    modification = { damage = 1 },
  },
  {
    id = "tool.cutter", display_name = "Cutter", family = "cutter",
    mass = 1.0, max_durability = 9,
    inventory = { width = 1, height = 2, rotatable = true },
    combat = { damage = 1, force = 0 },
    modification = { damage = 2 },
  },
  {
    id = "tool.drill", display_name = "Drill", family = "drill",
    mass = 1.8, max_durability = 14,
    inventory = { width = 2, height = 2, rotatable = true, shape = { "11", "01" } },
    combat = { damage = 2, force = 0 },
    modification = { damage = 2 },
  },
}
