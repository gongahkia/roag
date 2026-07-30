import { floorIndex, type Floor, type RunState, type SecretAccessMethod, type SecretClueChannel, type SecretEntryCondition, type SecretResolution, type SecretRewardClass, type SecretRewardProfile, type SecretRisk, type SecretRiskProfile, type SecretRoom, type SecretRoomKind, type SecretRoute, type SideSpace } from './types'

type SecretRules = { rewardProfile: SecretRewardProfile; riskProfile: SecretRiskProfile }
type SecretSourceKind = SideSpace['kind']

const rules: Record<SecretSourceKind, readonly SecretRules[]> = {
  'mine-breach-room': [
    { rewardProfile: { kind: 'high-value-resource', label: 'rail cache', value: 80, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'resource-opportunity-cost', label: 'breach cost', detail: 'Opening it spends a route-clearing resource.' } },
    { rewardProfile: { kind: 'kit-choice', label: 'salvage kit', value: 75, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'terrain-hazard', label: 'unstable rails', detail: 'The return passes exposed rail stone.' } }
  ],
  'wilds-cave': [
    { rewardProfile: { kind: 'kit-choice', label: 'forager kit', value: 80, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'terrain-hazard', label: 'root snare', detail: 'The hollow leaves hazardous footing.' } },
    { rewardProfile: { kind: 'companion-lead', label: 'trailfolk lead', value: 70, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'ambush', label: 'nest watch', detail: 'A hidden watcher may contest the cache.' } }
  ],
  'cavern-hidden-chamber': [
    { rewardProfile: { kind: 'lore-relic', label: 'tide reliquary', value: 105, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'terrain-hazard', label: 'undertow', detail: 'Current pressure makes the chamber costly.' } },
    { rewardProfile: { kind: 'high-value-resource', label: 'sealed stores', value: 85, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'route-isolation', label: 'flooded return', detail: 'The side route can isolate a careless courier.' } }
  ],
  'ritual-hidden-chamber': [
    { rewardProfile: { kind: 'lore-relic', label: 'glyph archive', value: 110, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'tool-cooldown', label: 'ward drag', detail: 'The ward taxes the next traversal tool.' } },
    { rewardProfile: { kind: 'companion-lead', label: 'ritualist lead', value: 75, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'ambush', label: 'guardian echo', detail: 'The glyphs can call a defender.' } }
  ],
  'furnace-service-space': [
    { rewardProfile: { kind: 'high-value-resource', label: 'service stores', value: 85, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'terrain-hazard', label: 'smoke lane', detail: 'Heat and smoke guard the service route.' } },
    { rewardProfile: { kind: 'kit-choice', label: 'kiln kit', value: 80, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'tool-cooldown', label: 'heat warp', detail: 'The route delays the next tool use.' } }
  ],
  'cliff-alcove': [
    { rewardProfile: { kind: 'kit-choice', label: 'climber kit', value: 80, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'route-isolation', label: 'exposed return', detail: 'Wind can cut the alcove off from the main route.' } },
    { rewardProfile: { kind: 'shortcut-access', label: 'ridge bypass', value: 100, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'resource-opportunity-cost', label: 'rope commitment', detail: 'The climb commits scarce rope and time.' } }
  ],
  'burial-crypt': [
    { rewardProfile: { kind: 'companion-lead', label: 'ancestor lead', value: 90, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'ambush', label: 'restless dead', detail: 'The crypt can answer with an ambush.' } },
    { rewardProfile: { kind: 'lore-relic', label: 'ossuary relic', value: 105, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'route-isolation', label: 'sealed procession', detail: 'The burial route narrows the return.' } }
  ],
  'frost-cave': [
    { rewardProfile: { kind: 'shortcut-access', label: 'ice shelf bypass', value: 100, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'tool-cooldown', label: 'frozen gear', detail: 'Cold delays the next tool cycle.' } },
    { rewardProfile: { kind: 'lore-relic', label: 'rime reliquary', value: 110, cap: 1, duplicateRule: 'once-per-run' }, riskProfile: { kind: 'terrain-hazard', label: 'frost rime', detail: 'The hollow leaves freezing ground.' } }
  ]
}

