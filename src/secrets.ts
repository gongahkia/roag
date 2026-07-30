import { floorIndex, type Floor, type RunState, type SecretAccessMethod, type SecretClueChannel, type SecretEntryCondition, type SecretRewardClass, type SecretRisk, type SecretRoom, type SecretRoomKind, type SecretRoute, type SideSpace } from './types'

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
    const id = `secret-room:${space.id}`
    space.reward.secretId = id
    return { version: 1, id, sourceId: space.id, ...profile, approach: { ...space.approach }, entries: entriesFor(space).map(point => ({ ...point })), chamber: space.chamber.map(point => ({ ...point })), safeFallback: true }
  })
  const roomFor = new Map(rooms.map(room => [room.sourceId, room]))
  const routes: SecretRoute[] = (floor.sideSpaces ?? []).flatMap(space => {
    const room = roomFor.get(space.id)!
    const access = room.entries.map((entry, index) => ({ version: 1 as const, id: `secret-route:${room.id}:access:${index}`, roomId: room.id, kind: 'concealed-passage' as const, from: { ...room.approach }, entry: { ...entry }, entryCondition: room.entryCondition, discoveryClue: room.discoveryClue, accessMethod: room.accessMethod, rewardClass: room.rewardClass, risk: room.risk, safeFallback: true as const }))
    if (space.kind !== 'mine-breach-room' || !space.rareTransition) return access
    return [...access, { version: 1 as const, id: `secret-route:${room.id}:transition`, roomId: room.id, kind: 'rare-transition' as const, from: { ...room.approach }, entry: { ...space.entry }, entryCondition: room.entryCondition, discoveryClue: room.discoveryClue, accessMethod: room.accessMethod, rewardClass: 'shortcut' as const, risk: room.risk, safeFallback: true as const, destination: { biome: space.rareTransition.targetBiome, floor: space.rareTransition.targetFloor } }]
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
