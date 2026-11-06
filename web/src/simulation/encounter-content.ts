/** Adapted squad and Captain rules; all durations are deterministic colony ticks. */
export const ENCOUNTER_CONTENT = {
  profiles: {
    shield: { health: 90, damage: 10, windup: 6, cooldown: 20, notice: 8 },
    archer: { health: 36, damage: 10, windup: 8, cooldown: 24, notice: 10 },
    healer: { health: 45, damage: 4, windup: 8, cooldown: 24, notice: 8 },
    rusher: { health: 28, damage: 10, windup: 3, cooldown: 10, notice: 8 },
    captain: { health: 240, damage: 20, windup: 20, cooldown: 60, notice: 12 },
  },
  shield: { reductionPercent: 80, loweredTicks: 20 },
  archer: { minRange: 2, range: 10 },
  heal: { range: 4, amount: 16, windup: 12, cooldown: 30 },
  captain: { reductionPercent: 80, exposedPercent: 50, exposureTicks: 30, specialRange: 8 },
  charge: { range: 4, damage: 20, windup: 20, cooldown: 60 },
  rampart: { windup: 20, cooldown: 60, health: 36, lifetime: 600, maxBarricades: 8, forward: 2 },
} as const;

for (const section of Object.values(ENCOUNTER_CONTENT)) {
  if (section === ENCOUNTER_CONTENT.profiles) continue;
  for (const value of Object.values(section)) if (!Number.isSafeInteger(value) || (value as number) < 1) throw Error('Invalid encounter tuning');
}
