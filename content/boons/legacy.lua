return {
  { id = "boon.legacy.iron_heart", display_name = "Iron Heart", description = "+2 maximum HP while equipped.", modifiers = { max_health = 2 } },
  { id = "boon.legacy.windwalker", display_name = "Windwalker", description = "Dash cooldown -1 while equipped.", modifiers = { dash_cooldown = -1 } },
  { id = "boon.legacy.demolition", display_name = "Demolition Kit", description = "Bomb radius +1 while equipped.", modifiers = { bomb_radius = 1 } },
  { id = "boon.legacy.flare_lens", display_name = "Flare Lens", description = "Flare light +2 while equipped.", modifiers = { flare_light = 2 } },
  -- Legacy route mode retains the historical reserve-ammo modifier. Campaign
  -- uses the owning charm's declarative on_reload effect instead.
  { id = "boon.legacy.quick_reload", display_name = "Quick Reload", description = "Legacy reloads reward +1 ammo; Campaign reloads reduce Dash recharge.", modifiers = { reload_bonus = 1 } },
  { id = "boon.legacy.kinetic_capacitor", display_name = "Kinetic Capacitor", description = "Melee force +1 while equipped.", modifiers = { melee_force = 1 } },
  { id = "boon.legacy.edge_tuning", display_name = "Edge Tuning", description = "Melee damage +1 while equipped.", modifiers = { melee_damage = 1 } },
  { id = "boon.legacy.ballistic_lens", display_name = "Ballistic Lens", description = "Projectile damage +1 while equipped.", modifiers = { projectile_damage = 1 } },
  { id = "boon.legacy.pathfinder", display_name = "Pathfinder", description = "Legacy routes need one fewer objective; Campaign terrain breaks recharge Dash.", modifiers = { objective_required = -1 } },
}
