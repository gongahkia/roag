import { describe, expect, it } from 'vitest'
import { autoplayDecision, autoplayOptionalDiagnostics, createAutoplayContext } from './autoplay'
import { castAstral } from './engine/astral'
import { operate } from './engine/inventory'
import { fieldReadout } from './engine/readout'
import { refreshFov } from './engine/visibility'
import { migrateRunRecord } from './storage'
import { createRun } from './test/factories'
import { indexOf, type SecretClueChannel, type SecretRoom } from './types'

const secret = (channel: SecretClueChannel, sourceId = `secret:${channel}`): SecretRoom => ({
  version: 1, id: `secret-room:${sourceId}`, sourceId, kind: 'hidden-room', approach: { x: 1, y: 1 }, entries: [{ x: 2, y: 1 }], chamber: [{ x: 3, y: 1 }], entryCondition: 'sealed-breakwall', discoveryClue: `${channel} clue`, clueChannel: channel, accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust', safeFallback: true
})

const fixture = (channel: SecretClueChannel) => {
  const state = createRun()
  state.floor.secretRooms = [secret(channel)]
  return state
}

describe('secret discovery clues', () => {
  it('discovers sight, sound, terrain, prop, and ritual clues through their affordances', () => {
    const sight = fixture('sight')
    refreshFov(sight)
    expect(sight.floor.secretRooms![0]!.discovery).toEqual({ channel: 'sight', turn: 0 })
    expect(sight.messages[0]).toContain('Secret found by sight')

    const sound = fixture('sound')
    refreshFov(sound)
    expect(sound.floor.secretRooms![0]!.discovery).toEqual({ channel: 'sound', turn: 0 })

    const terrain = fixture('terrain')
    refreshFov(terrain)
    expect(terrain.floor.secretRooms![0]!.discovery).toEqual({ channel: 'terrain', turn: 0 })

    const prop = fixture('prop')
    expect(operate(prop)).toEqual([{ type: 'menu' }])
    expect(prop.floor.secretRooms![0]!.discovery).toEqual({ channel: 'prop', turn: 0 })

    const ritual = fixture('ritual')
    castAstral(ritual, 'ward', { x: 1, y: 1 })
    expect(ritual.floor.secretRooms![0]!.discovery).toEqual({ channel: 'ritual', turn: 0 })
  })

  it('keeps undiscovered rooms out of visible autoplay and readout output', () => {
    const state = fixture('sight')
    const room = state.floor.secretRooms![0]!
    const reward = { id: 'ropeBundle' as const, x: 3, y: 1, count: 1, visibleInFog: true }
    state.floor.items = [reward]
    state.floor.sideSpaces = [{ id: room.sourceId, kind: 'mine-breach-room', approach: { ...room.approach }, entry: { ...room.entries[0]! }, chamber: room.chamber.map(point => ({ ...point })), reward }]
    expect(autoplayOptionalDiagnostics(state, 'visible', 'explore')).toEqual([])
    expect(autoplayOptionalDiagnostics(state, 'omniscient', 'explore')).toHaveLength(1)
    expect(fieldReadout(state).lines.join('\n')).not.toContain(room.discoveryClue)
    state.hero.bombs = 2
    state.floor.objective.status = 'complete'
    state.floor.guardianDefeated = true
    state.floor.tiles[indexOf(2, 1)]!.kind = 'breakwall'
    state.floor.tiles[indexOf(2, 1)]!.visible = false
    state.floor.tiles[indexOf(3, 1)]!.visible = false
    room.discovery = { channel: 'sight', turn: 0 }
    expect(autoplayDecision(state, 'visible', 'explore', createAutoplayContext())).toMatchObject({ command: 'b', reason: `open optional secret:${room.sourceId}` })
  })

  it('persists discovered clues through save and load with their interaction hint', () => {
    const state = fixture('terrain')
    refreshFov(state)
    const restored = migrateRunRecord(structuredClone(state))
    expect(restored?.floor.secretRooms?.[0]?.discovery).toEqual({ channel: 'terrain', turn: 0 })
    expect(fieldReadout(restored!).lines.join('\n')).toContain('Use B beside the sealed entry to breach it.')
  })
})
