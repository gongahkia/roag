/** Colony tuning, separate from immutable artwork identity and old party clock units. */
export const BOSS_CONTENT = {
  maxEncounters: 4, maxFeatures: 48, maxEffects: 96,
  profiles: {
    bellkeeper: { health: 180, damage: 12, windup: 20, cooldown: 60, notice: 16 },
    sluice: { health: 220, damage: 12, windup: 20, cooldown: 60, notice: 16 },
    threadmother: { health: 180, damage: 12, windup: 20, cooldown: 60, notice: 16 },
  },
  featureHealth: { resonator: 24, valve: 1, anchor: 24, web: 12, line: 1 },
  counters: { strike: 10, drain: 20, cut: 10, ignite: 24 },
  counterDamage: 12,
  exposure: { ticks: 40, percent: 50 },
  noise: { lifetime: 500, hearing: 16, wallCost: 5 },
  soundseek: { windup: 20, damage: 12 },
  ring: { windup: 20, gap: 10, radii: 3, damage: 8 },
  surge: { windup: 20, damage: 12 },
  feast: { windup: 30, heal: 24, travelTicks: 120 },
  partition: { windup: 20 },
  reel: { windup: 20, damage: 12, range: 16, pull: 2, bleedTicks: 120 },
  fire: { windup: 8, gap: 4, damage: 6, maxDelay: 200 },
  movement: { ordinary: 4, wet: 6, web: 8 },
} as const;
