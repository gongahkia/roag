-- Data-only OW-05 construction catalogue.  World behaviour comes from the
-- ordinary referenced world-object definitions and their material state.
return {
  {
    id = "construction.timber_wall",
    display_name = "Timber Wall",
    world_object_id = "world_object.build.timber_wall",
    costs = { ["resource.material.timber"] = 2 },
  },
  {
    id = "construction.masonry_wall",
    display_name = "Masonry Wall",
    world_object_id = "world_object.build.masonry_wall",
    costs = { ["resource.material.masonry"] = 3 },
  },
  {
    id = "construction.barricade",
    display_name = "Barricade",
    world_object_id = "world_object.build.barricade",
    costs = { ["resource.material.timber"] = 2, ["resource.material.metal"] = 1 },
  },
  {
    id = "construction.door",
    display_name = "Door",
    world_object_id = "world_object.build.door",
    costs = { ["resource.material.metal"] = 2, ["resource.material.timber"] = 1 },
  },
  {
    id = "construction.storage_crate",
    display_name = "Storage Crate",
    world_object_id = "world_object.build.storage_crate",
    costs = { ["resource.material.timber"] = 2, ["resource.material.metal"] = 1 },
  },
  {
    id = "construction.generator",
    display_name = "Generator",
    world_object_id = "world_object.build.generator",
    costs = { ["resource.material.metal"] = 4 },
  },
  {
    id = "construction.breaker",
    display_name = "Breaker",
    world_object_id = "world_object.build.breaker",
    costs = { ["resource.material.metal"] = 2 },
  },
  {
    id = "construction.reconstruction_station",
    display_name = "Reconstruction Station",
    world_object_id = "world_object.station.reconstruction",
    costs = { ["resource.material.metal"] = 6, ["resource.material.masonry"] = 3 },
  },
}
