-- Small first-party physical cover set. Object durability comes from the
-- referenced material; these definitions only describe the object's role.
return {
  {
    id = "world_object.cover.masonry_barricade",
    display_name = "Masonry Barricade",
    material_id = "material.structure.masonry",
    blocks_movement = true,
    blocks_vision = true,
    blocks_projectiles = true,
    blocks_gas = false,
    movable_by_force = false,
    render_style = "barricade",
  },
  {
    id = "world_object.cover.timber_crate",
    display_name = "Timber Crate",
    material_id = "material.structure.wood",
    blocks_movement = true,
    blocks_vision = true,
    blocks_projectiles = true,
    blocks_gas = false,
    movable_by_force = true,
    render_style = "crate",
  },
  {
    id = "world_object.cover.conductive_metal_crate",
    display_name = "Conductive Metal Crate",
    material_id = "material.structure.conductive_metal",
    blocks_movement = true,
    blocks_vision = true,
    blocks_projectiles = true,
    blocks_gas = false,
    movable_by_force = true,
    render_style = "metal_crate",
  },
}
