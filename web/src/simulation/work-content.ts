// Colony balance is local content; legacy party recipes are references, not runtime input.
export const MATERIALS = ['wood', 'stone', 'food', 'meal', 'ore', 'medicine', 'tool', 'ammo'] as const;
export type Material = typeof MATERIALS[number];
export type Stock = Record<Material, number>;
export const emptyStock = (): Stock => ({ wood: 0, stone: 0, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 });
export const total = (stock: Stock): number => MATERIALS.reduce((n, k) => n + stock[k], 0);
export const BUILDINGS = ['wall', 'door', 'storage', 'workshop', 'farm', 'kitchen', 'bed', 'infirmary', 'tower', 'trap', 'guardpost'] as const;
export type Building = typeof BUILDINGS[number];
export const WORK_CONTENT = {
  cargoCapacity: 5, stockpileCapacity: 30, extractionTicks: 8, repairTicks: 12, dismantleTicks: 12,
  salvagePercent: 75,
  buildings: {
    wall: { cost: { wood: 0, stone: 3, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 16, capacity: 0, sprite: 'rock' },
    door: { cost: { wood: 3, stone: 1, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 16, capacity: 0, sprite: 'cottage' },
    storage: { cost: { wood: 4, stone: 2, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 24, capacity: 60, sprite: 'wood' },
    workshop: { cost: { wood: 6, stone: 4, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 30, capacity: 0, sprite: 'hearth' },
    farm: { cost: { wood: 2, stone: 0, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 20, capacity: 0, sprite: 'wheat' },
    kitchen: { cost: { wood: 4, stone: 2, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 30, capacity: 0, sprite: 'hearth' },
    bed: { cost: { wood: 2, stone: 0, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 16, capacity: 0, sprite: 'bed' },
    tower: { cost: { ...emptyStock(), wood: 4, stone: 4 }, ticks: 30, capacity: 12, sprite: 'cottage' },
    trap: { cost: { ...emptyStock(), wood: 2, stone: 1 }, ticks: 16, capacity: 1, sprite: 'rock' },
    guardpost: { cost: { ...emptyStock(), wood: 1 }, ticks: 12, capacity: 0, sprite: 'wood' },
    infirmary: { cost: { wood: 4, stone: 2, food: 0, meal: 0, ore: 0, medicine: 0, tool: 0, ammo: 0 }, ticks: 30, capacity: 0, sprite: 'cottage' },
  },
} as const;
export function validateWorkContent(content: typeof WORK_CONTENT = WORK_CONTENT): void {
  for (const n of [content.cargoCapacity, content.stockpileCapacity, content.extractionTicks, content.repairTicks, content.dismantleTicks])
    if (!Number.isSafeInteger(n) || n < 1 || n > 1000) throw new Error('Invalid work limits');
  if (!Number.isSafeInteger(content.salvagePercent) || content.salvagePercent < 0 || content.salvagePercent > 100) throw new Error('Invalid salvage');
  if (Object.keys(content.buildings).sort().join() !== [...BUILDINGS].sort().join()) throw new Error('Invalid building catalog');
  for (const b of BUILDINGS) {
    const entry = content.buildings[b];
    if (!Number.isSafeInteger(entry.ticks) || entry.ticks < 1 || entry.ticks > 1000 || !Number.isSafeInteger(entry.capacity) || entry.capacity < 0 || entry.capacity > 1000 || !entry.sprite) throw new Error('Invalid building work');
    if (Object.keys(entry.cost).sort().join() !== [...MATERIALS].sort().join() || !MATERIALS.every(k => Number.isSafeInteger(entry.cost[k]) && entry.cost[k] >= 0 && entry.cost[k] <= 100) || total(entry.cost) === 0) throw new Error('Invalid building cost');
  }
}
validateWorkContent();
