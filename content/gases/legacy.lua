-- First finite atmospheric medium. Mutable concentration belongs to World;
-- this declarative definition only describes the shared physical substance.
return {
  {
    id = "gas.toxic.legacy",
    display_name = "Toxic Gas",
    max_concentration = 4,
    exposure_threshold = 2,
    damage = 1,
    render_style = "toxic_cloud",
  },
}
