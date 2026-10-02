-- Bounded world affiliations.  Relationships are explicit rather than being
-- inferred from differing IDs, so a later friendly or neutral group can be
-- authored without changing combat code.
return {
  {
    id = "faction.player",
    display_name = "Reclaimer",
    hostile_faction_ids = { "faction.feral", "faction.cult", "faction.machine", "faction.echo" },
    presentation = { color = "cyan" },
  },
  {
    id = "faction.feral",
    display_name = "Ravagers",
    hostile_faction_ids = { "faction.player", "faction.cult", "faction.machine", "faction.echo" },
    presentation = { color = "amber" },
  },
  {
    id = "faction.cult",
    display_name = "Altered",
    hostile_faction_ids = { "faction.player", "faction.feral", "faction.machine", "faction.echo" },
    presentation = { color = "violet" },
  },
  {
    id = "faction.machine",
    display_name = "Machine Remnants",
    hostile_faction_ids = { "faction.player", "faction.feral", "faction.cult", "faction.echo" },
    presentation = { color = "steel" },
  },
  -- Historical echoes are hostile encounter actors, not an additional
  -- ecology population.  Keeping the affiliation declarative prevents a
  -- dead player's former body from inheriting the player faction.
  {
    id = "faction.echo",
    display_name = "Fallen Echo",
    hostile_faction_ids = { "faction.player", "faction.feral", "faction.cult", "faction.machine" },
    synthetic = true,
    presentation = { color = "violet" },
  },
}
