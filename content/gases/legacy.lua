-- Finite toxic atmosphere proof. Concentration and harm are declarative;
-- World owns mutable coordinate state and the gas system owns diffusion.
return {
  {
    id = "gas.toxic.legacy",
    display_name = "Toxic Fumes",
    max_concentration = 4,
    exposure_threshold = 2,
    damage = 1,
    render_style = "toxic_fumes",
  },
}
