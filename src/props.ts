import { ITEM } from './content'
import type { Biome, ItemId, Prop, PropEffectKind, PropHook, PropId, PropTag, TileKind } from './types'

export interface PropDefinition {
  id: PropId
  biome: Biome
  name: string
  description: string
  glyph: string
  color: string
  terrain: readonly TileKind[]
  tags: readonly PropTag[]
  hooks: readonly PropHook[]
  activationReward: ItemId
  effectReward: ItemId
}

const define = (definition: PropDefinition): PropDefinition => definition

export const PROP_DEFINITIONS = [
  define({ id: 'mine.oreVein', biome: 'mine', name: 'ore vein', description: 'A dense seam with a brittle mineral sheen.', glyph: 'O', color: '#d2b56f', terrain: ['floor', 'rail', 'support'], tags: ['salvage', 'force'], hooks: ['operate', 'bomb', 'force', 'throw'], activationReward: 'rock', effectReward: 'rock' }),
  define({ id: 'mine.lanternPost', biome: 'mine', name: 'lantern post', description: 'A guttering lamp marks a worked passage.', glyph: 'i', color: '#ffe18a', terrain: ['floor', 'rail'], tags: ['light', 'fire', 'hazard'], hooks: ['operate', 'fire', 'water', 'hazard'], activationReward: 'ember', effectReward: 'rock' }),
  define({ id: 'mine.brokenCart', biome: 'mine', name: 'broken cart', description: 'A splintered cart blocks a worked rail.', glyph: 'C', color: '#c9a06e', terrain: ['rail'], tags: ['route', 'force', 'salvage'], hooks: ['operate', 'bomb', 'force', 'throw'], activationReward: 'ropeBundle', effectReward: 'rock' }),
  define({ id: 'mine.warningMarker', biome: 'mine', name: 'warning marker', description: 'A painted stake warns of unstable ground.', glyph: '!', color: '#f0b56a', terrain: ['floor', 'rail', 'support'], tags: ['warning', 'hazard'], hooks: ['operate', 'fire', 'throw', 'hazard'], activationReward: 'mapScroll', effectReward: 'rock' }),
  define({ id: 'mine.skullMarker', biome: 'mine', name: 'skull marker', description: 'A miner’s warning stares from the dust.', glyph: 'x', color: '#ddd5c2', terrain: ['floor', 'rail'], tags: ['warning', 'hazard'], hooks: ['operate', 'bomb', 'throw', 'hazard'], activationReward: 'tonic', effectReward: 'rock' }),
  define({ id: 'mine.discardedParcel', biome: 'mine', name: 'discarded parcel', description: 'A sealed bundle lies under a film of ash.', glyph: '?', color: '#d9c27e', terrain: ['floor', 'support'], tags: ['cache', 'salvage'], hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'], activationReward: 'ropeBundle', effectReward: 'key' }),
  define({ id: 'wilds.mushrooms', biome: 'wilds', name: 'mushrooms', description: 'A bright cluster grows through the leaf litter.', glyph: 'm', color: '#b6df8a', terrain: ['floor', 'web'], tags: ['growth', 'root', 'fire'], hooks: ['operate', 'fire', 'water', 'root', 'throw'], activationReward: 'tonic', effectReward: 'focusTonic' }),
  define({ id: 'wilds.danglingCharm', biome: 'wilds', name: 'dangling charm', description: 'A weathered charm twists from a low branch.', glyph: 'o', color: '#ca9fe4', terrain: ['floor', 'web'], tags: ['ritual', 'root'], hooks: ['operate', 'fire', 'root', 'throw'], activationReward: 'wardScript', effectReward: 'root' }),
  define({ id: 'wilds.birdNest', biome: 'wilds', name: 'bird nest', description: 'A woven nest rustles above the path.', glyph: 'n', color: '#d8bc82', terrain: ['floor', 'web'], tags: ['growth', 'warning'], hooks: ['operate', 'fire', 'throw', 'hazard'], activationReward: 'tonic', effectReward: 'ropeBundle' }),
  define({ id: 'wilds.rootShrine', biome: 'wilds', name: 'root shrine', description: 'Roots curl around an offering stone.', glyph: '+', color: '#86c064', terrain: ['floor', 'web'], tags: ['ritual', 'root', 'growth'], hooks: ['operate', 'fire', 'water', 'root', 'force'], activationReward: 'root', effectReward: 'mend' }),
  define({ id: 'wilds.lostParcel', biome: 'wilds', name: 'lost parcel', description: 'A courier’s bundle is caught beneath ferns.', glyph: '?', color: '#dfcc91', terrain: ['floor', 'web'], tags: ['cache', 'route'], hooks: ['operate', 'fire', 'throw', 'hazard'], activationReward: 'ropeBundle', effectReward: 'tonic' }),
  define({ id: 'wilds.rootArch', biome: 'wilds', name: 'root arch', description: 'A living arch frames an overgrown trail.', glyph: 'A', color: '#71a66d', terrain: ['floor', 'web'], tags: ['route', 'growth', 'root'], hooks: ['operate', 'fire', 'root', 'force', 'throw'], activationReward: 'machete', effectReward: 'root' }),
  define({ id: 'caverns.crystalCluster', biome: 'caverns', name: 'crystal cluster', description: 'Facets catch every trace of cave light.', glyph: '*', color: '#8ce5f2', terrain: ['floor', 'darkness'], tags: ['salvage', 'force', 'light'], hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'], activationReward: 'sight', effectReward: 'rock' }),
  define({ id: 'caverns.glowingFungus', biome: 'caverns', name: 'glowing fungus', description: 'Blue fungus spills a cold local glow.', glyph: 'f', color: '#8fd6c2', terrain: ['floor', 'darkness'], tags: ['light', 'growth', 'water'], hooks: ['operate', 'fire', 'water', 'root', 'throw'], activationReward: 'focusTonic', effectReward: 'tonic' }),
  define({ id: 'caverns.barnacledShrine', biome: 'caverns', name: 'barnacled shrine', description: 'A salt-crusted shrine waits beside the tide.', glyph: '+', color: '#9cc9ce', terrain: ['floor', 'water', 'darkness'], tags: ['ritual', 'water'], hooks: ['operate', 'fire', 'water', 'force', 'hazard'], activationReward: 'waterScript', effectReward: 'wardScript' }),
  define({ id: 'caverns.brokenBoat', biome: 'caverns', name: 'broken boat', description: 'A half-sunk skiff is tangled in cave reeds.', glyph: 'b', color: '#c8a879', terrain: ['floor', 'water'], tags: ['route', 'water', 'salvage'], hooks: ['operate', 'bomb', 'water', 'force', 'throw'], activationReward: 'ropeBundle', effectReward: 'rock' }),
  define({ id: 'caverns.eelTunnel', biome: 'caverns', name: 'eel tunnel', description: 'A narrow black tunnel exhales brine.', glyph: 'e', color: '#91bd9f', terrain: ['floor', 'water', 'darkness'], tags: ['route', 'water', 'hazard'], hooks: ['operate', 'bomb', 'fire', 'force', 'hazard'], activationReward: 'pull', effectReward: 'waterScript' }),
  define({ id: 'caverns.sealedParcel', biome: 'caverns', name: 'sealed parcel', description: 'Wax seals survive beneath a crust of salt.', glyph: '?', color: '#e5d6a4', terrain: ['floor', 'darkness'], tags: ['cache', 'water'], hooks: ['operate', 'bomb', 'water', 'throw', 'hazard'], activationReward: 'key', effectReward: 'focusTonic' }),
  define({ id: 'ruins.brokenStatue', biome: 'ruins', name: 'broken statue', description: 'A stone guardian has fallen across old mosaic.', glyph: 'S', color: '#b9b0c1', terrain: ['floor', 'dart'], tags: ['route', 'salvage', 'force'], hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'], activationReward: 'rock', effectReward: 'tonic' }),
  define({ id: 'ruins.ritualBrazier', biome: 'ruins', name: 'ritual brazier', description: 'Cold ash waits in a ring of warding marks.', glyph: 'B', color: '#e59b64', terrain: ['floor', 'altar'], tags: ['ritual', 'fire', 'hazard'], hooks: ['operate', 'fire', 'water', 'force', 'hazard'], activationReward: 'ember', effectReward: 'wardScript' }),
  define({ id: 'ruins.glyphTablet', biome: 'ruins', name: 'glyph tablet', description: 'A carved tablet records a warning in stone.', glyph: 'T', color: '#c6bad6', terrain: ['floor', 'dart'], tags: ['warning', 'ritual'], hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'], activationReward: 'mapScroll', effectReward: 'sight' }),
  define({ id: 'ruins.collapsedArch', biome: 'ruins', name: 'collapsed arch', description: 'A cracked arch leans over the passage.', glyph: 'A', color: '#a89fae', terrain: ['floor', 'dart'], tags: ['route', 'salvage', 'force'], hooks: ['operate', 'bomb', 'force', 'throw', 'hazard'], activationReward: 'pickaxe', effectReward: 'rock' }),
  define({ id: 'ruins.sealedCache', biome: 'ruins', name: 'sealed cache', description: 'Bronze clasps hold a cache shut.', glyph: '?', color: '#d8b363', terrain: ['floor', 'altar'], tags: ['cache', 'ritual'], hooks: ['operate', 'bomb', 'fire', 'throw', 'hazard'], activationReward: 'key', effectReward: 'sunseal' }),
  define({ id: 'ruins.monolith', biome: 'ruins', name: 'monolith', description: 'A black monolith hums with a warded pulse.', glyph: 'M', color: '#d2a4e8', terrain: ['floor', 'altar', 'dart'], tags: ['ritual', 'force', 'hazard'], hooks: ['operate', 'bomb', 'fire', 'force', 'hazard'], activationReward: 'ward', effectReward: 'gate' })
] as const satisfies readonly PropDefinition[]

export const PROP_IDS = PROP_DEFINITIONS.map(definition => definition.id) as readonly PropId[]
const definitions = new Map<PropId, PropDefinition>(PROP_DEFINITIONS.map(definition => [definition.id, definition]))

export const propDefinition = (id: PropId): PropDefinition => {
  const definition = definitions.get(id)
  if (!definition) throw new Error(`missing prop definition: ${id}`)
  return definition
}

export const propDefinitionsFor = (biome: Biome): readonly PropDefinition[] => PROP_DEFINITIONS.filter(definition => definition.biome === biome)
export const propAt = (props: readonly Prop[], x: number, y: number): Prop | undefined => props.find(prop => prop.x === x && prop.y === y && prop.state !== 'destroyed')
export const isBlockingProp = (prop: Prop | undefined): boolean => (prop?.kind === 'mine.brokenCart' && prop.state !== 'destroyed') || (prop?.kind === 'wilds.rootArch' && prop.state !== 'activated' && prop.state !== 'destroyed') || (prop?.kind === 'caverns.brokenBoat' && prop.state !== 'activated' && prop.state !== 'destroyed') || (prop?.kind === 'caverns.eelTunnel' && prop.state === 'activated')
export const isSightBlockingProp = (prop: Prop | undefined): boolean => prop?.kind === 'caverns.crystalCluster' && prop.state !== 'activated' && prop.state !== 'destroyed'
export const isLineBlockingProp = (prop: Prop | undefined): boolean => isBlockingProp(prop) || isSightBlockingProp(prop)
export const propEffects = (hooks: readonly PropHook[]): PropEffectKind[] => hooks.filter((hook): hook is PropEffectKind => hook !== 'operate')

export const validatePropDefinitions = (): string[] => {
  const errors: string[] = []
  if (PROP_DEFINITIONS.length !== 24) errors.push(`expected 24 prop definitions, found ${PROP_DEFINITIONS.length}`)
  const ids = new Set<string>()
  for (const definition of PROP_DEFINITIONS) {
    if (ids.has(definition.id)) errors.push(`duplicate prop definition: ${definition.id}`)
    ids.add(definition.id)
    if (!definition.tags.length) errors.push(`missing tags: ${definition.id}`)
    if (!definition.hooks.includes('operate') || !propEffects(definition.hooks).length) errors.push(`incomplete hooks: ${definition.id}`)
    if (!ITEM[definition.activationReward] || !ITEM[definition.effectReward]) errors.push(`unknown reward: ${definition.id}`)
  }
  for (const biome of ['mine', 'wilds', 'caverns', 'ruins'] as const) if (propDefinitionsFor(biome).length !== 6) errors.push(`expected 6 ${biome} props`)
  return errors
}
