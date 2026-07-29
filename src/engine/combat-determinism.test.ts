import { describe, expect, it } from 'vitest'
import { addCondition } from './conditions'
import { advance } from './combat'
import { resolveDisplacement } from './displacement'
import { advanceGuardianPhase } from './guardians'
import { resolveLineEffect } from './line-effect'
import { projectBolt } from './projectiles'
import { createEnemy, createRun } from '../test/factories'

const scenario = (seed: number) => {
  const projectileState = createRun({ seed })
  projectileState.floor.actors = [createEnemy({ id: 'sapper-1', ai: 'ranged', x: 5, y: 1, energy: 0 })]
  const line = resolveLineEffect(projectileState.floor, { x: 5, y: 1 }, projectileState.hero)
  const bolt = projectBolt(projectileState.floor, { x: 5, y: 1 }, projectileState.hero)
  const health = projectileState.hero.health
  advance(projectileState, [])
  const telegraph = structuredClone(projectileState.floor.telegraphs?.[0])
  advance(projectileState, [])

  const conditionState = createRun({ seed })
  addCondition(conditionState.hero, { kind: 'burning', duration: 1, potency: 3 })
  addCondition(conditionState.hero, { kind: 'shielded', duration: 1, potency: 1 })
  advance(conditionState, [])

  const displacementState = createRun({ seed })
  const target = createEnemy({ x: 3, y: 1 })
  displacementState.floor.actors = [target]
  displacementState.floor.tiles[1 * 48 + 4].kind = 'lava'
  const displacement = resolveDisplacement(displacementState, displacementState.hero, target, 'push')

  const guardianState = createRun({ seed })
  const guardian = createEnemy({ role: 'guardian', ai: 'guardian', maxHealth: 90, health: 60, guardianPhase: 'opening' })
  guardianState.floor.actors = [guardian]
  const guardianTransition = advanceGuardianPhase(guardianState, guardian)

  return {
    line, bolt, telegraph, projectileDamage: health - projectileState.hero.health,
    conditionHealth: conditionState.hero.health,
    displacement, target: { x: target.x, y: target.y, health: target.health },
    guardianTransition, guardianPhase: guardian.guardianPhase
  }
}

describe('combat determinism', () => {
  it('repeats resolver outcomes for a fixed seed', () => {
    expect(scenario(90210)).toEqual(scenario(90210))
  })

  it('covers telegraphs, conditions, displacement, line effects, and boss transitions', () => {
    const result = scenario(90210)
    expect(result.line.cells).toEqual(result.bolt.cells)
    expect(result.telegraph).toMatchObject({ actionId: 'enemy-shot', resolveTurn: 2 })
    expect(result.projectileDamage).toBeGreaterThan(0)
    expect(result.conditionHealth).toBeLessThan(22)
    expect(result.displacement).toMatchObject({ moved: true, hazard: 'lava' })
    expect(result.target).toMatchObject({ x: 4, y: 1, health: 0 })
    expect(result.guardianTransition).toMatchObject({ to: 'pressure', arena: 'hazard' })
  })
})
