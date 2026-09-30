-- Current prototype content, kept separate from behavior so it can later
-- migrate to data files without another simulation rewrite.
return {
  stages = {
    { level = 0, terrain = "forest", targets = 4, enemies = 6, score = 5, ammo = 2, vision = 6, torches = 3, wilds = true },
    -- One cultist makes the cave's existing shallow pools a restrained first
    -- electrical encounter, and leaves its physical shock coil available for
    -- reconstruction before the following dungeon floor.
    { level = 1, terrain = "cave", targets = 3, enemies = 1, score = 4, ammo = 1, vision = 5, torches = 3, cultists = true },
    { level = 2, terrain = "dungeon", targets = 1, enemies = 2, score = 5, ammo = 2, vision = 4, torches = 3, cultists = true },
  },
  classes = {
    { name = "VANGUARD", description = "TOUGH AND WELL-ARMED", modifiers = { health = 2, bombs = 1, flares = -1 } },
    { name = "GUNSLINGER", description = "EXTRA AMMO, LESS HEALTH", modifiers = { health = -1, ammo = 3 } },
    { name = "SCOUT", description = "BETTER SIGHT, FLARES, AND DASH", modifiers = { vision = 2, dash_cooldown = -1 } },
  },
  boons = {
    { name = "IRON HEART", description = "BEGIN EACH DESCENT WITH MORE HEALTH", modifiers = { health = 1 } },
    { name = "FULL QUIVER", description = "BEGIN EACH DESCENT WITH EXTRA AMMO", modifiers = { ammo = 2 } },
    { name = "DEMOLITION KIT", description = "BEGIN EACH DESCENT WITH AN EXTRA BOMB", modifiers = { bombs = 1 } },
    { name = "FLARE SATCHEL", description = "BEGIN EACH DESCENT WITH TWO EXTRA FLARES", modifiers = { flares = 2 } },
    { name = "EAGLE EYE", description = "SEE FURTHER THROUGH THE DARKNESS", modifiers = { vision = 2 } },
    { name = "WINDWALKER", description = "YOUR DASH RECHARGES MORE QUICKLY", modifiers = { dash_cooldown = -1 } },
  },
  curses = {
    { name = "FRAIL BODY", description = "BEGIN WITH ONE HEALTH", modifiers = { health = 1 } },
    { name = "DARKNESS", description = "REDUCED NATURAL VISION", modifiers = { vision = -3 } },
    { name = "HUNTED", description = "ONE EXTRA NECROMANCER", modifiers = { enemies = 1 } },
    { name = "RELENTLESS", description = "TWO EXTRA NECROMANCERS", modifiers = { enemies = 2 } },
    { name = "EMPTY CHAMBER", description = "START WITH LESS AMMO", modifiers = { ammo = -1 } },
    { name = "SPENT BOMBS", description = "START WITHOUT BOMBS", modifiers = { bombs = 0 } },
    { name = "LONG HUNT", description = "MORE TARGETS MUST FALL", modifiers = { score = 2 } },
    { name = "BLACKOUT", description = "NO TORCHES OR NATURAL LIGHT", modifiers = { vision = -99, torches = -99 } },
    { name = "GUTTERING TORCHES", description = "TORCHES BARELY REACH", modifiers = { torch_radius = 1 } },
    { name = "SLOW DASH", description = "DASH RECHARGES SLOWLY", modifiers = { dash_cooldown = 6 } },
    { name = "SPENT FLARES", description = "START WITHOUT FLARES", modifiers = { flares = 0 } },
    { name = "SMALL BLAST", description = "BOMBS HAVE LESS RANGE", modifiers = { bomb_radius = 1 } },
    { name = "SHORT FUSE", description = "BOMBS DETONATE QUICKLY", modifiers = { bomb_fuse = 1 } },
    { name = "RUSTED BARREL", description = "BULLETS FADE EARLY", modifiers = { bullet_range = 3 } },
    { name = "DRY RELOAD", description = "KILLS RESTORE LESS AMMO", modifiers = { reload_penalty = 1 } },
  },
  shop = {
    { name = "HEALTH", description = "BUY [B] / SELL [V] — MAX 5", key = "health", minimum = 1 },
    { name = "AMMO", description = "BUY [B] / SELL [V] — MAX 5", key = "ammo", minimum = 1 },
    { name = "BOMBS", description = "BUY [B] / SELL [V] — MAX 5", key = "bombs", minimum = 0 },
    { name = "FLARES", description = "BUY [B] / SELL [V] — MAX 5", key = "flares", minimum = 0 },
  },
}
