import { describe, expect, it } from 'vitest'
import { MONSTERS, monsterRoleFor } from '../content'
import { MONSTER_ROLES } from '../types'
import { roleIntentFor, roleProfileFor } from './roles'
import { createEnemy } from '../test/factories'

describe('enemy role profiles', () => {
  it('assigns every reusable combat role to authored monsters with counterplay', () => {
    expect(new Set(MONSTERS.map(monsterRoleFor))).toEqual(new Set(MONSTER_ROLES))
    for (const monster of MONSTERS) expect(roleProfileFor(createEnemy({ kind: monster.id, ai: monster.ai, combatRole: monsterRoleFor(monster) })).counterplay).not.toEqual('')
  })

  it('only chooses ranged role actions within declared deterministic bounds', () => {
    const artillery = roleProfileFor(createEnemy({ kind: 'sapper', combatRole: 'artillery' }))
    expect(roleIntentFor(artillery, 6, true)).toMatchObject({ actionId: 'enemy-shot' })
    expect(roleIntentFor(artillery, 1, true)).toBeUndefined()
    expect(roleIntentFor(artillery, 6, false)).toBeUndefined()
  })
})
