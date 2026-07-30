import type { Floor, SecretAccessMethod, SecretEntryCondition, SecretRewardClass, SecretRisk, SecretRoom, SecretRoomKind, SecretRoute, SideSpace } from './types'

const profileFor = (space: SideSpace): { kind: SecretRoomKind; entryCondition: SecretEntryCondition; discoveryClue: string; accessMethod: SecretAccessMethod; rewardClass: SecretRewardClass; risk: SecretRisk } => space.kind === 'mine-breach-room'
  ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'fractured rail stone', accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust' }
  : space.kind === 'wilds-cave'
    ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'root-choked hollow', accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust' }
    : space.kind === 'cavern-hidden-chamber'
      ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'current-fed fissure', accessMethod: 'breach', rewardClass: 'ritual', risk: 'undertow' }
      : space.kind === 'ritual-hidden-chamber'
        ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'broken glyph seam', accessMethod: 'breach', rewardClass: 'ritual', risk: 'ward' }
        : space.kind === 'furnace-service-space'
          ? { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'warm service vent', accessMethod: 'breach', rewardClass: 'supplies', risk: 'smoke' }
          : space.kind === 'cliff-alcove'
            ? { kind: 'side-pocket', entryCondition: 'anchored-rope', discoveryClue: 'weathered rope anchor', accessMethod: 'climb', rewardClass: 'supplies', risk: 'fall' }
            : space.kind === 'burial-crypt'
              ? { kind: 'side-pocket', entryCondition: 'sealed-breakwall', discoveryClue: 'settled cairn seam', accessMethod: 'breach', rewardClass: 'ritual', risk: 'spirits' }
              : { kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: 'rime-covered hollow', accessMethod: 'breach', rewardClass: 'ritual', risk: 'cold' }

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
