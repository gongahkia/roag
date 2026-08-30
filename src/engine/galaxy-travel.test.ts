import { describe, expect, it } from 'vitest'
import { advanceTransitWindow, newHero, newRun, newTransitRun } from './run'
import { abandonGalaxyCargo, acceptGalaxyContract, applyGalaxySiteConditions, createGalaxy, deliverGalaxyContracts, discoverLinkedSites, recordGalaxyLanding } from './galaxy'

describe('galaxy travel and landing conditions', () => {
  it('creates a deterministic walkable link corridor with a usable far airlock', () => {
    const hero = newHero({ name: 'Ari' })
    const travel = { version: 1 as const, fromSiteId: 'sector-00:site-00', toSiteId: 'sector-00:site-01', linkId: 'sector-00:site-00::sector-00:site-01', chunkCount: 3, residentStart: 0, activeChunk: 0, situations: ['quiet', 'trader', 'hazard'] as const }
    const first = newTransitRun(71, 'wilds', hero, travel)
    const second = newTransitRun(71, 'wilds', hero, travel)

    expect(first.floor.layoutId).toBe('voyager-link-corridor')
    expect(first.floor.width).toBeGreaterThan(second.floor.height)
    expect(first.travel).toEqual(travel)
    expect(first.floor.objective.status).toBe('complete')
    expect(first.floor.guardianDefeated).toBe(true)
    expect(first.floor.tiles[first.floor.exit.y * first.floor.width + first.floor.exit.x]?.kind).toBe('exit')
    expect(first.floor.tiles).toEqual(second.floor.tiles)
  })

  it('keeps only a three-chunk resident window for longer routes', () => {
    const hero = newHero({ name: 'Ari' })
    const travel = { version: 1 as const, fromSiteId: 'sector-00:site-00', toSiteId: 'sector-00:site-01', linkId: 'sector-00:site-00::sector-00:site-01', chunkCount: 5, residentStart: 0, activeChunk: 0, situations: ['quiet', 'trader', 'hazard', 'patrol', 'ecology'] as const }
    const state = newTransitRun(71, 'wilds', hero, travel)
    const chunkWidth = state.floor.width / 3
    state.hero.x = chunkWidth * 2
    expect(advanceTransitWindow(state)).toBe(true)
    expect(state.travel?.residentStart).toBe(1)
    expect(state.floor.width).toBe(chunkWidth * 3)
    expect(state.hero.x).toBe(chunkWidth)
  })

  it('retains defeated or damaged route actors when their partition is evicted and revisited', () => {
    const hero = newHero({ name: 'Ari' })
    const travel = { version: 1 as const, fromSiteId: 'sector-00:site-00', toSiteId: 'sector-00:site-01', linkId: 'sector-00:site-00::sector-00:site-01', chunkCount: 5, residentStart: 0, activeChunk: 0, situations: ['quiet', 'patrol', 'hazard', 'quiet', 'ecology'] as const }
    const state = newTransitRun(71, 'wilds', hero, travel)
    const chunkWidth = state.floor.width / 3
    const patrol = state.floor.actors.find(actor => actor.id === `route-patrol:${travel.linkId}:1`)
    expect(patrol).toBeDefined()
    patrol!.health = 1

    state.hero.x = chunkWidth * 2
    expect(advanceTransitWindow(state)).toBe(true)
    state.hero.x = chunkWidth * 2
    expect(advanceTransitWindow(state)).toBe(true)
    state.hero.x = 0
    expect(advanceTransitWindow(state)).toBe(true)
    state.hero.x = 0
    expect(advanceTransitWindow(state)).toBe(true)

    expect(state.travel?.residentStart).toBe(0)
    expect(state.floor.actors.find(actor => actor.id === `route-patrol:${travel.linkId}:1`)?.health).toBe(1)
  })

  it('turns live territory and ecology into local encounter pressure and landing yield', () => {
    const galaxy = createGalaxy(91, newHero({ name: 'Ari' }), 0)
    const site = galaxy.sites[galaxy.activeSiteId]!
    site.control = 'voidborn'
    site.integrity = 20
    site.ecology = 20
    site.salvage = 90
    const run = newRun(91, site.biome)
    const hostile = run.floor.actors.find(actor => actor.hostile)!
    const before = { health: hostile.maxHealth, attack: hostile.attack, defense: hostile.defense }

    applyGalaxySiteConditions(run, site)

    expect(hostile.maxHealth).toBeGreaterThan(before.health)
    expect(hostile.attack).toBeGreaterThan(before.attack)
    expect(hostile.defense).toBeGreaterThan(before.defense)
    const credits = run.hero.gold
    const settled = recordGalaxyLanding(galaxy, site.id, run, 1)
    expect(run.hero.gold).toBeGreaterThan(credits)
    expect(settled.sites[site.id]!.salvage).toBeLessThan(site.salvage)
    expect(settled.sites[site.id]!.supplies).toBeGreaterThan(site.supplies)
  })

  it('loads physical-route contracts, pays delivery, and preserves failed cargo as a cache', () => {
    const galaxy = createGalaxy(19, newHero({ name: 'Ari' }), 0)
    const origin = galaxy.activeSiteId
    const destination = galaxy.sites[origin]!.links[0]!
    const surveyed = discoverLinkedSites(galaxy, origin, 0)
    const accepted = acceptGalaxyContract(surveyed, origin, destination)
    expect(accepted.galaxy.cargo).not.toHaveLength(0)
    const hero = newHero({ name: 'Ari' })
    const delivered = deliverGalaxyContracts(accepted.galaxy, destination, hero)
    expect(hero.gold).toBeGreaterThan(0)
    expect(delivered.galaxy.contracts.some(contract => contract.status === 'completed')).toBe(true)
    const second = acceptGalaxyContract(surveyed, origin, destination)
    const cached = abandonGalaxyCargo(second.galaxy, [origin, destination].sort().join('::'), 1)
    expect(cached.routeCaches.some(cache => !cache.recovered)).toBe(true)
  })
})
