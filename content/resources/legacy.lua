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
}