const sourceKindFor = (sourceId: string): SecretSourceKind => sourceId.startsWith('mine-breach:') ? 'mine-breach-room'
  : sourceId.startsWith('wilds-cave:') ? 'wilds-cave'
    : sourceId.startsWith('cavern-hidden:') ? 'cavern-hidden-chamber'
      : sourceId.startsWith('ritual-hidden:') ? 'ritual-hidden-chamber'
        : sourceId.startsWith('furnace-service:') ? 'furnace-service-space'
          : sourceId.startsWith('cliff-alcove:') ? 'cliff-alcove'
            : sourceId.startsWith('burial-crypt:') ? 'burial-crypt'
              : 'frost-cave'
const sourceHash = (sourceId: string): number => [...sourceId].reduce((value, char) => (value * 31 + char.charCodeAt(0)) >>> 0, 0)
export const secretRulesForSourceId = (sourceId: string): SecretRules => {
  const choices = rules[sourceKindFor(sourceId)]
  const selected = choices[sourceHash(sourceId) % choices.length]!
  return { rewardProfile: { ...selected.rewardProfile }, riskProfile: { ...selected.riskProfile } }
}

const profileFor = (space: SideSpace): { kind: SecretRoomKind; entryCondition: SecretEntryCondition; discoveryClue: string; clueChannel: SecretClueChannel; accessMethod: SecretAccessMethod; rewardClass: SecretRewardClass; risk: SecretRisk } => space.kind === 'mine-breach-room'
  ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'fractured rail stone', clueChannel: 'terrain', accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust' }
  : space.kind === 'wilds-cave'
    ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'root-choked hollow', clueChannel: 'sight', accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust' }
    : space.kind === 'cavern-hidden-chamber'
      ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'current-fed fissure', clueChannel: 'sound', accessMethod: 'breach', rewardClass: 'ritual', risk: 'undertow' }
      : space.kind === 'ritual-hidden-chamber'
        ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'broken glyph seam', clueChannel: 'ritual', accessMethod: 'breach', rewardClass: 'ritual', risk: 'ward' }
        : space.kind === 'furnace-service-space'
          ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'warm service vent', clueChannel: 'sound', accessMethod: 'breach', rewardClass: 'supplies', risk: 'smoke' }
          : space.kind === 'cliff-alcove'
            ? { kind: 'side-pocket', entryCondition: 'anchored-rope', discoveryClue: 'weathered rope anchor', clueChannel: 'sight', accessMethod: 'climb', rewardClass: 'supplies', risk: 'fall' }
            : space.kind === 'burial-crypt'
              ? { kind: 'side-pocket', entryCondition: 'sealed-breakwall', discoveryClue: 'settled cairn seam', clueChannel: 'prop', accessMethod: 'breach', rewardClass: 'ritual', risk: 'spirits' }
              : { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'rime-covered hollow', clueChannel: 'terrain', accessMethod: 'breach', rewardClass: 'ritual', risk: 'cold' }

const entriesFor = (space: SideSpace) => space.kind === 'burial-crypt' ? space.entries : [space.entry]

export const placeSecretMetadata = (floor: Floor): void => {
  const rooms: SecretRoom[] = (floor.sideSpaces ?? []).map(space => {
    const profile = profileFor(space)
    const rule = secretRulesForSourceId(space.id)
    const id = `secret-room:${space.id}`
    space.reward.secretId = id
    return { version: 1, id, sourceId: space.id, ...profile, ...rule, approach: { ...space.approach }, entries: entriesFor(space).map(point => ({ ...point })), chamber: space.chamber.map(point => ({ ...point })), safeFallback: true }
  })
  const roomFor = new Map(rooms.map(room => [room.sourceId, room]))
  const routes: SecretRoute[] = (floor.sideSpaces ?? []).flatMap(space => {
    const room = roomFor.get(space.id)!
    const access = room.entries.map((entry, index) => ({ version: 1 as const, id: `secret-route:${room.id}:access:${index}`, roomId: room.id, kind: 'concealed-passage' as const, from: { ...room.approach }, entry: { ...entry }, entryCondition: room.entryCondition, discoveryClue: room.discoveryClue, accessMethod: room.accessMethod, rewardClass: room.rewardClass, risk: room.risk, safeFallback: true as const }))
    if (space.kind !== 'mine-breach-room' || !space.rareTransition) return access
    return [...access, { version: 1 as const, id: `secret-route:${room.id}:transition`, roomId: room.id, kind: 'rare-transition' as const, from: { ...room.approach }, entry: { ...space.entry }, entryCondition: room.entryCondition, discoveryClue: room.discoveryClue, accessMethod: room.accessMethod, rewardClass: 'shortcut' as const, risk: room.risk, safeFallback: true as const, destination: { biome: space.rareTransition.targetBiome, floor: space.rareTransition.targetFloor }, direction: 'one-way' as const, arrival: 'floor-start' as const, returnSemantics: 'no-return' as const }]
  })
  floor.secretRooms = rooms
  floor.secretRoutes = routes
}

