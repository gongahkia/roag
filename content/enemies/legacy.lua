return {
  {
    id = "enemy.legacy.bomber",
    display_name = "Bomber",
    body_topology_id = "body.topology.normal",
    installed_components = {
      { slot_id = "left_leg", component_id = "component.leg.legacy_locomotor" },
      { slot_id = "right_leg", component_id = "component.leg.legacy_locomotor" },
      { slot_id = "internal_1", component_id = "component.internal.legacy_volatile_charge" },
    },
  },
  {
    id = "enemy.legacy.cultist",
    display_name = "Cultist",
    body_topology_id = "body.topology.normal",
    installed_components = {
      { slot_id = "right_arm", component_id = "component.arm.legacy_arcane_projector" },
      { slot_id = "internal_1", component_id = "component.internal.legacy_shock_coil" },
      { slot_id = "left_leg", component_id = "component.leg.legacy_locomotor" },
      { slot_id = "right_leg", component_id = "component.leg.legacy_locomotor" },
    },
  },
}
