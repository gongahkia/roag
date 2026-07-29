export type ActionShape = 'adjacent' | 'line' | 'cone' | 'burst' | 'cross'

export interface ActionDefinition {
  id: string
  name: string
  tags: string[]
}

export const ACTIONS: ActionDefinition[] = [
  { id: 'player-strike', name: 'Strike', tags: ['melee'] },
  { id: 'player-throw', name: 'Throw', tags: ['ranged', 'thrown'] },
  { id: 'player-bomb', name: 'Bomb', tags: ['area', 'terrain'] },
  { id: 'player-script', name: 'Charm', tags: ['script'] },
  { id: 'enemy-strike', name: 'Strike', tags: ['melee'] },
  { id: 'enemy-shot', name: 'Shot', tags: ['ranged', 'telegraphed'] },
  { id: 'enemy-root', name: 'Root', tags: ['ranged', 'telegraphed', 'control'] },
  { id: 'enemy-web', name: 'Snare', tags: ['ranged', 'telegraphed', 'terrain'] },
  { id: 'enemy-fire', name: 'Fire Line', tags: ['ranged', 'telegraphed', 'terrain'] },
  { id: 'enemy-pull', name: 'Pull', tags: ['ranged', 'telegraphed', 'control'] },
  { id: 'enemy-ward', name: 'Ward', tags: ['defense', 'ritual'] },
  { id: 'enemy-dart', name: 'Dart Line', tags: ['ranged', 'telegraphed', 'terrain'] },
  { id: 'enemy-lock', name: 'Seal Door', tags: ['terrain', 'control'] },
  { id: 'enemy-ritual', name: 'Marking Ritual', tags: ['ranged', 'telegraphed', 'control'] },
  { id: 'foreman-cavein', name: 'Cave-In', tags: ['guardian', 'telegraphed', 'terrain'] },
  { id: 'heartwood-charge', name: 'Bramble Charge', tags: ['guardian', 'telegraphed', 'movement', 'terrain'] },
  { id: 'geode-fissure', name: 'Fissure Line', tags: ['guardian', 'telegraphed', 'terrain', 'control'] },
  { id: 'regent-ward', name: 'Keeper Ward', tags: ['guardian', 'defense', 'ritual'] },
  { id: 'regent-decree', name: 'Stone Decree', tags: ['guardian', 'telegraphed', 'terrain', 'control'] },
  { id: 'regent-judgment', name: 'Final Judgment', tags: ['guardian', 'telegraphed', 'terrain', 'control'] },
  { id: 'guardian-slam', name: 'Slam', tags: ['guardian', 'area'] },
  { id: 'enemy-approach', name: 'Advance', tags: ['movement'] },
  { id: 'enemy-reposition', name: 'Reposition', tags: ['movement', 'terrain'] }
]

export const actionById = (id: string): ActionDefinition | undefined => ACTIONS.find(action => action.id === id)