const distance = (left: { x: number; y: number }, right: { x: number; y: number }): number => Math.max(Math.abs(left.x - right.x), Math.abs(left.y - right.y))
const visible = (floor: Floor, point: { x: number; y: number }): boolean => floor.tiles[floorIndex(floor, point.x, point.y)]?.visible === true

export const secretClueTrigger: Record<SecretClueChannel, string> = {
  sight: 'see the marked entry',
  sound: 'hear it within two tiles',
  prop: 'inspect its marker with C',
  terrain: 'stand on its telltale terrain',
  ritual: 'work an Astral charm nearby'
}

export const secretInteractionHint = (room: SecretRoom): string => room.accessMethod === 'breach' ? 'Use B beside the sealed entry to breach it.' : 'Follow the rope-marked entry to climb in.'
export const isSecretDiscovered = (room: SecretRoom): boolean => room.discovery !== undefined
export const secretDiscoveryMessage = (room: SecretRoom): string => {
  const channel = room.discovery?.channel ?? room.clueChannel
  return `Secret found by ${channel} (${secretClueTrigger[channel]}): ${room.discoveryClue}. ${secretInteractionHint(room)}`
}

const clueIsAvailable = (state: RunState, room: SecretRoom, channel: SecretClueChannel): boolean => channel === 'sight'
  ? room.entries.some(point => visible(state.floor, point))
  : channel === 'sound'
    ? distance(state.hero, room.approach) <= 2
    : channel === 'prop' || channel === 'terrain'
      ? distance(state.hero, room.approach) === 0
      : distance(state.hero, room.approach) <= 4

export const discoverSecretClues = (state: RunState, channel: SecretClueChannel): SecretRoom[] => (state.floor.secretRooms ?? []).filter(room => {
  if (room.discovery || room.clueChannel !== channel || !clueIsAvailable(state, room, channel)) return false
  room.discovery = { channel, turn: state.turn }
  return true
})

export type SecretRewardClaim = { room: SecretRoom; resolution: SecretResolution } | 'already-resolved' | undefined
export const claimSecretReward = (state: RunState, secretId: string): SecretRewardClaim => {
  const room = state.floor.secretRooms?.find(candidate => candidate.id === secretId)
  if (!room) return undefined
  if (room.resolution) return 'already-resolved'
  const resolution: SecretResolution = { turn: state.turn, rewardKind: room.rewardProfile.kind, rewardValue: room.rewardProfile.value, riskKind: room.riskProfile.kind }
  room.resolution = resolution
  return { room, resolution }
}
export const secretResolutionMessage = (room: SecretRoom): string => `Secret resolved — ${room.rewardProfile.label} (+${room.rewardProfile.value} exploration). Risk: ${room.riskProfile.label}; ${room.riskProfile.detail}`
export const secretShortcutReport = (route: SecretRoute): string => route.destination ? `${route.direction === 'two-way' ? 'two-way' : 'one-way'} ${route.destination.biome} shortcut to floor ${route.destination.floor + 1}, arrival at floor start; ${route.returnSemantics === 'return-link' ? 'return link available' : 'no return'}` : 'shortcut destination unavailable'
