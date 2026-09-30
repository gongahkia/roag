-- First shallow-liquid definition.  Its depth is a small discrete amount,
-- while World owns every mutable cell instance and all flow state.
return {
  {
    id = "liquid.water.legacy",
    display_name = "Shallow Water",
    max_depth = 3,
    extinguishes_fire = true,
    render_style = "water",
  },
}
