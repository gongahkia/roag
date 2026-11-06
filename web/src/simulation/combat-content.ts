/** Colony timing; retained legacy rules are locked cells, bounded geometry and stable ties. */
export const COMBAT_CONTENT = {
  warningAt: 10800, spawnAt: 12000, maxRaiders: 24, maxAttacks: 64,
  searchLimit: 4096, searchesPerTick: 2,
  tower: { range: 10, damage: 18, windup: 3, cooldown: 12 },
  trap: { damage: 32, windup: 1, cooldown: 5 },
  guard: { damage: 12, windup: 3, cooldown: 10, pursuit: 5 },
  raider: { health: 36, damage: 12, windup: 5, cooldown: 14, notice: 6 },
  breacher: { health: 72, damage: 16, structureDamage: 25, windup: 8, cooldown: 18, notice: 1, defenseNotice: 3 },
  wave: ['raider', 'raider', 'breacher', 'raider', 'raider'],
} as const;
export function validateCombatContent(): void {
  if (COMBAT_CONTENT.warningAt >= COMBAT_CONTENT.spawnAt || COMBAT_CONTENT.wave.length > COMBAT_CONTENT.maxRaiders) throw Error('Invalid first raid schedule');
  for (const profile of [COMBAT_CONTENT.tower, COMBAT_CONTENT.trap, COMBAT_CONTENT.guard, COMBAT_CONTENT.raider, COMBAT_CONTENT.breacher])
    for (const value of Object.values(profile)) if (!Number.isSafeInteger(value) || value < 1) throw Error('Invalid combat tuning');
}
validateCombatContent();
