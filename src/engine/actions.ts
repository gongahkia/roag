export type ActionOwner = 'player' | 'enemy'
export type ActionShape = 'adjacent' | 'line' | 'cone' | 'burst' | 'cross'
export type ActionResolver = 'melee' | 'projectile' | 'throw' | 'bomb' | 'script' | 'move'
export type ActionCost = { resource: 'none' } | { resource: 'focus' | 'bomb' | 'item'; amount: number }

export interface ActionDefinition {
  id: string
  owner: ActionOwner
  name: string
  cost: ActionCost
  range: number
  shape: ActionShape
  tags: string[]
  resolver: ActionResolver
}

export const PLAYER_ACTIONS: ActionDefinition[] = [
  { id: 'player-strike', owner: 'player', name: 'Strike', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['melee'], resolver: 'melee' },
  { id: 'player-throw', owner: 'player', name: 'Throw', cost: { resource: 'item', amount: 1 }, range: 5, shape: 'line', tags: ['ranged', 'thrown'], resolver: 'throw' },
  { id: 'player-bomb', owner: 'player', name: 'Bomb', cost: { resource: 'bomb', amount: 1 }, range: 1, shape: 'burst', tags: ['area', 'terrain'], resolver: 'bomb' },
  { id: 'player-script', owner: 'player', name: 'Script', cost: { resource: 'focus', amount: 3 }, range: 1, shape: 'adjacent', tags: ['script'], resolver: 'script' }
]

export const ENEMY_ACTIONS: ActionDefinition[] = [
  { id: 'enemy-strike', owner: 'enemy', name: 'Strike', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['melee'], resolver: 'melee' },
  { id: 'enemy-shot', owner: 'enemy', name: 'Shot', cost: { resource: 'none' }, range: 7, shape: 'line', tags: ['ranged', 'telegraphed'], resolver: 'projectile' },
  { id: 'enemy-root', owner: 'enemy', name: 'Root', cost: { resource: 'none' }, range: 4, shape: 'line', tags: ['ranged', 'telegraphed', 'control'], resolver: 'script' },
  { id: 'enemy-web', owner: 'enemy', name: 'Snare', cost: { resource: 'none' }, range: 6, shape: 'line', tags: ['ranged', 'telegraphed', 'terrain'], resolver: 'script' },
  { id: 'enemy-fire', owner: 'enemy', name: 'Fire Line', cost: { resource: 'none' }, range: 5, shape: 'line', tags: ['ranged', 'telegraphed', 'terrain'], resolver: 'script' },
  { id: 'enemy-pull', owner: 'enemy', name: 'Pull', cost: { resource: 'none' }, range: 5, shape: 'line', tags: ['ranged', 'telegraphed', 'control'], resolver: 'script' },
  { id: 'enemy-ward', owner: 'enemy', name: 'Ward', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['defense', 'ritual'], resolver: 'script' },
  { id: 'enemy-dart', owner: 'enemy', name: 'Dart Line', cost: { resource: 'none' }, range: 6, shape: 'line', tags: ['ranged', 'telegraphed', 'terrain'], resolver: 'script' },
  { id: 'enemy-lock', owner: 'enemy', name: 'Seal Door', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['terrain', 'control'], resolver: 'script' },
  { id: 'enemy-ritual', owner: 'enemy', name: 'Marking Ritual', cost: { resource: 'none' }, range: 5, shape: 'line', tags: ['ranged', 'telegraphed', 'control'], resolver: 'script' },
  { id: 'foreman-cavein', owner: 'enemy', name: 'Cave-In', cost: { resource: 'none' }, range: 5, shape: 'line', tags: ['guardian', 'telegraphed', 'terrain'], resolver: 'script' },
  { id: 'heartwood-charge', owner: 'enemy', name: 'Bramble Charge', cost: { resource: 'none' }, range: 5, shape: 'line', tags: ['guardian', 'telegraphed', 'movement', 'terrain'], resolver: 'script' },
  { id: 'geode-fissure', owner: 'enemy', name: 'Fissure Line', cost: { resource: 'none' }, range: 6, shape: 'line', tags: ['guardian', 'telegraphed', 'terrain', 'control'], resolver: 'script' },
  { id: 'guardian-slam', owner: 'enemy', name: 'Slam', cost: { resource: 'none' }, range: 1, shape: 'cross', tags: ['guardian', 'area'], resolver: 'melee' },
  { id: 'enemy-approach', owner: 'enemy', name: 'Advance', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['movement'], resolver: 'move' },
  { id: 'enemy-reposition', owner: 'enemy', name: 'Reposition', cost: { resource: 'none' }, range: 1, shape: 'adjacent', tags: ['movement', 'terrain'], resolver: 'move' }
]

export const ACTIONS: ActionDefinition[] = [...PLAYER_ACTIONS, ...ENEMY_ACTIONS]
export const actionById = (id: string): ActionDefinition | undefined => ACTIONS.find(action => action.id === id)
