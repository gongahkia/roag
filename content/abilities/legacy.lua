-- Declarative ability definitions. Runtime systems bind these IDs to behavior;
-- content never supplies executable callbacks.
return {
  {
    id = "ability.explosive.self_destruct",
    display_name = "Volatile Self-Destruct",
    implementation = "self_destruct",
    activation_type = "body",
  },
  {
    id = "ability.weapon.projectile.basic",
    display_name = "Basic Projectile",
    implementation = "projectile",
    activation_type = "direct",
    resource = { name = "ammo", amount = 1 },
  },
  {
    id = "ability.arcane.burst",
    display_name = "Arcane Burst",
    implementation = "area_burst",
    activation_type = "body",
    range = 3,
    radius = 1,
    delay = 3,
  },
  {
    id = "ability.locomotion.move",
    display_name = "Locomotion",
    implementation = "locomotion",
    activation_type = "direct",
  },
}
