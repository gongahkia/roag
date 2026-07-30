import { describe, expect, it } from 'vitest'
import { newRun } from './engine'
import { migrateRunRecord } from './storage'
import { generateAreaFloor, hasPassablePath, validateGeneration, validateSecretRoutes } from './world'

describe('secret metadata', () => {
  it('serializes deterministic versioned hidden rooms, side pockets, and passages', () => {
    const mine = generateAreaFloor(7, 'mine', 3, 3)
    const cliffs = generateAreaFloor(7, 'cliffs', 0, 3)
    expect(mine.secretRooms).toHaveLength(4)
    expect(mine.secretRooms).toEqual(expect.arrayContaining([expect.objectContaining({ version: 1, kind: 'hidden-room', entryCondition: 'sealed-breakwall', discoveryClue: expect.any(String), clueChannel: expect.any(String), rewardProfile: expect.objectContaining({ cap: 1, duplicateRule: 'once-per-run' }), riskProfile: expect.objectContaining({ kind: expect.any(String) }), accessMethod: 'breach', rewardClass: 'supplies', risk: 'dust', safeFallback: true })]))
    expect(mine.secretRoutes).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'concealed-passage' }), expect.objectContaining({ kind: 'rare-transition', rewardClass: 'shortcut', destination: { biome: 'wilds', floor: 0 } })]))
    expect(cliffs.secretRooms).toEqual(expect.arrayContaining([expect.objectContaining({ kind: 'side-pocket', entryCondition: 'anchored-rope', accessMethod: 'climb' })]))
    expect(mine.items.some(item => item.secretId === mine.secretRooms?.[0]?.id)).toBe(true)
    expect(JSON.parse(JSON.stringify(mine)).secretRooms).toEqual(mine.secretRooms)
    expect(migrateRunRecord(newRun(7))?.floor.secretRooms).toEqual(newRun(7).floor.secretRooms)
    expect(generateAreaFloor(7, 'mine', 3, 3).secretRoutes).toEqual(mine.secretRoutes)
  })

  it('keeps every generated secret optional to the required route', () => {
    const floor = generateAreaFloor(91, 'burial', 3, 3)
    expect(hasPassablePath(floor, floor.start, floor.exit)).toBe(true)
    expect(validateSecretRoutes(floor)).toEqual([])
    expect(validateGeneration(floor)).toEqual({ valid: true, errors: [] })
  })

  it('reports a secret dependency on campaign completion', () => {
    const floor = generateAreaFloor(91, 'mine', 0, 3)
    floor.secretRooms![0] = { ...floor.secretRooms![0]!, entries: [{ ...floor.exit }] }
    expect(validateSecretRoutes(floor)).toEqual(expect.arrayContaining(['secret dependency on campaign completion']))
  })
})
