import { describe, expect, it } from 'vitest'
import { pickUp } from './engine/inventory'
import { AREA_ORDER } from './engine/campaign'
import { secretRulesForSourceId } from './secrets'
import { createRun } from './test/factories'
import type { SecretRoom } from './types'
import { generateAreaFloor, hasPassablePath } from './world'

const rewardRoom = (sourceId = 'mine-breach:1:0'): SecretRoom => ({
  version: 1, id: `secret-room:${sourceId}`, sourceId, kind: 'hidden-room', approach: { x: 1, y: 1 }, entries: [{ x: 2, y: 1 }], chamber: [{ x: 3, y: 1 }], entryCondition: 'sealed-breakwall', discoveryClue: 'fractured rail stone', clueChannel: 'terrain', ...secretRulesForSourceId(sourceId), accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust', safeFallback: true
})

describe('secret reward and risk rules', () => {
  it('derives stable seeded reward and risk profiles with every documented class', () => {
    const first = secretRulesForSourceId('burial-crypt:81:1')
    expect(secretRulesForSourceId('burial-crypt:81:1')).toEqual(first)
    const prefixes = ['mine-breach', 'wilds-cave', 'cavern-hidden', 'ritual-hidden', 'furnace-service', 'cliff-alcove', 'burial-crypt', 'frost-cave']
    const profiles = prefixes.flatMap(prefix => Array.from({ length: 8 }, (_, index) => secretRulesForSourceId(`${prefix}:1:${index}`)))
    expect(new Set(profiles.map(profile => profile.rewardProfile.kind))).toEqual(new Set(['kit-choice', 'lore-relic', 'companion-lead', 'shortcut-access', 'high-value-resource']))
    expect(new Set(profiles.map(profile => profile.riskProfile.kind))).toEqual(new Set(['ambush', 'terrain-hazard', 'tool-cooldown', 'route-isolation', 'resource-opportunity-cost']))
  })

  it('resolves a secret once, logs its reward and risk, and records AP-06 value', () => {
    const state = createRun()
    const room = rewardRoom()
    state.floor.secretRooms = [room]
    const item = { id: 'tonic' as const, x: 1, y: 1, count: 1, secretId: room.id }
    state.floor.items = [item]
    pickUp(state)
    expect(room.resolution).toMatchObject({ rewardKind: room.rewardProfile.kind, rewardValue: room.rewardProfile.value, riskKind: room.riskProfile.kind })
    expect(state.telemetry?.secretValue).toBe(room.rewardProfile.value)
    expect(state.telemetry?.optionalContent?.used).toMatchObject({ [`secret-resolution:${room.id}:${room.rewardProfile.kind}`]: 1 })
    expect(state.messages.join('\n')).toContain('Secret resolved')
    state.floor.items = [{ ...item }]
    pickUp(state)
    expect(state.hero.inventory.filter(id => id === 'tonic')).toHaveLength(1)
    expect(state.telemetry?.secretValue).toBe(room.rewardProfile.value)
  })

  it('keeps a clear base route when every secret reward is unavailable', () => {
    for (const [routePosition, biome] of AREA_ORDER.entries()) {
      const floor = generateAreaFloor(37, biome, 0, routePosition)
      floor.items = floor.items.filter(item => !item.secretId)
      expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    }
  }, 20_000)
})
