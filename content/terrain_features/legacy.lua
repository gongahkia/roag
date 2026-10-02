-- Authored terrain-landmark composition.  This is intentionally a small,
-- bounded content profile, not a generic map scripting language.  Each kind
-- maps to existing physical terrain, water, or world-object behavior.
return {
  forest = {
    { id = "landmark.forest.old_growth_grove", kind = "grove", definition_id = "world_object.feature.old_growth_tree", count = 6 },
    { id = "landmark.forest.granite_ridge", kind = "ridge", definition_id = "world_object.feature.granite_boulder", count = 4 },
    { id = "landmark.forest.fallen_timber", kind = "scatter", definition_id = "world_object.feature.fallen_log", count = 2 },
    { id = "landmark.forest.stream", kind = "stream", liquid_id = "liquid.water.legacy", count = 9 },
  },
  cave = {
    { id = "landmark.cave.pillar_field", kind = "grove", definition_id = "world_object.feature.stalagmite", count = 5 },
    { id = "landmark.cave.rockfall", kind = "ridge", definition_id = "world_object.feature.granite_boulder", count = 3 },
    { id = "landmark.cave.underground_run", kind = "stream", liquid_id = "liquid.water.legacy", count = 7 },
  },
  dungeon = {
    { id = "landmark.dungeon.collapsed_gallery", kind = "ridge", definition_id = "world_object.feature.rubble_pile", count = 4 },
    { id = "landmark.dungeon.memorial_court", kind = "scatter", definition_id = "world_object.feature.ruined_statue", count = 2 },
    { id = "landmark.dungeon.abandoned_stores", kind = "scatter", definition_id = "world_object.cover.timber_crate", count = 2 },
  },
  reactor = {
    { id = "landmark.reactor.machine_bay", kind = "ridge", definition_id = "world_object.feature.machine_bank", count = 3 },
    { id = "landmark.reactor.coolant_channel", kind = "stream", liquid_id = "liquid.water.legacy", count = 8 },
    { id = "landmark.reactor.exposed_conduit", kind = "stream_object", definition_id = "world_object.feature.cable_trunk", count = 6 },
  },
}
