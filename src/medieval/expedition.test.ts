import { describe, expect, it } from 'vitest'
import { initialHouseholdActiveCrew } from './initial-household'
import { expeditionPressure } from './expedition'
import { actInHearthfordExpedition, chooseHearthfordExpeditionLoadout, chooseHearthfordExpeditionSupport, chooseInitialCourier, createFoundationWorld, decideHearthfordExpeditionObjective, deliverHearthfordExpeditionSealCord, departForHearthfordExpedition, moveFoundationWorldCourier, moveHearthfordExpeditionCourier, resolveCourierContinuityLoss, returnFromHearthfordExpedition, validateFoundationWorld } from './world'

const deckMove = (world: ReturnType<typeof chooseInitialCourier>, directions: readonly ('north' | 'south' | 'west' | 'east')[]) => directions.reduce((current, direction) => {
  const result = moveFoundationWorldCourier(current, direction)
  if (result.status !== 'moved') throw new Error(`deck fixture could not move ${direction}`)
  return result.world
}, world)

const changed = <T extends { status: string }>(transition: T): Extract<T, { status: 'changed' }> => {
  if (transition.status !== 'changed') throw new Error(`expected changed expedition transition, received ${transition.status}`)
  return transition as Extract<T, { status: 'changed' }>
}

const preparedAtHearthford = (seed: string, loadout: 'spear-and-buckler' | 'smoke-and-hook' = 'spear-and-buckler') => {
  const source = createFoundationWorld({ seed, configuration: { preset: 'watershed' } })
  let world = chooseInitialCourier(source, initialHouseholdActiveCrew(source.crew)[0]!.id)
  world = deckMove(world, ['east', 'east', 'east'])
  world = chooseHearthfordExpeditionLoadout(world, loadout)
  world = chooseHearthfordExpeditionSupport(world, loadout === 'smoke-and-hook' ? 'quiet-scout' : 'porter')
  world = deckMove(world, ['west', 'west', 'west', 'south', 'west'])
  world = departForHearthfordExpedition(world)
  world = changed(moveHearthfordExpeditionCourier(world, 'east')).world
  world = changed(moveHearthfordExpeditionCourier(world, 'east')).world
  return world
}

const acceptedHearthford = (seed: string, loadout: 'spear-and-buckler' | 'smoke-and-hook' = 'spear-and-buckler') => decideHearthfordExpeditionObjective(preparedAtHearthford(seed, loadout), 'accept')

const mapMoves = (world: ReturnType<typeof acceptedHearthford>, directions: readonly ('north' | 'south' | 'west' | 'east')[]) => directions.reduce((current, direction) => changed(moveHearthfordExpeditionCourier(current, direction)).world, world)

