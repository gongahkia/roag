-- Legacy physical parts give the current prototype actors a real, shared
-- body model without changing their present balance.
return {
  {
    id = "component.head.legacy_optic",
    display_name = "Legacy Optic",
    compatible_slots = { "head" },
    max_integrity = 3,
    mass = 1,
    abilities = {},
  },
  {
    id = "component.core.legacy_frame",
    display_name = "Legacy Core Frame",
    compatible_slots = { "torso_core" },
    max_integrity = 5,
    mass = 4,
    abilities = {},
  },
  {
    id = "component.arm.legacy_manipulator",
    display_name = "Legacy Manipulator",
    compatible_slots = { "arm" },
    max_integrity = 3,
    mass = 2,
    abilities = {},
  },
  {
    id = "component.leg.legacy_locomotor",
    display_name = "Legacy Locomotor",
    compatible_slots = { "leg" },
    max_integrity = 3,
    mass = 2,
    abilities = {},
  },
  {
    id = "component.internal.legacy_support",
    display_name = "Legacy Support System",
    compatible_slots = { "internal" },
    max_integrity = 2,
    mass = 1,
    abilities = {},
  },
  {
    id = "component.internal.legacy_volatile_charge",
    display_name = "Volatile Charge",
    compatible_slots = { "internal" },
    max_integrity = 1,
    mass = 1,
    abilities = { "ability.explosive.self_destruct" },
  },
}
