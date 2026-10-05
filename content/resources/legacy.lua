-- OW-05 construction resources are deliberately few, physical, and portable.
-- They are not a campaign wallet: stacks live in ordinary inventories, corpses,
-- storage, or a zone's ground-item state.
return {
  {
    id = "resource.material.timber",
    display_name = "Timber",
    mass_per_unit = 0.15,
    max_stack = 16,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.material.masonry",
    display_name = "Masonry",
    mass_per_unit = 0.25,
    max_stack = 16,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.material.metal",
    display_name = "Metal",
    mass_per_unit = 0.35,
    max_stack = 16,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.ammo.bullets",
    display_name = "Bullets",
    mass_per_unit = 0.05,
    max_stack = 32,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.ammo.shells",
    display_name = "Shells",
    mass_per_unit = 0.12,
    max_stack = 20,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.ammo.energy_cells",
    display_name = "Energy Cells",
    mass_per_unit = 0.08,
    max_stack = 24,
    inventory = { width = 1, height = 1, rotatable = false },
  },
  {
    id = "resource.ammo.explosives",
    display_name = "Explosives",
    mass_per_unit = 0.30,
    max_stack = 12,
    inventory = { width = 1, height = 1, rotatable = false },
  },
}