describe('first Hearthford expedition', () => {
  it('requires physical chart-table preparation and gangplank departure, preserving zero-time choices', () => {
    const source = createFoundationWorld({ seed: 'expedition-physical-props', configuration: { preset: 'watershed' } })
    let world = chooseInitialCourier(source, initialHouseholdActiveCrew(source.crew)[0]!.id)
    const before = world.state.temporal.worldTime
    expect(() => chooseHearthfordExpeditionLoadout(world, 'spear-and-buckler')).toThrow('physical')
    world = deckMove(world, ['east', 'east', 'east'])
    world = chooseHearthfordExpeditionLoadout(world, 'spear-and-buckler')
    world = chooseHearthfordExpeditionSupport(world, 'field-dresser')
    expect(world.state.temporal.worldTime).toBe(before + 3)
    world = deckMove(world, ['west', 'west', 'west', 'south', 'west'])
    const departed = departForHearthfordExpedition(world)
    expect(departed.state.expedition).toMatchObject({ location: 'hearthford', loadout: 'spear-and-buckler', support: 'field-dresser', coordinate: { column: 1, row: 3 } })
    expect(departed.state.temporal.worldTime).toBe(world.state.temporal.worldTime)
    expect(validateFoundationWorld(departed)).toEqual([])
  }, 20_000)

  it('supports an authored combat expedition with visible pressure, resource delivery, return, and a changed repeat departure', () => {
    let world = acceptedHearthford('expedition-combat')
    world = mapMoves(world, ['east', 'east', 'east'])
    expect(world.state.expedition).toMatchObject({ threat: { status: 'engaged', health: 2 }, injury: 'hurt' })
    const pressure = expeditionPressure(world.state.expedition, world.state.temporal.worldTime)
    expect(pressure).toMatchObject({ elapsed: expect.any(Number), depth: 5, noise: expect.any(Number), valuables: 0 })

    world = changed(actInHearthfordExpedition(world, 'brace')).world
    world = changed(actInHearthfordExpedition(world, 'attack')).world
    world = changed(actInHearthfordExpedition(world, 'attack')).world
    expect(world.state.expedition.threat).toMatchObject({ status: 'defeated', health: 0 })
    world = mapMoves(world, ['east', 'north', 'north', 'east'])
    expect(world.state.expedition.resource).toBe('carried')
    world = mapMoves(world, ['west', 'south', 'south', 'west', 'west', 'west', 'west'])
    world = deliverHearthfordExpeditionSealCord(world)
    expect(world.state.expedition).toMatchObject({ objective: 'completed', resource: 'delivered', consequence: 'seal-delivered' })
    world = mapMoves(world, ['west', 'west'])
    world = changed(returnFromHearthfordExpedition(world)).world
    expect(world.state.expedition).toMatchObject({ location: 'jomon', expeditionCount: 1, consequence: 'seal-delivered' })

    const repeat = departForHearthfordExpedition(world)
    expect(repeat.state.expedition).toMatchObject({ location: 'hearthford', objective: 'completed', resource: 'delivered', threat: { status: 'defeated' }, consequence: 'seal-delivered', expeditionCount: 2 })
    expect(validateFoundationWorld(repeat)).toEqual([])
  }, 90_000)

  it('uses lowered reeds plus quiet prepared movement and the smoke-and-hook loadout to evade rather than fight', () => {
    let world = acceptedHearthford('expedition-evade', 'smoke-and-hook')
    world = mapMoves(world, ['west', 'west', 'north', 'north', 'east', 'east', 'east', 'east'])
    world = changed(actInHearthfordExpedition(world, 'lower-reed-screen')).world
    world = mapMoves(world, ['east', 'east', 'east'])
    expect(world.state.expedition).toMatchObject({ resource: 'carried', threat: { status: 'engaged' }, reedScreen: 'lowered', injury: 'clear' })
    world = changed(actInHearthfordExpedition(world, 'evade')).world
    expect(world.state.expedition.threat).toMatchObject({ status: 'evaded', health: 2 })
    world = mapMoves(world, ['west', 'west', 'west', 'west', 'west', 'west', 'west', 'south', 'south', 'east', 'east'])
    world = deliverHearthfordExpeditionSealCord(world)
    world = mapMoves(world, ['west', 'west'])
    world = changed(returnFromHearthfordExpedition(world)).world
    expect(world.state.expedition).toMatchObject({ location: 'jomon', objective: 'completed', consequence: 'seal-delivered', threat: { status: 'evaded' } })
  }, 90_000)

  it('routes a second unbraced hound strike through the existing permanent-death succession reducer', () => {
    let world = acceptedHearthford('expedition-succession')
    world = mapMoves(world, ['east', 'east', 'east'])
    const fatal = actInHearthfordExpedition(world, 'attack')
    expect(fatal.status).toBe('courier-death')
    if (fatal.status !== 'courier-death') throw new Error('expected permanent expedition defeat')
    expect(fatal.world.state.expedition).toMatchObject({ location: 'jomon', objective: 'failed', consequence: 'courier-lost' })
    const resolution = resolveCourierContinuityLoss(fatal.world, fatal.confirmation)
    expect(resolution.status).toBe('continued')
    if (resolution.status !== 'continued') throw new Error('expected a household successor')
    expect(resolution.world.state.courier.activeCourierId).not.toBe(world.state.courier.activeCourierId)
    expect(resolution.world.state.expedition).toMatchObject({ location: 'jomon', consequence: 'courier-lost' })
    expect(validateFoundationWorld(resolution.world)).toEqual([])
  }, 30_000)
})
