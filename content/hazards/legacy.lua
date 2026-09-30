-- Initial deterministic, kinetic-only hazard vocabulary. Effects remain
-- declarative; the authoritative resolver owns simulation behavior.
return {
  {
    id = "hazard.legacy.spike_field",
    display_name = "Spike Field",
    trigger = "on_enter",
    effect = {
      type = "kinetic_damage",
      amount = 1,
    },
    render_style = "spikes",
  },
}
