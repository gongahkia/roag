-- The current fixed anatomy. Future topology definitions may add or replace
-- slots without changing the Body implementation.
return {
  {
    id = "body.topology.normal",
    display_name = "Normal Anatomy",
    slots = {
      { id = "head", kind = "head" },
      { id = "torso_core", kind = "torso_core" },
      { id = "left_arm", kind = "arm" },
      { id = "right_arm", kind = "arm" },
      { id = "left_leg", kind = "leg" },
      { id = "right_leg", kind = "leg" },
      { id = "internal_1", kind = "internal" },
      { id = "internal_2", kind = "internal" },
    },
  },
}
